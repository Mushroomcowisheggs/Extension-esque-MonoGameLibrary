using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Core.Hosting {
    /// <summary>
    /// Shared release routine for participants and content. Every release elects through the
    /// ownership ledger, so an object is released at most once for the life of the owner; this type
    /// also guarantees per-object isolation and reverse order.
    /// </summary>
    internal static class OwnershipRelease {
        /// <summary>
        /// Elects and performs one release when the object is disposable.
        /// </summary>
        /// <param name="module">The owned object. </param>
        /// <param name="report">Receives a release failure or a refused election without propagating
        /// it. </param>
        /// <param name="context">The operation and object context. </param>
        /// <param name="ledgerOwnership">The ledger that decides whether this attempt is the first. </param>
        public static void ReleaseModule(
            object module,
            Action<Exception, string> report,
            string context,
            OwnershipLedger ledgerOwnership
        ) {
            if (module == null) {
                throw new ArgumentNullException(nameof(module));
            }
            if (report == null) {
                throw new ArgumentNullException(nameof(report));
            }
            if (ledgerOwnership == null) {
                throw new ArgumentNullException(nameof(ledgerOwnership));
            }
            
            OwnershipState statePrevious;
            if (!ledgerOwnership.TryElectRelease(module, out statePrevious)) {
                // A second release is reported rather than repeated. With the admission guard in
                // place this branch means a release path that bypassed the host, not a re-admission.
                report(
                    new InvalidOperationException(
                        $"The release of {module.GetType().FullName} was refused because " +
                        $"{OwnershipLedger.Describe(statePrevious)}. "
                    ),
                    context
                );
                return;
            }
            
            IDisposable disposable = module as IDisposable;
            if (disposable == null) {
                return;
            }
            try {
                disposable.Dispose();
            } catch (Exception exception) {
                report(exception, $"{context} (Module: {module.GetType().Name})");
            }
        }
        
        /// <summary>
        /// Releases owned objects in reverse order, attempting every release and electing each one.
        /// </summary>
        /// <param name="modules">The owned objects in acquisition order. </param>
        /// <param name="report">Receives one failure per object without propagating it. </param>
        /// <param name="context">The operation context. </param>
        /// <param name="ledgerOwnership">The ledger that decides whether each attempt is the first. </param>
        public static void ReleaseModules(
            IEnumerable<object> modules,
            Action<Exception, string> report,
            string context,
            OwnershipLedger ledgerOwnership
        ) {
            if (modules == null) {
                throw new ArgumentNullException(nameof(modules));
            }
            List<object> listSnapshot = new List<object>(modules);
            for (int index = listSnapshot.Count - 1; index >= 0; index -= 1) {
                ReleaseModule(listSnapshot[index], report, context, ledgerOwnership);
            }
        }
    }
}
