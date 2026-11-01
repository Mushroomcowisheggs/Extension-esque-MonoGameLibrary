using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Core.Hosting {
    /// <summary>
    /// Shared release routine for participants and content. Callers guarantee that the release is
    /// entered once; this type guarantees per-object isolation and reverse order.
    /// </summary>
    internal static class OwnershipRelease {
        /// <summary>
        /// Releases one owned object when it is disposable.
        /// </summary>
        /// <param name="module">The owned object. </param>
        /// <param name="report">Receives a release failure without propagating it. </param>
        /// <param name="context">The operation and object context. </param>
        public static void ReleaseModule(
            object module,
            Action<Exception, string> report,
            string context
        ) {
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
        /// Releases owned objects in reverse order, attempting every release.
        /// </summary>
        /// <param name="modules">The owned objects in acquisition order. </param>
        /// <param name="report">Receives one failure per object without propagating it. </param>
        /// <param name="context">The operation context. </param>
        public static void ReleaseModules(
            IEnumerable<object> modules,
            Action<Exception, string> report,
            string context
        ) {
            List<object> listSnapshot = new List<object>(modules);
            for (int index = listSnapshot.Count - 1; index >= 0; index -= 1) {
                ReleaseModule(listSnapshot[index], report, context);
            }
        }
    }
}
