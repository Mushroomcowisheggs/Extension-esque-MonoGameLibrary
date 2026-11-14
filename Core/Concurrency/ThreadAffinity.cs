namespace MonoGameLibrary.Core.Concurrency {
    /// <summary>
    /// States the threads from which a contract's members may be called. 
    /// </summary>
    public enum ThreadAffinity {
        /// <summary>
        /// Any thread. 
        /// </summary>
        Any = 0,
        
        /// <summary>
        /// One frame thread. Every call must come from the thread that drives the frame. 
        /// </summary>
        Frame = 1,
        
        /// <summary>
        /// Not thread-safe. The caller must serialize the calls; a single thread is the normal case. 
        /// </summary>
        Single = 2,
        
        /// <summary>
        /// Runs on the caller's thread, so a callback, a load or a disposal inherits the threading of
        /// whoever invoked it. 
        /// </summary>
        Caller = 3
    }
}
