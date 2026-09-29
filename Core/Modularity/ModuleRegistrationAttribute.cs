using System;

namespace MonoGameLibrary.Core.Modularity {
    /// <summary>
    /// Marks a module for automatic discovery and registration by <see cref="ModuleLoader"/>,
    /// and specifies its deterministic registration order. A module without this attribute is
    /// never created by the loader: it is registered explicitly, for example by the builder
    /// extension that also builds the services the module depends on.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ModuleRegistrationAttribute : Attribute {
        /// <summary>Gets the registration order. Lower values register first.</summary>
        public int Order { get; }
        
        /// <summary>Creates an ordering attribute.</summary>
        public ModuleRegistrationAttribute(int order = 0) { Order = order; }
    }
}
