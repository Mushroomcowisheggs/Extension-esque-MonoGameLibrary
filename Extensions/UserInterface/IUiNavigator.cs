using MonoGameLibrary.Core.Concurrency;

namespace MonoGameLibrary.Extensions.UserInterface {
    /// <summary>
    /// Navigation seam for the neutral user-interface widgets: reports whether a
    /// <see cref="NavigationKey"/> and whether the confirm action were just pressed.
    /// Widgets name only this contract; the concrete navigator is composed at the
    /// composition root from the platform's input source.
    /// </summary>
    [ThreadCondition(ThreadAffinity.Frame)]
    public interface IUiNavigator {
        /// <summary>Reports whether a navigation key was just pressed.</summary>
        /// <param name="key">The navigation key to poll.</param>
        /// <returns>True when the key was just pressed.</returns>
        bool IsPressed(NavigationKey key);
        
        /// <summary>Reports whether the confirm action was just pressed.</summary>
        /// <returns>True when the confirm action was just pressed.</returns>
        bool IsConfirmPressed();
    }
}
