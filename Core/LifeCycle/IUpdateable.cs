using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Core.Lifecycle {
    /// <summary>
    /// Implemented by modules that need to be updated every frame.
    /// </summary>
    public interface IUpdateable {
        /// <summary>
        /// Called once per frame to update the module's logic.
        /// </summary>
        /// <param name="timeFrame">Timing information for the current frame.</param>
        void Update(FrameTime timeFrame);
        
        /// <summary>
        /// Gets the update order. Lower values update first.
        /// Participants that share a value are ordered by the order in which the composition root
        /// admitted them, never by their type name, so renaming a participant cannot move it. A tie
        /// is a reproducible fallback, not a way to express intent: give distinct values wherever
        /// the order matters.
        /// </summary>
        /// <remarks>
        /// The value must be fixed by construction. The host caches the sorted schedule and rebuilds it
        /// when membership changes, so a later change to a backing field is invisible until then. A
        /// participant that must change its order at runtime calls <c>IGameHost.InvalidateSchedule</c>
        /// after the change; that is the only supported route.
        /// </remarks>
        int Order { get; }
        
        /// <summary>
        /// Gets or sets whether the module is enabled for updating. If <c>false</c>, <see cref="Update"/> is skipped.
        /// The host only reads this flag; modules and the game layer are expected to change it at runtime
        /// (for example to pause a subsystem), so the contract exposes a setter.
        /// </summary>
        bool Enabled { get; set; }
    }
}
