using MonoGameLibrary.Extensions.UserInterface;

namespace MonoGameLibrary.Adapters.Gum.MonoGame {
    /// <summary>Compatibility names for Gum tab navigation configuration.</summary>
    public static class GumServiceExtensions {
        /// <summary>Adds a key that moves focus forward through Gum's tab order.</summary>
        /// <param name="serviceUserInterface">The user-interface service to configure.</param>
        /// <param name="key">The key to add.</param>
        public static void AddTabForwardKey(
            this IUserInterfaceService serviceUserInterface,
            NavigationKey key
        ) {
            serviceUserInterface.AddNavigationForwardKey(key);
        }
        
        /// <summary>Adds a key that moves focus backward through Gum's tab order.</summary>
        /// <param name="serviceUserInterface">The user-interface service to configure.</param>
        /// <param name="key">The key to add.</param>
        public static void AddTabReverseKey(
            this IUserInterfaceService serviceUserInterface,
            NavigationKey key
        ) {
            serviceUserInterface.AddNavigationReverseKey(key);
        }
        
        /// <summary>Removes a forward-navigation key.</summary>
        /// <param name="serviceUserInterface">The user-interface service to configure.</param>
        /// <param name="key">The key to remove.</param>
        public static void RemoveTabForwardKey(
            this IUserInterfaceService serviceUserInterface,
            NavigationKey key
        ) {
            serviceUserInterface.RemoveNavigationForwardKey(key);
        }
        
        /// <summary>Removes a reverse-navigation key.</summary>
        /// <param name="serviceUserInterface">The user-interface service to configure.</param>
        /// <param name="key">The key to remove.</param>
        public static void RemoveTabReverseKey(
            this IUserInterfaceService serviceUserInterface,
            NavigationKey key
        ) {
            serviceUserInterface.RemoveNavigationReverseKey(key);
        }
    }
}
