using System;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Graphics {
    /// <summary>A sprite that automatically advances through an animation.</summary>
    public sealed class AnimatedSprite : Sprite {
        private int _frameCurrent;
        private TimeSpan _spanElapsed;
        public Animation Animation { get; set; }
        
        public AnimatedSprite() {
        }
        
        public AnimatedSprite(Animation animation) {
            Animation = animation;
            if (animation != null && animation.Frames != null && animation.Frames.Count > 0) {
                Region = animation.Frames[0];
            }
        }
        
        public void Update(FrameTime timeFrame) {
            if (Animation == null || Animation.Frames.Count <= 1) {
                return;
            }
            _spanElapsed += timeFrame.DeltaTimeSpan;
            if (_spanElapsed >= Animation.Delay) {
                _spanElapsed = TimeSpan.Zero;
                _frameCurrent = (_frameCurrent + 1) % Animation.Frames.Count;
                Region = Animation.Frames[_frameCurrent];
            }
        }
    }
}
