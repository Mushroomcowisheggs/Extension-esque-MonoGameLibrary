using System;
using System.Collections.Generic;
using MonoGameLibrary.Core.Diagnostics;

namespace MonoGameLibrary.Utilities {
    /// <summary>
    /// Utility for detecting resource leaks in game application code.
    /// Designed for development and testing assistance.
    /// Not intended for framework modules.
    /// </summary>
    public static class ResourceLeakDetector {
        private static readonly HashSet<WeakReference> _resourcesTracked;
        
        static ResourceLeakDetector() {
            _resourcesTracked = new HashSet<WeakReference>();
        }
        
        /// <summary>
        /// Tracks an IDisposable resource for leak detection.
        /// </summary>
        /// <param name="resource">The resource to track.</param>
        public static void Track(IDisposable resource) {
            if (resource == null) {
                return;
            }
            _resourcesTracked.Add(new WeakReference(resource));
        }
        
        /// <summary>
        /// Reports all resources that have not been released.
        /// </summary>
        /// <param name="logger">The logger to report to.</param>
        public static void ReportLeaks(ILogger logger) {
            if (logger == null) {
                return;
            }
            
            int countLeak = 0;
            foreach (WeakReference reference in _resourcesTracked) {
                if (reference.IsAlive) {
                    countLeak += 1;
                }
            }
            
            if (countLeak > 0) {
                logger.Warning($"ResourceLeakDetector: {countLeak} resources may have leaked.");
            }
            
            _resourcesTracked.Clear();
        }
    }
}