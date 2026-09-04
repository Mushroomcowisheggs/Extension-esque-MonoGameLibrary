using System;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Scenes {
    /// <summary>
    /// Contract for a game scene managed by <see cref="SceneService"/>.
    /// </summary>
    public interface IScene : IDisposable {
        /// <summary>Gets the update/draw order (lower values execute earlier). </summary>
        int Order { get; }
        
        /// <summary>Gets or sets whether the scene updates. </summary>
        bool Enabled { get; set; }
        
        /// <summary>Gets or sets whether the scene draws. </summary>
        bool Visible { get; set; }
        
        /// <summary>Called once to load content. </summary>
        void LoadContent();
        
        /// <summary>Called after <see cref="LoadContent"/> to perform setup that depends on loaded assets. </summary>
        void Initialize();
        
        /// <summary>Updates the scene logic each frame. </summary>
        void Update(FrameTime timeFrame);
        
        /// <summary>Draws the scene each frame.</summary>
        void Draw(FrameTime timeFrame);
    }
}
