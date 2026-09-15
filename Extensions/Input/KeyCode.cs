using System;

namespace MonoGameLibrary.Extensions.Input {
    /// <summary>
    /// Platform‑independent key codes used for input handling. 
    /// </summary>
    public enum KeyCode {
        /// <summary>No key pressed.</summary>
        None = 0, 
        /// <summary>A key.</summary>
        A,
        /// <summary>B key.</summary>
        B,
        /// <summary>C key.</summary>
        C,
        /// <summary>D key.</summary>
        D,
        /// <summary>E key.</summary>
        E,
        /// <summary>F key.</summary>
        F,
        /// <summary>G key.</summary>
        G,
        /// <summary>H key.</summary>
        H,
        /// <summary>I key.</summary>
        I,
        /// <summary>J key.</summary>
        J,
        /// <summary>K key.</summary>
        K,
        /// <summary>L key.</summary>
        L,
        /// <summary>M key.</summary>
        M,
        /// <summary>N key.</summary>
        N,
        /// <summary>O key.</summary>
        O,
        /// <summary>P key.</summary>
        P,
        /// <summary>Q key.</summary>
        Q,
        /// <summary>R key.</summary>
        R,
        /// <summary>S key.</summary>
        S,
        /// <summary>T key.</summary>
        T,
        /// <summary>U key.</summary>
        U,
        /// <summary>V key.</summary>
        V,
        /// <summary>W key.</summary>
        W,
        /// <summary>X key.</summary>
        X,
        /// <summary>Y key.</summary>
        Y,
        /// <summary>Z key.</summary>
        Z,
        
        /// <summary>Space bar.</summary>
        Space,
        /// <summary>Enter/Return key.</summary>
        Enter,
        /// <summary>Escape key.</summary>
        Escape,
        /// <summary>Tab key.</summary>
        Tab,
        /// <summary>Backspace key.</summary>
        Backspace,
        
        /// <summary>Up arrow.</summary>
        Up,
        /// <summary>Down arrow.</summary>
        Down,
        /// <summary>Left arrow.</summary>
        Left,
        /// <summary>Right arrow.</summary>
        Right,
        
        /// <summary>F1 function key.</summary>
        F1,
        /// <summary>F2 function key.</summary>
        F2,
        /// <summary>F3 function key.</summary>
        F3,
        /// <summary>F4 function key.</summary>
        F4,
        /// <summary>F5 function key.</summary>
        F5,
        /// <summary>F6 function key.</summary>
        F6,
        /// <summary>F7 function key.</summary>
        F7,
        /// <summary>F8 function key.</summary>
        F8,
        /// <summary>F9 function key.</summary>
        F9,
        /// <summary>F10 function key.</summary>
        F10,
        /// <summary>F11 function key.</summary>
        F11,
        /// <summary>F12 function key.</summary>
        F12,
        
        /// <summary>Left Shift key.</summary>
        LeftShift,
        /// <summary>Right Shift key.</summary>
        RightShift,
        /// <summary>Left Control key.</summary>
        LeftControl,
        /// <summary>Right Control key.</summary>
        RightControl,
        /// <summary>Left Alt key.</summary>
        LeftAlt,
        /// <summary>Right Alt key.</summary>
        RightAlt,
        /// <summary>Left system key (Windows key, Command key, or Super key).</summary>
        LeftSystem,
        /// <summary>Right system key (Windows key, Command key, or Super key).</summary>
        RightSystem,
        /// <summary>Caps Lock key.</summary>
        CapsLock,
        
        /// <summary>Top row digit 0.</summary>
        Number0,
        /// <summary>Top row digit 1.</summary>
        Number1,
        /// <summary>Top row digit 2.</summary>
        Number2,
        /// <summary>Top row digit 3.</summary>
        Number3,
        /// <summary>Top row digit 4.</summary>
        Number4,
        /// <summary>Top row digit 5.</summary>
        Number5,
        /// <summary>Top row digit 6.</summary>
        Number6,
        /// <summary>Top row digit 7.</summary>
        Number7,
        /// <summary>Top row digit 8.</summary>
        Number8,
        /// <summary>Top row digit 9.</summary>
        Number9,
        
        /// <summary>Insert key.</summary>
        Insert,
        /// <summary>Delete key.</summary>
        Delete,
        /// <summary>Home key.</summary>
        Home,
        /// <summary>End key.</summary>
        End,
        /// <summary>Page Up key.</summary>
        PageUp,
        /// <summary>Page Down key.</summary>
        PageDown,
        
        /// <summary>Minus key.</summary>
        Minus,
        /// <summary>Equals key.</summary>
        Equals,
        /// <summary>Left square bracket key.</summary>
        LeftBracket,
        /// <summary>Right square bracket key.</summary>
        RightBracket,
        /// <summary>Backslash key.</summary>
        Backslash,
        /// <summary>Semicolon key.</summary>
        Semicolon,
        /// <summary>Apostrophe key.</summary>
        Apostrophe,
        /// <summary>Grave accent key.</summary>
        Grave,
        /// <summary>Comma key.</summary>
        Comma,
        /// <summary>Period key.</summary>
        Period,
        /// <summary>Slash key.</summary>
        Slash
    }
}