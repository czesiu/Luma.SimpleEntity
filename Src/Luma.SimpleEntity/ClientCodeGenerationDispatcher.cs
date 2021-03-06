﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Hosting;
using Luma.SimpleEntity.Helpers;
using Luma.SimpleEntity.Server;
using Luma.SimpleEntity.Tools;
using Luma.SimpleEntity.Tools.SharedTypes;

namespace Luma.SimpleEntity
{
    /// <summary>
    /// Stateless dispatcher class that discovers and invokes the appropriate
    /// code generator for a specified <see cref="ClientCodeGenerationOptions"/>.
    /// </summary>
    /// <remarks>
    /// This class is <see cref="MarshalByRefObject"/> so that it can be invoked across
    /// AppDomain boundaries.</remarks>
    public class ClientCodeGenerationDispatcher : MarshalByRefObject, IRegisteredObject, IDisposable
    {
        // MEF composition container and part catalog, computed lazily and only once
        private CompositionContainer _compositionContainer;
        private ComposablePartCatalog _partCatalog;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientCodeGenerationDispatcher"/> class.
        /// </summary>
        public ClientCodeGenerationDispatcher()
        {
        }

        // MEF import of all code generators
        [ImportMany(typeof(IClientCodeGenerator))]
        public IEnumerable<Lazy<IClientCodeGenerator, ICodeGeneratorMetadata>> ClientCodeGenerators { get; set; }

        /// <summary>
        /// Generates client proxy source code using the generator specified by <paramref name="codeGeneratorName"/>.
        /// </summary>
        /// <param name="options">The options to use for code generation.</param>
        /// <param name="parameters">The parameters required to create the <see cref="ISharedCodeService"/>.</param>
        /// <param name="loggingService">The service to use for logging.</param>
        /// <param name="codeGeneratorName">Optional generator name.  A <c>null</c> or empty value will select the default generator.</param>
        /// <returns>The generated source code or <c>null</c> if none was generated.</returns>
        public string GenerateCode(ClientCodeGenerationOptions options, SharedCodeServiceParameters parameters, ILoggingService loggingService, string codeGeneratorName)
        {
            Debug.Assert(options != null, "options cannot be null");
            Debug.Assert(parameters != null, "parameters cannot be null");
            Debug.Assert(loggingService != null, "loggingService cannot be null");

            AppDomainUtilities.ConfigureAppDomain(options);

            using (SharedCodeService sharedCodeService = new SharedCodeService(parameters, loggingService))
            {
                CodeGenerationHost host = new CodeGenerationHost(loggingService, sharedCodeService);
                return this.GenerateCode(host, options, parameters.ServerAssemblies, codeGeneratorName);
            }
        }

        /// <summary>
        /// Generates client proxy source code using the specified <paramref name="codeGeneratorName"/> in the context
        /// of the specified <paramref name="host"/>.
        /// </summary>
        /// <param name="host">The host for code generation.</param>
        /// <param name="options">The options to use for code generation.</param>
        /// <param name="assembliesToLoad">The set of server assemblies to use for analysis and composition.</param>
        /// <param name="codeGeneratorName">Optional generator name.  A <c>null</c> or empty value will select the default generator.</param>
        /// <returns>The generated source code or <c>null</c> if none was generated.</returns>
        public string GenerateCode(ICodeGenerationHost host, ClientCodeGenerationOptions options, IEnumerable<string> assembliesToLoad, string codeGeneratorName)
        {
            Debug.Assert(host != null, "host cannot be null");
            Debug.Assert(options != null, "options cannot be null");
            Debug.Assert(assembliesToLoad != null, "assembliesToLoad cannot be null");

            var logger = host as ILogger;
            var catalog = new EntityCatalog(assembliesToLoad, logger);
            return this.GenerateCode(host, options, catalog, assembliesToLoad, codeGeneratorName);
        }



        /// <summary>
        /// Generates client proxy source code using the specified <paramref name="codeGeneratorName"/> in the context
        /// of the specified <paramref name="host"/>.
        /// </summary>
        /// <param name="host">The host for code generation.</param>
        /// <param name="options">The options to use for code generation.</param>
        /// <param name="entityTypes">The set of <see cref="Luma.Client.Entity"/> types for which to generate code.</param>
        /// <param name="compositionAssemblies">The optional set of assemblies to use to create the MEF composition container.</param>
        /// <param name="codeGeneratorName">Optional generator name.  A <c>null</c> or empty value will select the default generator.</param>
        /// <returns>The generated source code or <c>null</c> if none was generated.</returns>
        public string GenerateCode(ICodeGenerationHost host, ClientCodeGenerationOptions options, IEnumerable<Type> entityTypes, IEnumerable<string> compositionAssemblies, string codeGeneratorName)
        {
            Debug.Assert(host != null, "host cannot be null");
            Debug.Assert(options != null, "options cannot be null");
            Debug.Assert(entityTypes != null, "entityTypes cannot be null");

            ILogger logger = host as ILogger;
            var catalog = new EntityCatalog(entityTypes, logger);
            return this.GenerateCode(host, options, catalog, compositionAssemblies, codeGeneratorName);
        }

