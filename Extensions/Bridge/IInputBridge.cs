namespace MonoGameLibrary.Extensions.Bridge {
    /// <summary>
    /// Tests platform-neutral key codes against a backend input event. The contract is an
    /// adapter-facing bridge in the Extensions.Bridge anchor; the event-argument type is
    /// supplied by the caller so the contract names no backend type.
    /// </summary>
    public interface IInputBridge<in TKeyEventArguments, in TKey> {
        bool IsKeyPressed(
            TKeyEventArguments arguments,
            TKey key
        );
    }
}
