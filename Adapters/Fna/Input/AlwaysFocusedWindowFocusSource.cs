namespace MonoGameLibrary.Adapters.Fna.Input {
    /// <summary>
    /// Reports focus unconditionally; used when no running game observes the window.
    /// </summary>
    public sealed class AlwaysFocusedWindowFocusSource : IWindowFocusSource {
        /// <inheritdoc />
        public bool IsActive {
            get { return true; }
        }
    }
}
