using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;

namespace MonoGameLibrary.Adapters.MonoGame.Lifecycle {
    /// <summary>
    /// Puts the title on the operating-system window.
    /// </summary>
    /// <remarks>
    /// A title has to be written to the window here, because the framework applies the one given before
    /// the window exists through the process ANSI code page. The reach of that is any title carrying a
    /// character the page cannot represent: a non-Latin script on a Western page, Hangul or kana on a
    /// Chinese or Japanese one, emoji on all of them. Such characters reach the window as replacement
    /// bytes and question marks, and the damage is silent — the title simply looks wrong.
    /// 
    /// Measured on MonoGame.Framework.DesktopGL 3.8.4.1: a title set in the constructor is mangled,
    /// setting it again from Initialize does not repair it, and writing it through the Unicode entry
    /// point does. The framework keeps the correct string throughout — <see cref="GameWindow.Title"/>
    /// reads back intact — so what is broken is where the string is applied, not what it holds. Setting
    /// the title from the first Update also works; the Unicode entry point is used because it corrects
    /// the window that already exists instead of depending on when the framework next applies the value.
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
