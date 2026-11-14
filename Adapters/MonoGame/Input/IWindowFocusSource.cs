using MonoGameLibrary.Core.Concurrency;

namespace MonoGameLibrary.Adapters.MonoGame.Input {
    /// <summary>
    /// Reports whether the window currently accepts input.
    /// </summary>
    [ThreadCondition(ThreadAffinity.Frame)]
    public interface IWindowFocusSource {
        /// <summary>
        /// Gets a value indicating whether the window is active.
        /// </summary>
        bool IsActive { get; }
    }
}