        /// <summary>
        /// Generates client proxy source code using the specified <paramref name="codeGeneratorName"/> in the context
        /// of the specified <paramref name="host"/>.
        /// </summary>
        /// <param name="host">The host for code generation.</param>
        /// <param name="options">The options to use for code generation.</param>
        /// <param name="catalog">The catalog containing the <see cref="Luma.Client.Entity"/> types.</param>
        /// <param name="compositionAssemblies">The optional set of assemblies to use to create the MEF composition container.</param>
        /// <param name="codeGeneratorName">Optional generator name.  A <c>null</c> or empty value will select the default generator.</param>
        /// <returns>The generated source code or <c>null</c> if none was generated.</returns>
        public string GenerateCode(ICodeGenerationHost host, ClientCodeGenerationOptions options, EntityCatalog catalog, IEnumerable<string> compositionAssemblies, string codeGeneratorName)
        {
            Debug.Assert(host != null, "host cannot be null");
            Debug.Assert(options != null, "options cannot be null");
            Debug.Assert(catalog != null, "catalog cannot be null");

            IEnumerable<EntityDescription> entityDescriptions = catalog.EntityDescriptions;
            IClientCodeGenerator proxyGenerator = FindCodeGenerator(host, options, compositionAssemblies, codeGeneratorName);
            string generatedCode = null;

            if (proxyGenerator != null)
            {
                try
                {
                    generatedCode = proxyGenerator.GenerateCode(host, entityDescriptions, options);
                }
                catch (Exception ex)
                {
                    // Fatal exceptions are never swallowed or processed
                    if (ex.IsFatal())
                    {
                        throw;
                    }

                    // Any exception from the code generator is caught and reported, otherwise it will
                    // hit the MSBuild backstop and report failure of the custom build task.
                    // It is acceptable to report this exception and "ignore" it because we
                    // are running in a separate AppDomain which will be torn down immediately
                    // after our return.
                    host.LogError(string.Format(CultureInfo.CurrentCulture,
                                                    Resource.CodeGenerator_Threw_Exception,
                                                    string.IsNullOrEmpty(codeGeneratorName) ? proxyGenerator.GetType().FullName : codeGeneratorName,
                                                    options.ClientProjectPath,
                                                    ex.Message));
                }
            }

            return generatedCode;
        }

        /// <summary>
        /// Locates and returns the <see cref="IClientCodeGenerator"/> to use to generate client proxies
        /// for the specified <paramref name="options"/>.
        /// </summary>
        /// <param name="host">The host for code generation.</param>
        /// <param name="options">The options to use for code generation.</param>
        /// <param name="compositionAssemblies">The optional set of assemblies to use to create the MEF composition container.</param>
        /// <param name="codeGeneratorName">Optional generator name.  A <c>null</c> or empty value will select the default generator.</param>
        /// <returns>The code generator to use, or <c>null</c> if a matching one could not be found.</returns>
        public IClientCodeGenerator FindCodeGenerator(ICodeGenerationHost host, ClientCodeGenerationOptions options, IEnumerable<string> compositionAssemblies, string codeGeneratorName)
        {
            Debug.Assert(host != null, "host cannot be null");
            Debug.Assert(options != null, "options cannot be null");

            if (string.IsNullOrEmpty(options.Language))
            {
                throw new ArgumentException(Resource.Null_Language_Property, "options");
            }

            IClientCodeGenerator generator = null;

            // Try to load the code generator directly if given an assembly qualified name.
            // We insist on at least one comma in the name to know this is an assembly qualified name.
            // Otherwise, we might succeed in loading a dotted name that happens to be in our assembly,
            // such as the default CodeDom generator.
            if (!string.IsNullOrEmpty(codeGeneratorName) && codeGeneratorName.Contains(','))
            {
                Type codeGeneratorType = Type.GetType(codeGeneratorName, /*throwOnError*/ false);
                if (codeGeneratorType != null)
                {
                    if (!typeof(IClientCodeGenerator).IsAssignableFrom(codeGeneratorType))
                    {
                        // If generator is of the incorrect type, we will still allow the MEF approach below
                        // to find a better one.   This path could be exercised by inadvertantly using a name
                        // that happened to load some random type that was not a code generator.
                        host.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.Code_Generator_Incorrect_Type, codeGeneratorName));
                    }
                    else
                    {
                        try
                        {
                            generator = Activator.CreateInstance(codeGeneratorType) as IClientCodeGenerator;
                        }
                        catch (Exception e)
                        {
                            // The open catch of Exception is acceptable because we unconditionally report
                            // the error and are running in a separate AppDomain.
                            if (e.IsFatal())
                            {
                                throw;
                            }
                            host.LogError(string.Format(CultureInfo.CurrentCulture, Resource.Code_Generator_Instantiation_Error, codeGeneratorName, e.Message));
                        }
                    }
                }
            }

