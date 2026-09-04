using System;

namespace MonoGameLibrary.Core.Modularity {
    /// <summary>Specifies deterministic automatic registration order for a module.</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ModuleRegistrationAttribute : Attribute {
        /// <summary>Gets the registration order. Lower values register first.</summary>
        public int Order { get; }
        
        /// <summary>Creates an ordering attribute.</summary>
        public ModuleRegistrationAttribute(int order = 0) { Order = order; }
    }
}
