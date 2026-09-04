using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Extensions.Graphics {
    /// <summary>Represents texture regions displayed over time.</summary>
    public sealed class Animation {
        public List<TextureRegion> Frames { get; set; } = new List<TextureRegion>();
        public TimeSpan Delay { get; set; } = TimeSpan.FromMilliseconds(100);
    }
}
