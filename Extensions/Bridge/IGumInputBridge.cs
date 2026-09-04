namespace MonoGameLibrary.Extensions.Bridge {
    /// <summary>
    /// Defines the cross-adapter contract for applying platform-neutral input
    /// values to a Gum input backend. The event-argument type is supplied by
    /// the consuming adapter so Extensions does not reference Gum.
    /// </summary>
    public interface IGumInputBridge<in TKeyEventArguments, in TKey> {
        bool IsKeyPressed(
            TKeyEventArguments arguments,
            TKey key
        );
    }
}
