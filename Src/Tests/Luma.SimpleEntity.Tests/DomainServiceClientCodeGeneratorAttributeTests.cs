using System;
using System.Collections.Generic;
using Luma.SimpleEntity;
using Luma.SimpleEntity.Server;
using Luma.SimpleEntity.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Luma.SimpleEntity.Tests
{
    /// <summary>
    /// Tests the <see cref="ClientCodeGeneratorAttribute"/> class
    /// </summary>
    [TestClass]
    public class ClientCodeGeneratorAttributeTests
    {
        [TestMethod]
        [Description("ClientProxyGenerator ctor taking strings work properly")]
        public void CodeGeneratorAttribute_Ctor_Strings()
        {
            // nulls allowed
            var attr = new ClientCodeGeneratorAttribute((string) null, null);
            Assert.IsNull(attr.GeneratorName, "Generator name not null");
            Assert.IsNull(attr.Language, "Language not null");

            // empty strings allowed
            attr = new ClientCodeGeneratorAttribute(string.Empty, string.Empty);
            Assert.AreEqual(string.Empty, attr.GeneratorName, "Generator name not empty");
            Assert.AreEqual(string.Empty, attr.Language, "Language not empty");

            // valid strings accepted
            attr = new ClientCodeGeneratorAttribute("AName", "ALanguage");
            Assert.AreEqual("AName", attr.GeneratorName, "Generator name not respected");
            Assert.AreEqual("ALanguage", attr.Language, "Language not respected");
        }

        [TestMethod]
        [Description("ClientProxyGenerator ctor taking Type work properly")]
        public void CodeGeneratorAttribute_Ctor_Type()
        {
            // nulls allowed
            ClientCodeGeneratorAttribute attr = new ClientCodeGeneratorAttribute((Type) null, null);
            Assert.AreEqual(string.Empty, attr.GeneratorName, "Generator name not empty");
            Assert.IsNull(attr.Language, "Language not null");

            // empty strings allowed
            attr = new ClientCodeGeneratorAttribute((Type) null, string.Empty);
            Assert.AreEqual(string.Empty, attr.GeneratorName, "Generator name not empty");
            Assert.AreEqual(string.Empty, attr.Language, "Language not empty");

            // valid type accepted
            attr = new ClientCodeGeneratorAttribute(typeof(DSCPG_Generator), "ALanguage");
            Assert.AreEqual(typeof(DSCPG_Generator).FullName, attr.GeneratorName, "Generator name the type's full name");
            Assert.AreEqual("ALanguage", attr.Language, "Language not respected");
        }
    }

    public class DSCPG_Generator : IClientCodeGenerator
    {
        public string GenerateCode(ICodeGenerationHost codeGenerationHost, IEnumerable<EntityDescription> entityDescriptions, ClientCodeGenerationOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
