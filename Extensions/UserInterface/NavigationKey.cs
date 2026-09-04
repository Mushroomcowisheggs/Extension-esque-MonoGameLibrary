namespace MonoGameLibrary.Extensions.UserInterface {
    /// <summary>
    /// Keys understood by user-interface navigation. This UI-owned value
    /// avoids coupling the UserInterface extension to the Input extension.
    /// </summary>
    public enum NavigationKey {
        None,
        Tab,
        Up,
        Down,
        Left,
        Right,
        Enter,
        Escape,
        Space
    }
}
