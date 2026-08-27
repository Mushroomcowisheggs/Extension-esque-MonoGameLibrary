using System;
using System.IO;
using System.Reflection;
using MonoGameLibrary.Core.Diagnostics;
using MonoGameLibrary.Core.Hosting;

namespace MonoGameLibrary.Core.Modularity {
    /// <summary>
    /// Provides extension methods for discovering and loading modules
    /// from the file system at runtime. 
    /// </summary>
    public static class ModuleLoader {
        /// <summary>
        /// Scans the specified directory for assemblies, loads each assembly, 
        /// and instantiates all types that implement <see cref="IModule"/>. 
        /// For each discovered module, <see cref="IModule.Register"/> is invoked
        /// with the current builder. All operations are logged via the provided logger. 
        /// </summary>
        /// <param name="builder">The game builder to configure. </param>
        /// <param name="path">The directory path to scan for .dll files. </param>
        /// <param name="logger">The logger to use for diagnostic output. If null, <see cref="NullLogger"/> is used. </param>
        /// <returns>The same builder instance for chaining. </returns>
        /// <exception cref="ArgumentNullException">Thrown if builder or path is null. </exception>
        /// <exception cref="DirectoryNotFoundException">Thrown if the specified directory does not exist. </exception>
        public static GameBuilder LoadModulesFrom(
            this GameBuilder builder,
            string path,
            Optional<ILogger> logger = default
        ) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            if (string.IsNullOrWhiteSpace(path)) {
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            }
            if (!Directory.Exists(path)) {
                throw new DirectoryNotFoundException($"Directory not found: {path}");
            }
            
            ILogger loggerResolved = logger.HasValue ? logger.Value : NullLogger.Instance;
            
            loggerResolved.Info($"ModuleLoader: Scanning directory '{path}' for modules...");
            
            foreach (string dllPath in Directory.GetFiles(path, "*.dll")) {
                loggerResolved.Debug($"ModuleLoader: Processing file '{dllPath}'");
                
                try {
                    Assembly assembly = Assembly.LoadFrom(dllPath);
                    bool flagAnyModuleFound = false;
                    
                    foreach (Type type in assembly.GetTypes()) {
                        if (!typeof(IModule).IsAssignableFrom(type) || type.IsAbstract || !type.IsClass) {
                            continue;
                        }
                        
                        flagAnyModuleFound = true;
                        loggerResolved.Debug($"ModuleLoader: Found module type '{type.FullName}' in '{dllPath}'");
                        
                        try {
                            IModule module = (IModule)Activator.CreateInstance(type);
                            module.Register(builder);
                            loggerResolved.Info($"ModuleLoader: Successfully registered module '{type.FullName}'.");
                        } catch (Exception exception) {
                            loggerResolved.Error($"ModuleLoader: Failed to register module '{type.FullName}'", exception);
                        }
                    }
                    
                    if (!flagAnyModuleFound) {
                        loggerResolved.Debug($"ModuleLoader: No IModule implementations found in '{dllPath}'.");
                    }
                } catch (Exception exception) {
                    loggerResolved.Error($"ModuleLoader: Failed to load assembly '{dllPath}'", exception);
                }
            }
            
            loggerResolved.Info($"ModuleLoader: Scan completed.");
            return builder;
        }
    }
}