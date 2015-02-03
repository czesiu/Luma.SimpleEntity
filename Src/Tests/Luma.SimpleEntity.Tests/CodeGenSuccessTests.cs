﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Luma.SimpleEntity.Tests.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Luma.SimpleEntity.Tests
{
    [TestClass]
    public class CodeGenSuccessTests
    {
        [Ignore]
        [TestMethod]
        [Description("Verifies errors are generated when a non-simple type is used as a Dictionary generic argument.")]
        public void CodeGen_DictionaryProperty()
        {
            var logger = new ConsoleLogger();
            var generatedCode = TestHelper.GenerateCode("C#", typeof(Mock_CG_Entity_DictionaryMember), logger);

            TestHelper.AssertGeneratedCodeContains(generatedCode, "public Dictionary<string, List<string>> DictionaryProperty");
        }

        [Ignore]
        [TestMethod]
        [Description("Verifies errors are generated when an Entity type is used as a Dictionary generic argument.")]
        public void CodeGen_EntityUsedAsGenericParam_ShouldGenerateIt()
        {
            var logger = new ConsoleLogger();
            var generatedCode = TestHelper.GenerateCode("C#", typeof(Mock_CG_Entity_DictionaryMember_EntityUsedAsDictionaryTypeArg), logger);

            TestHelper.AssertGeneratedCodeContains(generatedCode, "public Dictionary<string, MockEntity1> EntityUsedAsDictionaryTypeArg");
        }

        public class Mock_CG_Entity_DictionaryMember
        {
            [Key]
            public int Id { get; set; }

            public Dictionary<string, List<string>> DictionaryProperty { get; set; }
        }

        public class Mock_CG_Entity_DictionaryMember_EntityUsedAsDictionaryTypeArg
        {
            [Key]
            public int Id { get; set; }

            public Dictionary<string, MockEntity1> EntityUsedAsDictionaryTypeArg { get; set; }
        }
    }
}