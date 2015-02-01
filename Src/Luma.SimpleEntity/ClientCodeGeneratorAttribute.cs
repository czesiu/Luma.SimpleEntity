using System;
using System.ComponentModel.Composition;
using Luma.SimpleEntity.Tools;

namespace Luma.SimpleEntity
{
    /// <summary>
    /// Derived <see cref="ExportAttribute"/> used for all code generators
    /// that support <see cref="IClientCodeGenerator"/>.
    /// </summary>
    /// <remarks>
    /// This attribute exports both the type of the code generator
    /// (<see cref="IClientCodeGenerator"/>) as well
    /// as the metadata that describe the code generator.
    /// </remarks>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ClientCodeGeneratorAttribute : ExportAttribute, ICodeGeneratorMetadata
    {
        private readonly string _generatorName;
        private readonly string _language;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientCodeGeneratorAttribute"/> class
        /// for a generator name specified by <paramref name="generatorName"/>.
        /// </summary>
        /// <param name="generatorName">The unique name of this generator.</param>
        /// <param name="language">The language supported by this generator.</param>
        public ClientCodeGeneratorAttribute(string generatorName, string language)
            : base(typeof(IClientCodeGenerator))
        {
            _generatorName = generatorName;
            _language = language;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientCodeGeneratorAttribute"/> class
        /// for a generator name specified by <paramref name="generatorType"/>.
        /// </summary>
        /// <remarks>
        /// This overload will use the <paramref name="generatorType"/>'s name for
        /// the <see cref="GeneratorName"/> property.
        /// </remarks>
        /// <param name="generatorType">The type of the generator.</param>
        /// <param name="language">The language supported by this generator.</param>
        public ClientCodeGeneratorAttribute(Type generatorType, string language) : this(generatorType != null ? generatorType.FullName : string.Empty, language)
        {
        }

        /// <summary>
        /// Gets the language supported by this code generator.
        /// </summary>
        public string Language { get { return this._language; } }

        /// <summary>
        /// Gets the logical name of this generator.
        /// </summary>
        /// <value>
        /// This value provides a unique identity to this code generator
        /// that can be used to select among multiple code generators.
        /// </value>
        public string GeneratorName { get { return this._generatorName; } }
    }
}
