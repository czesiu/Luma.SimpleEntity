﻿using System.Collections.Generic;
using Luma.SimpleEntity.Server;

namespace Luma.SimpleEntity
{
    /// <summary>
    /// Common interface for code generators that produce client code from 
    /// <see cref="EntityDescription"/> instances.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface are expected to be stateless objects
    /// that can be invoked to generate code on demand.   A single instance of this
    /// class may be used multiple times with different sets of inputs.
    /// </remarks>
    public interface IClientCodeGenerator
    {
        /// <summary>
        /// Generates the source code for the client classes
        /// for the given <paramref name="entityDescriptions"/>.
        /// </summary>
        /// <remarks>
        /// Errors and warnings should be reported using the <paramref name="codeGenerationHost"/>.
        /// </remarks>
        /// <param name="codeGenerationHost">The <see cref="ICodeGenerationHost"/> object hosting code generation.</param>
        /// <param name="entityDescriptions">The set of <see cref="EntityDescription"/> 
        /// instances for which code generation is required.</param>
        /// <param name="options">The options for code generation.</param>
        /// <returns>The generated code.  This value may be empty or <c>null</c> if errors occurred or there was no work to do.</returns>
        string GenerateCode(ICodeGenerationHost codeGenerationHost, IEnumerable<EntityDescription> entityDescriptions, ClientCodeGenerationOptions options);
    }
}
