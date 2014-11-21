using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Luma.SimpleEntity.Helpers;
using Luma.SimpleEntity.Server;

namespace Luma.SimpleEntity
{
    /// <summary>
    /// Represents a catalog of DomainServices.
    /// </summary>
    internal class EntityCatalog
    {
        private readonly HashSet<string> _assembliesToLoad;
        private Dictionary<Assembly, bool> _loadedAssemblies;
        private readonly List<EntityDescription> _domainServiceDescriptions = new List<EntityDescription>();
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityCatalog"/> class with the specified input and reference assemblies
        /// </summary>
        /// <param name="assembliesToLoad">The set of assemblies to load (includes all known assemblies and references).</param>
        /// <param name="logger">logger for logging messages while processing</param>
        /// <exception cref="ArgumentNullException"> is thrown if <paramref name="assembliesToLoad"/> or <paramref name="logger"/> is null.</exception>
        public EntityCatalog(IEnumerable<string> assembliesToLoad, ILogger logger)
        {
            if (assembliesToLoad == null)
            {
                throw new ArgumentNullException("assembliesToLoad");
            }

            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _logger = logger;

            _assembliesToLoad = new HashSet<string>(assembliesToLoad, StringComparer.OrdinalIgnoreCase);

            LoadAllAssembliesAndSetAssemblyResolver();
            AddDomainServiceDescriptions();
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        /// <param name="message">message to be logged</param>
        private void LogError(string message)
        {
            if (_logger != null)
            {
                _logger.LogError(message);
            }
        }

        /// <summary>
        /// Log an error exception
        /// </summary>
        /// <param name="ex">Exception to be logged</param>
        private void LogException(Exception ex)
        {
            if (_logger != null)
            {
                _logger.LogException(ex);
            }
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        /// <param name="message">message to be logged</param>
        private void LogWarning(string message)
        {
            if (_logger != null)
            {
                _logger.LogWarning(message);
            }
        }

        /// <summary>
        /// Gets a collection of domain service descriptions
        /// </summary>
        public ICollection<EntityDescription> DomainServiceDescriptions
        {
            get
            {
                return _domainServiceDescriptions;
            }
        }

        /// <summary>
        /// Looks at all loaded assemblies and adds EntityDescription for each DomainService found
        /// </summary>
        private void AddDomainServiceDescriptions()
        {
            var entityDescription = new EntityDescription();

            foreach (KeyValuePair<Assembly, bool> pair in _loadedAssemblies)
            {
                // Performance optimization: standard Microsoft assemblies are excluded from this search
                if (pair.Value)
                {
                    // Utility autorecovers and logs for common exceptions
                    IEnumerable<Type> types = AssemblyUtilities.GetExportedTypes(pair.Key, _logger);

                    foreach (var type in types)
                    {
                        try
                        {
                            entityDescription.TryAddEntityType(type);
                        }
                        catch (Exception e)
                        {
                            LogWarning(e.ToString());
                        }
                    }
                }
            }

            entityDescription.Initialize();

            if (entityDescription.EntityTypes.Any())
            {
                _domainServiceDescriptions.Add(entityDescription);
            }
        }

        /// <summary>
        /// Invoked once to force load all assemblies into an analysis unit
        /// </summary>
        private void LoadAllAssembliesAndSetAssemblyResolver()
        {
            _loadedAssemblies = new Dictionary<Assembly, bool>();

            foreach (var assemblyName in _assembliesToLoad)
            {
                Assembly assembly = AssemblyUtilities.LoadAssembly(assemblyName, _logger);
                if (assembly != null)
                {
                    // The bool value indicates whether this assembly should be searched for a DomainService
                    _loadedAssemblies[assembly] = !assembly.IsSystemAssembly();
                }
            }

            AssemblyUtilities.SetAssemblyResolver(_loadedAssemblies.Keys);
        }
    }
}
