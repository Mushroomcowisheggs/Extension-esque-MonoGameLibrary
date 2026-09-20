using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;

namespace MonoGameLibrary.Adapters.MonoGame.Lifecycle {
    /// <summary>
    /// Puts the title on the operating-system window.
    /// </summary>
    /// <remarks>
    /// MonoGame's DesktopGL platform hands the title to SDL through <c>Marshal.StringToHGlobalAnsi</c>, which
    /// encodes it with the process ANSI code page, while SDL reads it back as UTF-8. Any title outside that code
    /// page — every Chinese, Japanese or Korean title on a machine whose code page is not UTF-8 — therefore
    /// reaches the window as a row of placeholder characters. The title is set again here through the Unicode
    /// entry point of the platform, which takes the string exactly as it is.
    /// </remarks>
    internal static class WindowTitle {
        /// <summary>The window class SDL registers for the window it creates.</summary>
        private const string WindowClassName = "SDL_app";
        
        /// <summary>
        /// Applies a title to a window, through the platform when it offers a Unicode entry point.
        /// </summary>
        /// <param name="window">The window to title.</param>
        /// <param name="title">The title to show.</param>
        public static void Apply(GameWindow window, string title) {
            if (window == null || string.IsNullOrEmpty(title)) {
                return;
            }
            // Keep MonoGame's own idea of the title in step, even where the platform path below does the work.
            window.Title = title;
            if (!OperatingSystem.IsWindows()) {
                return;
            }
            IntPtr handle = FindOwnWindow();
            if (handle == IntPtr.Zero) {
                return;
            }
            SetWindowTextW(handle, title);
        }
        
        /// <summary>
        /// Finds the window this process is drawing in. MonoGame's <see cref="GameWindow.Handle"/> is the SDL
        /// window object rather than the platform window, so the platform handle is looked up by owner and class.
        /// </summary>
        /// <returns>The platform window handle, or <see cref="IntPtr.Zero"/> when there is none yet.</returns>
        private static IntPtr FindOwnWindow() {
            uint process = (uint)Environment.ProcessId;
            IntPtr found = IntPtr.Zero;
            EnumWindows(delegate(IntPtr candidate, IntPtr parameter) {
                uint owner;
                GetWindowThreadProcessId(candidate, out owner);
                if (owner != process) {
                    return true;
                }
                StringBuilder name = new StringBuilder(64);
                if (GetClassNameW(candidate, name, name.Capacity) == 0 || name.ToString() != WindowClassName) {
                    return true;
                }
                found = candidate;
                return false;
            }, IntPtr.Zero);
            return found;
        }
        
        private delegate bool EnumWindowsCallback(IntPtr window, IntPtr parameter);
        
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);
        
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
        
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassNameW(IntPtr window, StringBuilder name, int count);
        
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetWindowTextW(IntPtr window, string text);
    }
}
