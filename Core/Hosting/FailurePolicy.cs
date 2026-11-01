namespace MonoGameLibrary.Core.Hosting {
    /// <summary>
    /// Selects what a failed composition or initialization does with the resources it accepted.
    /// The policy is required and has no implicit default.
    /// </summary>
    public enum FailurePolicy {
        /// <summary>
        /// No policy has been selected; <see cref="GameBuilder.Build()"/> rejects this state.
        /// </summary>
        Unspecified = 0,
        
        /// <summary>
        /// The failing component releases every resource it accepted before rethrowing.
        /// </summary>
        Release = 1,
        
        /// <summary>
        /// The failing component retains every resource it accepted and exposes it through
        /// <see cref="LifecycleFailure.RetainedObjects"/>; the caller owns the retained resources.
        /// </summary>
        Retain = 2
    }
}
