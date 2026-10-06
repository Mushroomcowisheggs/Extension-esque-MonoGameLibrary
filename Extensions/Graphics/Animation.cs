using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Extensions.Graphics {
    /// <summary>Represents texture regions displayed over time.</summary>
    public sealed class Animation {
        /// <summary>Gets or sets the regions displayed in order. The list is never null, but may be empty.</summary>
        public List<TextureRegion> Frames { get; set; } = new List<TextureRegion>();
        /// <summary>Gets or sets how long each frame is displayed.</summary>
        public TimeSpan Delay { get; set; } = TimeSpan.FromMilliseconds(100);
    }
}