            if (generator == null)
            {
                // Create the MEF composition container (once only) from the assemblies we are analyzing
                this.CreateCompositionContainer(compositionAssemblies, host as ILogger);

                // The following property is filled by MEF by the line above.
                if (this.ClientCodeGenerators != null && this.ClientCodeGenerators.Any())
                {
                    // Select only those registered for the required language
                    IEnumerable<Lazy<IClientCodeGenerator, ICodeGeneratorMetadata>> allImportsForLanguage =
                        this.ClientCodeGenerators.Where(i => string.Equals(options.Language, i.Metadata.Language, StringComparison.OrdinalIgnoreCase));

                    Lazy<IClientCodeGenerator, ICodeGeneratorMetadata> lazyImport = null;

                    // If client specified a specific generator, use that one.
                    // If it cannot be found, log an error to explain the problem.
                    // If multiple with that name are found, log an error and explain the problem.
                    // We consider this an error because the user has explicitly named a generator,
                    // meaning they would not expect the default to be used.
                    if (!string.IsNullOrEmpty(codeGeneratorName))
                    {
                        IEnumerable<Lazy<IClientCodeGenerator, ICodeGeneratorMetadata>> allImportsForLanguageAndName = allImportsForLanguage.Where(i => string.Equals(i.Metadata.GeneratorName, codeGeneratorName, StringComparison.OrdinalIgnoreCase));

                        int numberOfMatchingGenerators = allImportsForLanguageAndName.Count();

                        // No generator with that name was found.  Log an error and explain how to register one.
                        if (numberOfMatchingGenerators == 0)
                        {
                            host.LogError(string.Format(CultureInfo.CurrentCulture,
                                                        Resource.Code_Generator_Not_Found,
                                                        codeGeneratorName,
                                                        options.Language,
                                                        options.ServerProjectPath,
                                                        options.ClientProjectPath,
                                                        CodeDomClientCodeGenerator.GeneratorName));
                        }
                        else if (numberOfMatchingGenerators == 1)
                        {
                            // Exactly one was found -- take it
                            lazyImport = allImportsForLanguageAndName.First();
                        }
                        else
                        {
                            // Multiple with that name were found.  Explain how to remove some of them or
                            // explicitly name one.
                            StringBuilder sb = new StringBuilder();
                            foreach (var import in allImportsForLanguageAndName.OrderBy(i => i.Value.GetType().FullName))
                            {
                                sb.AppendLine("    " + import.Value.GetType().FullName);
                            }
                            host.LogError(string.Format(CultureInfo.CurrentCulture,
                                                        Resource.Multiple_Named_Code_Generators,
                                                        codeGeneratorName,
                                                        options.Language,
                                                        sb.ToString(),
                                                        options.ServerProjectPath,
                                                        options.ClientProjectPath,
                                                        allImportsForLanguageAndName.First().Value.GetType().AssemblyQualifiedName));
                        }
                    }
                    else
                    {
                        // We are here if no generator name was specified.
                        // If only one import matched the language, we have it.
                        // This is the most common path to discovery of our own CodeDom generator
                        // but will work equally well when it replaced.
                        if (allImportsForLanguage.Count() == 1)
                        {
                            lazyImport = allImportsForLanguage.First();
                        }
                        else
                        {
                            // Multiple custom generators exist, but a specific generator name was not provided.
                            // Look for any custom generators other than our default CodeDom one.
                            // If we find there is only one custom generator registered, we use that one rather than the default
                            IEnumerable<Lazy<IClientCodeGenerator, ICodeGeneratorMetadata>> customGeneratorImports =
                                allImportsForLanguage.Where(i => !string.Equals(CodeDomClientCodeGenerator.GeneratorName, i.Metadata.GeneratorName, StringComparison.OrdinalIgnoreCase));

                            int generatorCount = customGeneratorImports.Count();

                            // Exactly 1 custom generator that is not the default -- take it
                            if (generatorCount == 1)
                            {
                                lazyImport = customGeneratorImports.First();
                                host.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.Using_Custom_Code_Generator, lazyImport.Metadata.GeneratorName));
                            }
                            else if (generatorCount != 0)
                            {
                                // Multiple generators are available but we have insufficient information
                                // to choose one.  Log an warning and use the default
                                StringBuilder sb = new StringBuilder();

                                // Sort for unit test predictability
                                IEnumerable<Lazy<IClientCodeGenerator, ICodeGeneratorMetadata>> orderedCustomGenerators = customGeneratorImports.OrderBy(i => i.Metadata.GeneratorName);
                                foreach (var import in orderedCustomGenerators)
                                {
                                    sb.AppendLine("    " + import.Metadata.GeneratorName);
                                }

                                host.LogWarning(string.Format(CultureInfo.CurrentCulture,
                                                                Resource.Multiple_Custom_Code_Generators_Using_Default,
                                                                options.Language, sb.ToString(),
                                                                options.ClientProjectPath,
                                                                orderedCustomGenerators.First().Metadata.GeneratorName,
                                                                CodeDomClientCodeGenerator.GeneratorName));

                                // Pick the default.  There should be one, but if not, the calling methods will detect and report a problem.
                                lazyImport = allImportsForLanguage.FirstOrDefault(i => string.Equals(CodeDomClientCodeGenerator.GeneratorName, i.Metadata.GeneratorName, StringComparison.OrdinalIgnoreCase));
                            }
                        }
                    }

