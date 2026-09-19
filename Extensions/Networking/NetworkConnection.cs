using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Diagnostics;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// The per-peer state a session keeps for one established link: the channel, the outbound queue, the two
    /// pump tasks and the timestamps the watchdog needs. A receive loop, a send loop and the watchdog touch
    /// the same instance concurrently, so every mutable member is either immutable after the handshake or
    /// guarded by the primitives below.
    /// </summary>
    internal sealed class NetworkConnection : IDisposable {
        /// <summary>The channel that carries framed payloads to this peer. It is closed, never replaced.</summary>
        internal INetworkChannel Channel;
        
        /// <summary>The identifier of the remote peer.</summary>
        internal Guid Identifier;
        
        /// <summary>The display name of the remote peer.</summary>
        internal string DisplayName = "";
        
        /// <summary>The remote endpoint in host:port form.</summary>
        internal string Address = "";
        
        /// <summary>True when the remote peer is the session host.</summary>
        internal bool HostFlag;
        
        /// <summary>The last measured round-trip time in milliseconds, or -1 when unknown.</summary>
        internal int MillisecondsLatency = -1;
        
        /// <summary>The largest number of frames that may wait for one peer before it is declared too slow.</summary>
        internal const int QueueFrameLimit = 512;
        
        /// <summary>The outbound queue drained by the send loop.</summary>
        internal readonly ConcurrentQueue<byte[]> OutboundQueue = new ConcurrentQueue<byte[]>();
        
        /// <summary>Set when the outbound queue overflowed, which the watchdog turns into a disconnect.</summary>
        internal bool QueueOverflowedFlag;
        
        /// <summary>Signals the send loop that the outbound queue is no longer empty.</summary>
        internal readonly SemaphoreSlim OutboundSignal = new SemaphoreSlim(0);
        
        /// <summary>The cancellation source that stops the pumps of this link.</summary>
        internal readonly CancellationTokenSource LinkSource = new CancellationTokenSource();
        
        private readonly ILogger _logger;
        
        /// <summary>The send loop task.</summary>
        internal Task SendLoopTask;
        
        /// <summary>The receive loop task.</summary>
        internal Task ReceiveLoopTask;
        
        private long _timestampLastReceived;
        private long _timestampPingSent;
        private long _timestampPingPayload;
        private int _flagDisposed;
        private int _flagSignalled;
        
        /// <summary>
        /// Initializes the timing fields so that a fresh link is never considered idle.
        /// </summary>
        internal NetworkConnection(Optional<ILogger> logger = default) {
            if (logger.HasValue) { _logger = logger.Value; } else { _logger = NullLogger.Instance; }
            long timestampNow = Environment.TickCount64;
            _timestampLastReceived = timestampNow;
            _timestampPingSent = timestampNow;
        }
        
        /// <summary>
        /// Records that a payload arrived from this peer.
        /// </summary>
        internal void MarkReceived() {
            Interlocked.Exchange(ref _timestampLastReceived, Environment.TickCount64);
        }
        
        /// <summary>
        /// Gets the number of milliseconds since the last payload arrived.
        /// </summary>
        internal long IdleMilliseconds {
            get {
                return Environment.TickCount64 - Interlocked.Read(ref _timestampLastReceived);
            }
        }
        
        /// <summary>
        /// Determines whether a probe is due and records that it is being sent.
        /// </summary>
        /// <param name="millisecondsInterval">The probe interval.</param>
        /// <param name="timestampPayload">The payload to embed in the probe.</param>
        /// <returns><c>true</c> when the caller should send a probe now.</returns>
        internal bool TryBeginPing(int millisecondsInterval, out long timestampPayload) {
            timestampPayload = 0;
            long timestampNow = Environment.TickCount64;
            long timestampPrevious = Interlocked.Read(ref _timestampPingSent);
            if (timestampNow - timestampPrevious < millisecondsInterval) {
                return false;
            }
            Interlocked.Exchange(ref _timestampPingSent, timestampNow);
            Interlocked.Exchange(ref _timestampPingPayload, timestampNow);
            timestampPayload = timestampNow;
            return true;
        }
        
        /// <summary>
        /// Completes a round trip when the echoed probe payload matches the probe that is outstanding.
        /// </summary>
        /// <param name="timestampPayload">The payload echoed by the peer.</param>
        /// <returns><c>true</c> when the latency was updated.</returns>
        internal bool TryCompletePing(long timestampPayload) {
            long timestampOutstanding = Interlocked.Read(ref _timestampPingPayload);
            if (timestampOutstanding == 0 || timestampOutstanding != timestampPayload) {
                return false;
            }
            long elapsed = Environment.TickCount64 - timestampOutstanding;
            if (elapsed < 0) {
                elapsed = 0;
            }
            if (elapsed > int.MaxValue) {
                elapsed = int.MaxValue;
            }
            MillisecondsLatency = (int)elapsed;
            return true;
        }
        
        /// <summary>
        /// Queues an already framed payload and wakes the send loop.
        /// </summary>
        /// <param name="frame">The framed payload.</param>
        internal void EnqueueFrame(byte[] frame) {
            if (OutboundQueue.Count >= QueueFrameLimit) {
                QueueOverflowedFlag = true;
                return;
            }
            OutboundQueue.Enqueue(frame);
            try {
                OutboundSignal.Release();
            } catch (ObjectDisposedException exception) {
                // The link was closed between the enqueue and the signal; the frame is dropped.
                _logger.Debug("A recoverable condition was handled: " + exception.Message);
                
                // The link was closed between the enqueue and the signal; the frame is dropped.
            } catch (SemaphoreFullException exception) {
                // The count already covers the queued frames, so the send loop will not sleep.
                _logger.Debug("A recoverable condition was handled: " + exception.Message);
            }
        }
        
        /// <summary>
        /// Clears the wake-up permit once the send loop has drained the queue, so the next frame wakes it again.
        /// </summary>
        internal void MarkQueueDrained() {
            Interlocked.Exchange(ref _flagSignalled, 0);
            if (!OutboundQueue.IsEmpty) { WakeSendLoop(); }
        }
        
        /// <summary>
        /// Wakes the send loop so that it can observe the cancellation that stops it.
        /// </summary>
        internal void WakeSendLoop() {
            try {
                OutboundSignal.Release();
            } catch (ObjectDisposedException exception) {
                // The link is already released.
                _logger.Debug("A recoverable condition was handled: " + exception.Message);
                
                // The link is already released.
            } catch (SemaphoreFullException exception) {
                // The send loop is already awake.
                _logger.Debug("A recoverable condition was handled: " + exception.Message);
            }
        }
        
        /// <summary>
        /// Stops the pumps and releases the channel. The method is idempotent.
        /// </summary>
        public void Dispose() {
            if (Interlocked.Exchange(ref _flagDisposed, 1) != 0) {
                return;
            }
            try {
                LinkSource.Cancel();
            } catch (ObjectDisposedException exceptionReleased) {
                // The cancellation source was already released.
                _logger.Debug("The resource was already released: " + exceptionReleased.Message);
            }
            INetworkChannel channel = Channel;
            if (channel != null) {
                channel.Close();
            }
            WakeSendLoop();
            LinkSource.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
