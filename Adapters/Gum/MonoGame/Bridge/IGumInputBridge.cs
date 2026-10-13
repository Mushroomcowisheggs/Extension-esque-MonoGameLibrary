namespace MonoGameLibrary.Adapters.Gum.MonoGame.Bridge {
    /// <summary>
    /// Tests platform-neutral key codes against a Gum input event. The contract lives
    /// with the Gum adapter that both implements and consumes it; the event-argument
    /// type is supplied by the caller so the contract names no Gum type.
    /// </summary>
    public interface IGumInputBridge<in TKeyEventArguments, in TKey> {
        bool IsKeyPressed(
            TKeyEventArguments arguments,
            TKey key
        );
    }
}
