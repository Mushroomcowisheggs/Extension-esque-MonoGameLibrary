using System;

namespace MonoGameLibrary.Core.Modularity {
    /// <summary>
    /// Marks a module for automatic discovery and registration by <see cref="ModuleLoader"/>,
    /// and specifies its deterministic registration order. A module without this attribute is
    /// never created by the loader: it is registered explicitly, for example by the builder
    /// extension that also builds the services the module depends on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The value is not cosmetic. <see cref="ModuleLoader"/> constructs and registers the discovered
    /// modules in ascending order, so a module that provides a service must carry a lower value than
    /// the module that consumes it; the Gum bridge handshake between the input and Gum adapters is the
    /// worked example. Registration order is therefore a dependency channel, and a duplicate value
    /// hands the two modules' relative load order to the loader's tie-break on the full type name,
    /// which a rename would change, so a value must be unique inside its band.
    /// </para>
    /// <para>
    /// Reserved bands, so that a value also says which layer declares the module:
    /// <c>-500 .. -200</c> Adapters, <c>-199 .. -100</c> Extensions, <c>-99 .. -1</c> reserved for
    /// future library modules, <c>0</c> and above application modules. Values must be unique within a
    /// band.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ModuleRegistrationAttribute : Attribute {
        /// <summary>Gets the registration order. Lower values register first.</summary>
        public int Order { get; }
        
        /// <summary>Creates an ordering attribute.</summary>
        public ModuleRegistrationAttribute(int order = 0) { Order = order; }
    }
}
