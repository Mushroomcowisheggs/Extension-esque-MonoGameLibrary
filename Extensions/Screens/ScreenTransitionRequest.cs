namespace MonoGameLibrary.Extensions.Screens {
    /// <summary>
    /// Base type for a screen transition requested by a screen. Each operation has its own request
    /// type, so a pop cannot carry a screen and a push or change cannot omit one.
    /// </summary>
    public abstract class ScreenTransitionRequest {
    }
}
