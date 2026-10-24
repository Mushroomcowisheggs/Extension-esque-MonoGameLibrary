using System;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Core.Hosting {
    /// <summary>
    /// Represents a game host that manages the lifecycle of game modules. 
    /// The host exposes no service registry on purpose: capabilities are resolved once, at the
    /// composition root, and a participant receives them through constructor injection. 
    /// </summary>
    public interface IGameHost : IDisposable {
        /// <summary>
        /// Gets or sets a callback that is invoked when an unhandled exception occurs in any module. 
        /// If not set, exceptions are rethrown. 
        /// </summary>
        Action<Exception, string> OnError { get; set; }
        
        /// <summary>
        /// Adds a module to the host. Modules may be added at any time, before or after 
        /// <see cref="Initialize"/>. A module added before initialization is loaded by 
        /// <see cref="Initialize"/>. A module added afterwards is loaded immediately: its
        /// <c>LoadContent</c> runs before this method returns, and the admission is a tracked host
        /// operation, so a disposal in progress waits for that load to finish. If the late load fails,
        /// the module is removed again and the exception is rethrown; the host itself stays initialized,
        /// and it does not dispose the withdrawn module. The caller received the exception and still
        /// holds the reference, so releasing what the failed load acquired is the caller's decision.
        /// Modules are picked up by <see cref="Update"/>/<see cref="Draw"/> on the next frame. 
        /// </summary>
        /// <param name="module">The module to add. </param>
        void AddModule(object module);
        
        void Initialize(IContentService serviceContent);
        
        void Update(FrameTime timeFrame);
        
        void Draw(FrameTime timeFrame);
        
        /// <summary>
        /// Marks both schedules stale so the next <see cref="Update"/> and <see cref="Draw"/> rebuild
        /// them from the current membership and the current <c>Order</c> values. 
        /// </summary>
        /// <remarks>
        /// The host caches the sorted order and rebuilds it when membership changes. A participant that
        /// changes its <c>Order</c> at runtime must call this method: no setter exists on the lifecycle
        /// interfaces, so the host cannot observe the change. Order should be fixed at construction;
        /// this is the supported route for the exceptional case.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown if the host has been disposed or is being disposed. </exception>
        void InvalidateSchedule();
    }
}
