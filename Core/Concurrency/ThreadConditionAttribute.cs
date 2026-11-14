using System;

namespace MonoGameLibrary.Core.Concurrency {
    /// <summary>
    /// Declares the threading condition of a contract or of one of its members. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// A member without its own attribute inherits the condition of its declaring type, so a contract
    /// states one condition for the common case and names only its exceptions. 
    /// </para>
    /// <para>
    /// The check portfolio's P4-08 rule reads this attribute: every public contract member must
    /// resolve to a condition, the value must be one of <see cref="ThreadAffinity"/>'s, and a derived
    /// contract must not widen the threads its base contracts state. 
    /// </para>
    /// </remarks>
    [AttributeUsage(
        AttributeTargets.Interface | AttributeTargets.Class |
        AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event,
        AllowMultiple = false,
        Inherited = true
    )]
    public sealed class ThreadConditionAttribute : Attribute {
        /// <summary>
        /// Creates the attribute for one condition. 
        /// </summary>
        /// <param name="affinity">The threads the annotated contract or member may be called from. </param>
        public ThreadConditionAttribute(ThreadAffinity affinity) {
            Affinity = affinity;
        }
        
        /// <summary>
        /// Gets the declared condition. 
        /// </summary>
        public ThreadAffinity Affinity {
            get;
        }
    }
}
