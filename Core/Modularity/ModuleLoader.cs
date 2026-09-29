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
        /// Discovers the modules marked with <see cref="ModuleRegistrationAttribute"/> in the
        /// managed assemblies found in <paramref name="path"/>, sorts them deterministically,
        /// and registers them. A constructor parameter without a default value is resolved from
        /// the services already registered on the builder; a module whose dependencies cannot be
        /// satisfied is reported and skipped instead of aborting the application.
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
            
            int countRegistered = 0;
            foreach (Type typeModule in typesModule) {
                if (RegisterModule(builder, typeModule, loggerResolved)) {
                    countRegistered += 1;
                }
            }
            
            loggerResolved.Info(
                $"ModuleLoader: Scan completed; registered {countRegistered} of " +
                $"{typesModule.Count} module(s)."
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
                    
                    // Only modules that opt in are created here. Modules whose services are built
                    // by their own builder extension (the networking modules, for example) are
                    // registered explicitly and must not be constructed by the loader.
                    if (type.GetCustomAttribute<ModuleRegistrationAttribute>() == null) {
                        logger.Info(
                            $"ModuleLoader: Skipping module '{type.FullName}' (no " +
                            "[ModuleRegistration] attribute; it is registered explicitly)."
                        );
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
        
        private static bool RegisterModule(
            GameBuilder builder,
            Type typeModule,
            ILogger logger
        ) {
            object moduleInstance;
            List<string> dependenciesMissing;
            if (!TryCreateModule(builder, typeModule, out moduleInstance, out dependenciesMissing)) {
                logger.Warning(
                    $"ModuleLoader: Skipped module '{typeModule.FullName}' because it cannot be " +
                    "constructed. Register the missing service(s) before calling " +
                    "LoadModulesFrom(), or register the module explicitly: " +
                    string.Join(", ", dependenciesMissing) + "."
                );
                return false;
            }
            
            IModule module = moduleInstance as IModule;
            if (module == null) {
                throw new InvalidOperationException(
                    $"Type '{typeModule.FullName}' did not create an IModule instance."
                );
            }
            
            try {
                module.Register(builder);
                logger.Info(
                    $"ModuleLoader: Successfully registered module '{typeModule.FullName}'."
                );
                return true;
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
        
        private static bool TryCreateModule(
            GameBuilder builder,
            Type typeModule,
            out object moduleInstance,
            out List<string> dependenciesMissing
        ) {
            ConstructorInfo[] constructors = typeModule.GetConstructors();
            Array.Sort(
                constructors,
                delegate(ConstructorInfo left, ConstructorInfo right) {
                    return left.GetParameters().Length.CompareTo(
                        right.GetParameters().Length
                    );
                }
            );
            
            dependenciesMissing = new List<string>();
            foreach (ConstructorInfo constructor in constructors) {
                ParameterInfo[] parameters = constructor.GetParameters();
                bool flagCanInvoke = true;
                object[] arguments = new object[parameters.Length];
                for (int index = 0; index < parameters.Length; index += 1) {
                    ParameterInfo parameter = parameters[index];
                    object valueResolved;
                    if (TryResolveService(builder, parameter.ParameterType, out valueResolved)) {
                        arguments[index] = valueResolved;
                    } else if (parameter.HasDefaultValue) {
                        arguments[index] = parameter.DefaultValue;
                    } else {
                        flagCanInvoke = false;
                        dependenciesMissing.Add(
                            $"{parameter.ParameterType.FullName} ({parameter.Name})"
                        );
                    }
                }
                if (flagCanInvoke) {
                    moduleInstance = constructor.Invoke(arguments);
                    return true;
                }
            }
            
            moduleInstance = null;
            return false;
        }
        
        private static bool TryResolveService(
            GameBuilder builder,
            Type typeService,
            out object instance
        ) {
            instance = null;
            if (typeService == null) {
                return false;
            }
            // The service registry stores reference types only (`where TService : class`), so
            // value types such as int or Optional<T> fall back to their declared default value.
            if (!typeService.IsClass && !typeService.IsInterface) {
                return false;
            }
            
            MethodInfo infoTryGetMethod = typeof(GameBuilder).GetMethod(
                nameof(GameBuilder.TryGetService)
            );
            if (infoTryGetMethod == null) {
                return false;
            }
            
            MethodInfo infoClosedMethod;
            try {
                infoClosedMethod = infoTryGetMethod.MakeGenericMethod(typeService);
            } catch (ArgumentException) {
                return false;
            }
            
            object[] arguments = new object[] { null };
            bool flagFound = (bool)infoClosedMethod.Invoke(builder, arguments);
            if (!flagFound) {
                return false;
            }
            
            instance = arguments[0];
            return instance != null;
        }
    }
}
