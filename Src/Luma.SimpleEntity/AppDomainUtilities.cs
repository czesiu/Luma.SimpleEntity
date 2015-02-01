using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Luma.SimpleEntity.Helpers;

namespace Luma.SimpleEntity
{
    /// <summary>
    /// Utilities related to configuring an <see cref="AppDomain"/>
    /// that will have client framework assemblies loaded into it.
    /// </summary>
    /// <remarks>
    /// This class implements logic specific to handling client assembly
    /// versioning to allow referenced client assemblies to be loaded
    /// and examined properly.
    /// </remarks>
    internal static class AppDomainUtilities
    {
        /// <summary>
        /// The key to use for storing the framework manifest as data on
        /// the <see cref="AppDomain"/> through <see cref="AppDomain.SetData(string,object)"/>
        /// and retrieving it through <see cref="AppDomain.GetData"/>.
        /// </summary>
        private const string FrameworkManifestKey = "FrameworkManifest";

        /// <summary>
        /// Creates an <see cref="AppDomain"/> configured for client code generation.
        /// </summary>
        /// <param name="options">The code generation options.</param>
        internal static void ConfigureAppDomain(ClientCodeGenerationOptions options)
        {
            FrameworkManifest frameworkManifest = GetFrameworkManifest(options.ClientFrameworkPath);

            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += AppDomainUtilities.ResolveFrameworkAssemblyVersioning;
            AppDomain.CurrentDomain.SetData(FrameworkManifestKey, frameworkManifest);
        }

        /// <summary>
        /// Gets the list of assemblies found for the specified directory
        /// </summary>
        /// <param name="frameworkDirectory">The directory containing the framework manifest.</param>
        /// <returns>The list of assemblies that are part of the target platform runtime.</returns>
        public static FrameworkManifest GetFrameworkManifest(string frameworkDirectory)
        {
            var assemblies = from dll in Directory.EnumerateFiles(frameworkDirectory, "*.dll")
                             let assemblyName = TryGetAssemblyName(dll)
                             where assemblyName != null
                             select new FrameworkManifestEntry
            {
                Name = assemblyName.Name,
                Version = assemblyName.Version, 
                PublicKeyTokenBytes = assemblyName.GetPublicKeyToken()
            };

            return new FrameworkManifest { Assemblies = assemblies.ToArray() };
        }

        public static AssemblyName TryGetAssemblyName(string dll)
        {
            try
            {
                return AssemblyName.GetAssemblyName(dll);
            }
            catch (BadImageFormatException)
            {
                return null;
            }
        }

        /// <summary>
        /// An event handler for resolving framework assembly versioning.
        /// </summary>
        /// <remarks>
        /// When a previous version of a assembly is sought, the targeted version
        /// of that assembly will be returned.
        /// </remarks>
        /// <param name="sender">The event sender.</param>
        /// <param name="args">The assembly resolution event arguments.</param>
        /// <returns>The <see cref="Assembly"/> from the targeted version of client runtime, or <c>null</c>.</returns>
        private static Assembly ResolveFrameworkAssemblyVersioning(object sender, ResolveEventArgs args)
        {
            var frameworkManifest = (FrameworkManifest)AppDomain.CurrentDomain.GetData(AppDomainUtilities.FrameworkManifestKey);
            System.Diagnostics.Debug.Assert(frameworkManifest != null, "The FrameworkManifest must have been set on the AppDomain");

            AssemblyName requestedAssembly = new AssemblyName(args.Name);

            // If the requested assembly is a System assembly and it's an older version
            // than the framework manifest has, then we'll need to resolve to its newer version
            bool isOldVersion = requestedAssembly.Version.CompareTo(frameworkManifest.SystemVersion) < 0;

            if (isOldVersion && requestedAssembly.IsSystemAssembly())
            {
                // Now we need to see if the requested assembly is part of the framework manifest (as opposed to an SDK assembly)
                var assembly = (from a in frameworkManifest.Assemblies
                                           where a.Name == requestedAssembly.Name
                                           select a).SingleOrDefault();

                // If the assembly is part of the framework manifest, then we need to "redirect" its resolution
                // to the current framework version.
                if (assembly != null)
                {
                    // Find the client framework assembly from the already-loaded assemblies
                    var matches = from a in AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies()
                                  let assemblyName = a.GetName()
                                  where assemblyName.Name == assembly.Name
                                     && assemblyName.GetPublicKeyToken().SequenceEqual(assembly.PublicKeyTokenBytes)
                                     && assemblyName.Version.CompareTo(assembly.Version) == 0
                                  select a;

                    return matches.SingleOrDefault();
                }
            }

            return null;
        }

        /// <summary>
        /// A framework manifest entry with assembly name parts.
        /// </summary>
        internal class FrameworkManifestEntry : MarshalByRefObject
        {
            internal string Name { get; set; }
            internal Version Version { get; set; }
            internal byte[] PublicKeyTokenBytes { get; set; }
        }

        /// <summary>
        /// Represents the framework manifest with its System version and
        /// the list of framework assemblies.
        /// </summary>
        internal class FrameworkManifest : MarshalByRefObject
        {
            private FrameworkManifestEntry[] _assemblies;

            internal Version SystemVersion { get; private set; }
            internal FrameworkManifestEntry[] Assemblies
            {
                get
                {
                    return this._assemblies;
                }
                set
                {
                    this._assemblies = value;
                    this.SystemVersion = (from assembly in this._assemblies
                                          where assembly.Name == "System"
                                          select assembly.Version).SingleOrDefault();
                }
            }
        }
    }
}
