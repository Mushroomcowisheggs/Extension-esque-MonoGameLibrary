using System;

namespace MonoGameLibrary.Extensions.Screens {
    public static class ScreenServiceExtensions {
        /// <summary>Returns whether the service holds no screens.</summary>
        /// <param name="service">The service to inspect; null counts as empty.</param>
        public static bool IsEmpty(this IScreenService service) {
            return service == null || service.CurrentScreen == null;
        }
        
        /// <summary>Returns the runtime type of the top screen, or null when there is none.</summary>
        /// <param name="service">The service to inspect; null yields null.</param>
        public static Type GetCurrentScreenType(this IScreenService service) {
            if (service == null) {
                return null;
            }
            
            IScreen screenCurrent = service.CurrentScreen;
            if (screenCurrent == null) {
                return null;
            }
            
            return screenCurrent.GetType();
        }

        /// <summary>Returns whether the top screen is an instance of <typeparamref name="T"/>.</summary>
        /// <param name="service">The service to inspect; null yields false.</param>
        public static bool IsInScreen<T>(this IScreenService service) where T : Screen {
            return service != null && service.CurrentScreen is T;
        }
    }
}