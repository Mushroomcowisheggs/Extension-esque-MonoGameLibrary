using System;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Graphics {
    /// <summary>A sprite that automatically advances through an animation.</summary>
    public sealed class AnimatedSprite : Sprite {
        private int _frameCurrent;
        private TimeSpan _spanElapsed;
        /// <summary>Gets or sets the animation to advance through. A null animation leaves the region unchanged.</summary>
        public Animation Animation { get; set; }
        
        /// <summary>Initializes an animated sprite with no animation.</summary>
        public AnimatedSprite() {
        }
        
        /// <summary>Initializes an animated sprite that starts on the animation's first frame.</summary>
        /// <param name="animation">The animation to advance through; may be null.</param>
        public AnimatedSprite(Animation animation) {
            Animation = animation;
            if (animation != null && animation.Frames != null && animation.Frames.Count > 0) {
                Region = animation.Frames[0];
            }
        }
        
        /// <summary>Advances the animation when its delay has elapsed, wrapping to the first frame.</summary>
        /// <param name="timeFrame">The frame time whose delta is accumulated.</param>
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
