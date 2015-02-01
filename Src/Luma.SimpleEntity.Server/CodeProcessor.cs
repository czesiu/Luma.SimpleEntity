using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace Luma.SimpleEntity.Server
{
    /// <summary>
    /// Base class for all <see cref="CodeProcessor"/> implementations. By associating a <see cref="CodeProcessor"/> Type
    /// with a <see cref="Entity"/> Type via the <see cref="Luma.SimpleEntity.DomainIdentifierAttribute"/>, codegen for the service
    /// Type can be customized.
    /// </summary>
    public abstract class CodeProcessor
    {
        /// <summary>
        /// Private reference to the <see cref="CodeDomProvider"/> used during code generation.
        /// </summary>
        private readonly CodeDomProvider _codeDomProvider;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="codeDomProvider">The <see cref="CodeDomProvider"/> used during code generation.</param>
        protected CodeProcessor(CodeDomProvider codeDomProvider)
        {
            if (codeDomProvider == null)
            {
                throw new ArgumentNullException("codeDomProvider");
            }

            this._codeDomProvider = codeDomProvider;
        }

        /// <summary>
        /// The <see cref="CodeDomProvider"/> used during code generation.
        /// </summary>
        protected CodeDomProvider CodeDomProvider
        {
            get
            {
                return this._codeDomProvider;
            }
        }

        /// <summary>
        /// Invoked after code generation of the current <see cref="EntityDescription"/> has completed, allowing for post processing of the <see cref="CodeCompileUnit"/>.
        /// </summary>
        /// <param name="entityDescription">The <see cref="EntityDescription"/> describing the <see cref="EntityDescription"/> currently being examined.</param>
        /// <param name="codeCompileUnit">The <see cref="CodeCompileUnit"/> that the <see cref="EntityDescription"/> client code is being generated into.</param>
        /// <param name="typeMapping">A dictionary mapping <see cref="EntityDescription"/> and related entity types to their corresponding <see cref="CodeTypeDeclaration"/>s.</param>
        public abstract void ProcessGeneratedCode(EntityDescription entityDescription, CodeCompileUnit codeCompileUnit, IDictionary<Type, CodeTypeDeclaration> typeMapping);
    }
}
