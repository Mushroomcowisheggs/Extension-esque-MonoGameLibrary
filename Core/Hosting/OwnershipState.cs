namespace MonoGameLibrary.Core.Hosting {
    /// <summary>
    /// Who owns one participant object at the host's boundary.
    /// </summary>
    /// <remarks>
    /// One bit cannot say all of it. A participant can leave membership without being released, and
    /// it can be admitted again in that state; it can be released, after which no admission follows;
    /// and a retained failure can hand it to the caller, after which the host must not release it.
    /// The ledger records which of those happened, so "released once" is a property of the object
    /// rather than of one admission.
    /// </remarks>
    internal enum OwnershipState {
        /// <summary>The host never recorded the object; the first release or admission claims it. </summary>
        Absent = 0,
        
        /// <summary>The host owns the object and may release it at most once. </summary>
        Owned = 1,
        
        /// <summary>
        /// A retained failure transferred the object to the caller; the host will not release it.
        /// </summary>
        Retained = 2,
        
        /// <summary>The host has released the object. The state is terminal. </summary>
        Released = 3
    }
}
