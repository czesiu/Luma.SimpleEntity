using System;
using Luma.SimpleEntity.Tools;

namespace Luma.SimpleEntity
{
    /// <summary>
    /// Class to hold options used for
    /// <see cref="Luma.SimpleEntity.Server.DomainService"/>
    /// client code generation.
    /// </summary>
    /// <remarks>
    /// This is a data class and has no behavior.  It is used solely to package
    /// the code generation options.
    /// </remarks>
    [Serializable]
    public class ClientCodeGenerationOptions
    {
        private string _language;

        /// <summary>
        /// Gets or sets the language for code generation.  Cannot be null or empty.
        /// </summary>
        public string Language
        {
            get
            {
                return this._language;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value", Resource.Null_Language_Property);
                }
                this._language = value;
            }
        }

        /// <summary>
        /// Gets or sets the full path to the targeted framework for the client
        /// </summary>
        public string ClientFrameworkPath { get; set; }

        /// <summary>
        /// Gets or sets the full path to the server's project file
        /// </summary>
        public string ServerProjectPath { get; set; }

        /// <summary>
        /// Gets or sets the full path to the client's project file
        /// </summary>
        public string ClientProjectPath { get; set; }

        /// <summary>
        /// Gets or sets the root namespace of the target project. If it's not null or empty, 
        /// the code generator will try to change generated namespaces in such a way that the client and
        /// server namespaces match. Use this to get correct code generation for Visual Basic projects 
        /// with nonempty root namespace.
        /// </summary>
        public string ClientRootNamespace { get; set; }

        /// <summary>
        /// Gets or sets the root namespace of the server project. If it's not null or empty, 
        /// the code generator will try to change generated namespaces in such a way that the client and
        /// server namespaces match. Use this to get correct code generation for Visual Basic projects 
        /// with nonempty root namespace.
        /// </summary>
        public string ServerRootNamespace { get; set; }

        /// <summary>
        /// Gets or sets a value the target platform of the client.
        /// </summary>
        public TargetPlatform ClientProjectTargetPlatform { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether fully qualified type names
        /// should be used during code generation
        /// </summary>
        /// <remarks>
        /// If <c>false</c> the code generator will generate only short type names
        /// and add import statements.  If <c>true</c> the code generator will always
        /// generate fully qualified type names and avoid adding unnecessary imports.
        /// </remarks>
        public bool UseFullTypeNames { get; set; }
    }
}
