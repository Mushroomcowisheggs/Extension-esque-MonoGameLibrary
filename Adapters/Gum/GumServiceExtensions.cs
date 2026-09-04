using MonoGameLibrary.Extensions.UserInterface;

namespace MonoGameLibrary.Adapters.Gum {
    /// <summary>Compatibility names for Gum tab navigation configuration.</summary>
    public static class GumServiceExtensions {
        public static void AddTabForwardKey(
            this IUserInterfaceService serviceUserInterface,
            NavigationKey key
        ) {
            serviceUserInterface.AddNavigationForwardKey(key);
        }
        
        public static void AddTabReverseKey(
            this IUserInterfaceService serviceUserInterface,
            NavigationKey key
        ) {
            serviceUserInterface.AddNavigationReverseKey(key);
        }
        
        public static void RemoveTabForwardKey(
            this IUserInterfaceService serviceUserInterface,
            NavigationKey key
        ) {
            serviceUserInterface.RemoveNavigationForwardKey(key);
        }
        
        public static void RemoveTabReverseKey(
            this IUserInterfaceService serviceUserInterface,
            NavigationKey key
        ) {
            serviceUserInterface.RemoveNavigationReverseKey(key);
        }
    }
}
