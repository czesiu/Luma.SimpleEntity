﻿using System.Collections.Generic;
using System.Linq;
using Luma.SimpleEntity;
using Luma.SimpleEntity.Server;
using Luma.SimpleEntity.Tools;

namespace Luma.SimpleEntity.Tests.T4Generator
{
    [DomainServiceClientCodeGenerator(GeneratorName,  "C#")]
    public partial class T4DomainServiceClientCodeGenerator : IDomainServiceClientCodeGenerator
    {
        public const string GeneratorName = "T4CodeGenerator";
        public const string GeneratedBoilerPlate = "This code was generated by T4DomainServiceClientCodeGenerator";

        private ICodeGenerationHost _codeGenerationHost;
        private ClientCodeGenerationOptions _options;
        private List<EntityDescription> _domainServiceDescriptions;

        public ICodeGenerationHost CodeGenerationHost { get { return this._codeGenerationHost; } }
        public ClientCodeGenerationOptions CodeGenerationOptions { get { return this._options; } }
        public IEnumerable<EntityDescription> DomainServiceDescriptions { get { return this._domainServiceDescriptions; } }

        public string GenerateCode(ICodeGenerationHost host, IEnumerable<EntityDescription> domainServiceDescriptions, ClientCodeGenerationOptions options)
        {
            this._codeGenerationHost = host;
            this._options = options;
            this._domainServiceDescriptions = domainServiceDescriptions.ToList();

            return this.TransformText();
        }
    }
}