                    generator = lazyImport == null ? null : lazyImport.Value;
                }
            }

            return generator;
        }

        /// <summary>
        /// Creates the MEF composition container to use for code generation.
        /// </summary>
        /// <remarks>
        /// This container is constructed from the specified set of <paramref name="compositionAssemblyPaths"/>
        /// and serves as the context in which to find code generators.
        /// </remarks>
        /// <param name="compositionAssemblyPaths">Optional set of assembly locations to add to container.</param>
        /// <param name="logger"><see cref="ILogger"/> instance to report issues.</param>
        private void CreateCompositionContainer(IEnumerable<string> compositionAssemblyPaths, ILogger logger)
        {
            Debug.Assert(this._compositionContainer == null, "The composition container cannot be created twice");
            Debug.Assert(logger != null, "logger cannot be null");

            try
            {
                // This code creates a MEF composition container from all the assemblies of this solution
                IEnumerable<AssemblyCatalog> catalogs = ClientCodeGenerationDispatcher.GetCompositionAssemblies(compositionAssemblyPaths, logger).Select<Assembly, AssemblyCatalog>(a => new AssemblyCatalog(a));
                this._partCatalog = new AggregateCatalog(catalogs);
                this._compositionContainer = new CompositionContainer(this._partCatalog);

                // Add this instance to the container to satisfy the [ImportMany]
                this._compositionContainer.ComposeParts(this);
            }
            catch (Exception ex)
            {
                // An exception is possible in situations where the user has included
                // reference assemblies that cause TypeLoadFailures.  If we encounter
                // this situation, fallback to using only our current assembly as the
                // catalog.   This allows MEF to still work for types in this assembly.
                logger.LogWarning(string.Format(CultureInfo.CurrentCulture,
                                Resource.Failed_To_Create_Composition_Container, ex.Message));
                this._partCatalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());
                this._compositionContainer = new CompositionContainer(this._partCatalog);

                this._compositionContainer.ComposeParts(this);
            }
        }

        /// <summary>
        /// Returns the full set of <see cref="Assembly"/> instances from which to build the MEF composition container,
        /// </summary>
        /// <param name="compositionAssemblyPaths">Optional set of assembly locations to include.</param>
        /// <param name="logger"><see cref="ILogger"/> instance to report issues.</param>
        /// <returns>The set of <see cref="Assembly"/> instances to use.</returns>
        private static IEnumerable<Assembly> GetCompositionAssemblies(IEnumerable<string> compositionAssemblyPaths, ILogger logger)
        {
            HashSet<Assembly> assemblies = new HashSet<Assembly>();
            if (compositionAssemblyPaths != null)
            {
                foreach (string assemblyPath in compositionAssemblyPaths)
                {
                    Assembly a = AssemblyUtilities.LoadAssembly(assemblyPath, logger);
                    if (a != null)
                    {
                        // Don't put System assemblies into container
                        if (!a.IsSystemAssembly())
                        {
                            assemblies.Add(a);
                        }
                    }
                }
            }

            // Add this assembly itself to allow MEF to satisfy our imports
            assemblies.Add(typeof(ClientCodeGenerationDispatcher).Assembly);

            return assemblies;
        }

        #region IRegisteredObject Members
        void IRegisteredObject.Stop(bool immediate)
        {
        }
        #endregion


        #region IDisposable members

        public void Dispose()
        {
            CompositionContainer container = this._compositionContainer;
            ComposablePartCatalog catalog = this._partCatalog;
            this._compositionContainer = null;
            this._partCatalog = null;
            if (container != null)
            {
                container.Dispose();
            }
            if (catalog != null)
            {
                catalog.Dispose();
            }
        }
        #endregion
    }
}
