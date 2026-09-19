using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using MonoGameLibrary.Core;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// The default <see cref="INetworkSerializer"/>. It maps each registered message type to a stable kind
    /// identifier and encodes the body with <see cref="JsonSerializer"/>.
    /// </summary>
    /// <remarks>
    /// The payload layout is a four-byte little-endian kind length, the UTF-8 kind, and the JSON body.
    /// Because the kind travels with every payload, a receiver never needs to guess a type from context.
    /// The control messages of <see cref="NetworkSessionMessages"/> are registered by the constructor and
    /// cannot be replaced.
    /// </remarks>
    public sealed class JsonNetworkSerializer : INetworkSerializer {
        private const int KindLengthLimit = 128;
        
        private readonly object _lock = new object();
        private readonly Dictionary<string, Type> _typesByKind = new Dictionary<string, Type>(StringComparer.Ordinal);
        private readonly Dictionary<Type, string> _kindsByType = new Dictionary<Type, string>();
        private readonly JsonSerializerOptions _optionsJson;
        
        /// <summary>
        /// Initializes a new serializer and registers the library control messages.
        /// </summary>
        /// <param name="optionsJson">
        /// Optional JSON settings shared by every payload. When omitted, default
        /// <see cref="JsonSerializerOptions"/> are used.
        /// </param>
        public JsonNetworkSerializer(Optional<JsonSerializerOptions> optionsJson = default) {
            if (optionsJson.HasValue) {
                _optionsJson = optionsJson.Value;
            } else {
                _optionsJson = new JsonSerializerOptions();
            }
            RegisterControlMessages();
        }
        
        /// <summary>
        /// Gets the number of message types the serializer can currently handle.
        /// </summary>
        public int RegisteredTypeCount {
            get {
                lock (_lock) {
                    return _kindsByType.Count;
                }
            }
        }
        
        /// <summary>
        /// Registers a message type under a stable kind identifier.
        /// </summary>
        /// <typeparam name="TMessage">The message type to register.</typeparam>
        /// <param name="kind">The identifier written into every payload of this type.</param>
        /// <exception cref="ArgumentException">Thrown if the kind is invalid or reserved.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the kind or the type is already registered.</exception>
        public void Register<TMessage>(string kind) where TMessage : INetworkMessage {
            Register(typeof(TMessage), kind);
        }
        
        /// <summary>
        /// Registers a message type under a stable kind identifier.
        /// </summary>
        /// <param name="typeMessage">The message type to register.</param>
        /// <param name="kind">The identifier written into every payload of this type.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="typeMessage"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the kind is invalid, reserved, or the type is not a concrete message.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the kind or the type is already registered.</exception>
        public void Register(Type typeMessage, string kind) {
            if (typeMessage == null) {
                throw new ArgumentNullException(nameof(typeMessage));
            }
            ValidateKind(kind);
            if (NetworkSessionMessages.IsControlKind(kind)) {
                throw new ArgumentException($"The kind prefix '{NetworkDefaults.KindPrefix}' is reserved by the library.", nameof(kind));
            }
            RegisterCore(typeMessage, kind);
        }
        
        /// <summary>
        /// Adds a mapping to the catalog. The library uses it for its own control messages, which own the
        /// reserved kind prefix and therefore cannot pass the public entry point.
        /// </summary>
        /// <param name="typeMessage">The message type to register.</param>
        /// <param name="kind">The identifier written into every payload of this type.</param>
        /// <exception cref="ArgumentException">Thrown if the type is not a concrete message.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the kind or the type is already registered.</exception>
        private void RegisterCore(Type typeMessage, string kind) {
            if (!typeof(INetworkMessage).IsAssignableFrom(typeMessage)) {
                throw new ArgumentException($"Type '{typeMessage.FullName}' does not implement {nameof(INetworkMessage)}.", nameof(typeMessage));
            }
            if (typeMessage.IsAbstract || typeMessage.IsInterface) {
                throw new ArgumentException($"Type '{typeMessage.FullName}' must be a concrete message type.", nameof(typeMessage));
            }
            
            lock (_lock) {
                if (_typesByKind.ContainsKey(kind)) {
                    throw new InvalidOperationException($"The message kind '{kind}' is already registered.");
                }
                if (_kindsByType.ContainsKey(typeMessage)) {
                    throw new InvalidOperationException($"The message type '{typeMessage.FullName}' is already registered as '{_kindsByType[typeMessage]}'.");
                }
                _typesByKind.Add(kind, typeMessage);
                _kindsByType.Add(typeMessage, kind);
            }
        }
        
        /// <summary>
        /// Determines whether a message type has been registered.
        /// </summary>
        /// <param name="typeMessage">The message type to test.</param>
        /// <returns><c>true</c> when the type can be serialized; otherwise <c>false</c>.</returns>
        public bool IsRegistered(Type typeMessage) {
            if (typeMessage == null) {
                return false;
            }
            lock (_lock) {
                return _kindsByType.ContainsKey(typeMessage);
            }
        }
        
        /// <summary>
        /// Determines whether a kind identifier has been registered.
        /// </summary>
        /// <param name="kind">The kind identifier to test.</param>
        /// <returns><c>true</c> when the kind can be deserialized; otherwise <c>false</c>.</returns>
        public bool IsKindRegistered(string kind) {
            if (string.IsNullOrEmpty(kind)) {
                return false;
            }
            lock (_lock) {
                return _typesByKind.ContainsKey(kind);
            }
        }
        
        /// <inheritdoc />
        public byte[] Serialize(INetworkMessage message) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            
            Type typeMessage = message.GetType();
            string kind;
            lock (_lock) {
                if (!_kindsByType.TryGetValue(typeMessage, out kind)) {
                    throw new NetworkProtocolException($"The message type '{typeMessage.FullName}' is not registered with the serializer.");
                }
            }
            
            byte[] bytesKind = Encoding.UTF8.GetBytes(kind);
            byte[] bytesBody = JsonSerializer.SerializeToUtf8Bytes(message, typeMessage, _optionsJson);
            byte[] payload = new byte[4 + bytesKind.Length + bytesBody.Length];
            NetworkBinary.WriteInt32(payload, 0, bytesKind.Length);
            Buffer.BlockCopy(bytesKind, 0, payload, 4, bytesKind.Length);
            Buffer.BlockCopy(bytesBody, 0, payload, 4 + bytesKind.Length, bytesBody.Length);
            return payload;
        }
        
        /// <inheritdoc />
        public bool TryDeserialize(byte[] payload, out string kind, out INetworkMessage message) {
            kind = string.Empty;
            message = null;
            if (payload == null) {
                return false;
            }
            if (payload.Length < 4) {
                return false;
            }
            
            int countKind = NetworkBinary.ReadInt32(payload, 0);
            if (countKind <= 0 || countKind > KindLengthLimit) {
                return false;
            }
            if (payload.Length < 4 + countKind) {
                return false;
            }
            kind = Encoding.UTF8.GetString(payload, 4, countKind);
            
            Type typeMessage;
            lock (_lock) {
                if (!_typesByKind.TryGetValue(kind, out typeMessage)) {
                    return false;
                }
            }
            
            try {
                object instance = JsonSerializer.Deserialize(
                    new ReadOnlySpan<byte>(payload, 4 + countKind, payload.Length - 4 - countKind),
                    typeMessage,
                    _optionsJson
                );
                message = instance as INetworkMessage;
            } catch (JsonException) {
                message = null;
            } catch (NotSupportedException) {
                message = null;
            }
            
            if (message == null) {
                return false;
            }
            return true;
        }
        
        /// <summary>
        /// Registers the control messages every session depends on.
        /// </summary>
        private void RegisterControlMessages() {
            RegisterCore(typeof(NetworkHelloMessage), NetworkSessionMessages.HelloKind);
            RegisterCore(typeof(NetworkWelcomeMessage), NetworkSessionMessages.WelcomeKind);
            RegisterCore(typeof(NetworkRefusedMessage), NetworkSessionMessages.RefusedKind);
            RegisterCore(typeof(NetworkRosterMessage), NetworkSessionMessages.RosterKind);
            RegisterCore(typeof(NetworkPingMessage), NetworkSessionMessages.PingKind);
            RegisterCore(typeof(NetworkPongMessage), NetworkSessionMessages.PongKind);
            RegisterCore(typeof(NetworkDisconnectMessage), NetworkSessionMessages.DisconnectKind);
            RegisterCore(typeof(RoomAdvertisement), NetworkDiscoveryMessages.AdvertisementKind);
            RegisterCore(typeof(RoomProbeRequest), NetworkDiscoveryMessages.ProbeKind);
        }
        
        /// <summary>
        /// Rejects kind identifiers that could not survive a round trip or that belong to the library.
        /// </summary>
        /// <param name="kind">The identifier to validate.</param>
        private static void ValidateKind(string kind) {
            if (string.IsNullOrWhiteSpace(kind)) {
                throw new ArgumentException("A message kind is required.", nameof(kind));
            }
            if (kind.Length > KindLengthLimit) {
                throw new ArgumentException($"A message kind cannot exceed {KindLengthLimit} characters.", nameof(kind));
            }
            for (int index = 0; index < kind.Length; index += 1) {
                char character = kind[index];
                if (character < 0x21 || character > 0x7E) {
                    throw new ArgumentException("A message kind may only contain printable ASCII characters without spaces.", nameof(kind));
                }
            }
        }
    }
}
