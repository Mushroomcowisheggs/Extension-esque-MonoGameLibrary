using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MonoGameLibrary.Core.Diagnostics;
using MonoGameLibrary.Core.Hosting;

namespace MonoGameLibrary.Core.Modularity {
    /// <summary>Discovers and registers modules from managed assemblies.</summary>
    public static class ModuleLoader {
        /// <summary>
        /// Discovers modules with a parameterless or all-optional constructor,
        /// sorts them deterministically, and registers them.
        /// </summary>
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
            
            ILogger loggerResolved = logger.HasValue
                ? logger.Value
                : NullLogger.Instance;
            loggerResolved.Info(
                $"ModuleLoader: Scanning directory '{path}' for modules..."
            );
            
            List<Type> typesModule = new List<Type>();
            HashSet<string> typesIdentities = new HashSet<string>(
                StringComparer.Ordinal
            );
            string[] pathsAssembly = Directory.GetFiles(path, "*.dll");
            Array.Sort(pathsAssembly, StringComparer.Ordinal);
            
            foreach (string pathAssembly in pathsAssembly) {
                DiscoverAssemblyModules(
                    pathAssembly,
                    typesModule,
                    typesIdentities,
                    loggerResolved
                );
            }
            
            typesModule.Sort(delegate(Type left, Type right) {
                int comparison = GetRegistrationOrder(left).CompareTo(
                    GetRegistrationOrder(right)
                );
                if (comparison != 0) {
                    return comparison;
                }
                return string.Compare(
                    left.FullName,
                    right.FullName,
                    StringComparison.Ordinal
                );
            });
            
            foreach (Type typeModule in typesModule) {
                RegisterModule(builder, typeModule, loggerResolved);
            }
            
            loggerResolved.Info(
                $"ModuleLoader: Scan completed; registered {typesModule.Count} module(s)."
            );
            return builder;
        }
        
        private static void DiscoverAssemblyModules(
            string pathAssembly,
            List<Type> typesModule,
            HashSet<string> typesIdentities,
            ILogger logger
        ) {
            try {
                Assembly assembly = Assembly.LoadFrom(pathAssembly);
                Type[] types;
                try {
                    types = assembly.GetTypes();
                } catch (ReflectionTypeLoadException exception) {
                    types = exception.Types;
                    logger.Warning(
                        $"ModuleLoader: Some types could not be loaded from '{pathAssembly}'."
                    );
                }
                
                foreach (Type type in types) {
                    if (
                        type == null ||
                        !typeof(IModule).IsAssignableFrom(type) ||
                        type.IsAbstract ||
                        !type.IsClass
                    ) {
                        continue;
                    }
                    
                    string identity = type.AssemblyQualifiedName;
                    if (identity == null) {
                        identity = type.FullName;
                    }
                    if (identity == null || !typesIdentities.Add(identity)) {
                        continue;
                    }
                    
                    typesModule.Add(type);
                    logger.Debug(
                        $"ModuleLoader: Discovered module '{type.FullName}'."
                    );
                }
            } catch (BadImageFormatException) {
                logger.Debug(
                    $"ModuleLoader: Skipped native assembly '{pathAssembly}'."
                );
            } catch (FileLoadException exception) {
                logger.Warning(
                    $"ModuleLoader: Could not load candidate assembly '{pathAssembly}': " +
                    exception.Message
                );
            } catch (FileNotFoundException exception) {
                logger.Warning(
                    $"ModuleLoader: Dependency missing while loading '{pathAssembly}': " +
                    exception.Message
                );
            }
        }
        
        private static int GetRegistrationOrder(Type typeModule) {
            ModuleRegistrationAttribute attribute =
                typeModule.GetCustomAttribute<ModuleRegistrationAttribute>();
            return attribute == null ? 0 : attribute.Order;
        }
        
        private static void RegisterModule(
            GameBuilder builder,
            Type typeModule,
            ILogger logger
        ) {
            try {
                object instance = CreateModule(typeModule);
                IModule module = instance as IModule;
                if (module == null) {
                    throw new InvalidOperationException(
                        $"Type '{typeModule.FullName}' did not create an IModule instance."
                    );
                }
                
                module.Register(builder);
                logger.Info(
                    $"ModuleLoader: Successfully registered module '{typeModule.FullName}'."
                );
            } catch (Exception exception) {
                logger.Error(
                    $"ModuleLoader: Failed to register module '{typeModule.FullName}'.",
                    exception
                );
                throw new InvalidOperationException(
                    $"Automatic module registration failed for '{typeModule.FullName}'.",
                    exception
                );
            }
        }
        
        private static object CreateModule(Type typeModule) {
            ConstructorInfo[] constructors = typeModule.GetConstructors();
            Array.Sort(
                constructors,
                delegate(ConstructorInfo left, ConstructorInfo right) {
                    return left.GetParameters().Length.CompareTo(
                        right.GetParameters().Length
                    );
                }
            );
            
            foreach (ConstructorInfo constructor in constructors) {
                ParameterInfo[] parameters = constructor.GetParameters();
                bool flagCanInvoke = true;
                object[] arguments = new object[parameters.Length];
                for (int index = 0; index < parameters.Length; index += 1) {
                    if (!parameters[index].HasDefaultValue) {
                        flagCanInvoke = false;
                        break;
                    }
                    arguments[index] = parameters[index].DefaultValue;
                }
                if (flagCanInvoke) {
                    return constructor.Invoke(arguments);
                }
            }
            
            throw new InvalidOperationException(
                $"Module '{typeModule.FullName}' must expose a public parameterless " +
                "constructor or a public constructor whose parameters are all optional."
            );
        }
    }
}
