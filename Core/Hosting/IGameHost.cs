using System;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Core.Hosting {
    /// <summary>
    /// Represents a game host that manages the lifecycle of game modules. 
    /// </summary>
    public interface IGameHost : IDisposable {
        /// <summary>
        /// Gets or sets a callback that is invoked when an unhandled exception occurs in any module. 
        /// If not set, exceptions are rethrown. 
        /// </summary>
        Action<Exception, string> OnError { get; set; }
        
        IServiceRegistry Services { get; }
        
        /// <summary>
        /// Adds a module to the host. Modules may be added at any time, before or after 
        /// <see cref="Initialize"/>. A module added before initialization is loaded by 
        /// <see cref="Initialize"/>. A module added afterwards (or concurrently with loading) 
        /// is loaded immediately: its <c>LoadContent</c> runs before this method 
        /// returns. If that late load fails, the module is removed again and the exception is 
        /// rethrown; the host itself stays initialized. Modules are picked up by 
        /// <see cref="Update"/>/<see cref="Draw"/> on the next frame. 
        /// </summary>
        /// <param name="module">The module to add. </param>
        void AddModule(object module);
        
        void Initialize(IContentService serviceContent);
        
        void Update(FrameTime timeFrame);
        
        void Draw(FrameTime timeFrame);
    }
}