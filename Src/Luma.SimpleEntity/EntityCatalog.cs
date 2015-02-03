using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Luma.SimpleEntity.Helpers;
using Luma.SimpleEntity.Server;

namespace Luma.SimpleEntity
{
    /// <summary>
    /// Represents a catalog of entities.
    /// </summary>
    public class EntityCatalog
    {
        private readonly HashSet<string> _assembliesToLoad;
        private Dictionary<Assembly, bool> _loadedAssemblies;
        private readonly List<EntityDescription> _entityDescriptions = new List<EntityDescription>();
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
            AddEntityDescriptions();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityCatalog"/> class that permits code gen over a list of domain services
        /// </summary>
        /// <param name="entityTypes">list of domain service types to generate code for</param>
        /// <param name="logger">logger for logging messages while processing</param>
        /// <exception cref="ArgumentNullException"> is thrown if <paramref name="entityTypes"/> or <paramref name="logger"/> is null.</exception>
        public EntityCatalog(IEnumerable<Type> entityTypes, ILogger logger)
        {
            if (entityTypes == null)
            {
                throw new ArgumentNullException("entityTypes");
            }

            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _logger = logger;


            AddEntityDescriptions(entityTypes);
            
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
        public ICollection<EntityDescription> EntityDescriptions
        {
            get
            {
                return _entityDescriptions;
            }
        }

        /// <summary>
        /// Looks at all loaded assemblies and adds EntityDescription for each entity found
        /// </summary>
        private void AddEntityDescriptions()
        {
            var entityDescription = new EntityDescription();

            foreach (KeyValuePair<Assembly, bool> pair in _loadedAssemblies)
            {
                // Performance optimization: standard Microsoft assemblies are excluded from this search
                if (pair.Value)
                {
                    // Utility autorecovers and logs for common exceptions
                    var types = AssemblyUtilities.GetExportedTypes(pair.Key, _logger);

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
                _entityDescriptions.Add(entityDescription);
            }
        }

        private void AddEntityDescriptions(IEnumerable<Type> entityTypes)
        {
            var entityDescription = new EntityDescription();

            foreach (var entityType in entityTypes)
            { 
                try
                {
                    entityDescription.AddEntityType(entityType);
                }
                catch (Exception e)
                {
                    LogWarning(e.Message);
                }
            }

            entityDescription.Initialize();

            if (entityDescription.EntityTypes.Any())
            {
                _entityDescriptions.Add(entityDescription);
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
                    // The bool value indicates whether this assembly should be searched for a Entity
                    _loadedAssemblies[assembly] = !assembly.IsSystemAssembly();
                }
            }

            AssemblyUtilities.SetAssemblyResolver(_loadedAssemblies.Keys);
        }
    }
}
