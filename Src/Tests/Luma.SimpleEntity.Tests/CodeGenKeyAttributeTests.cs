﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Luma.SimpleEntity;
using Luma.SimpleEntity.Tests.Server.Test.Utilities;
using Luma.SimpleEntity.Tests.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Luma.SimpleEntity.Tests
{
    /// <summary>
    /// Test that have to deal with [Key] attribute on entities
    /// </summary>
    [TestClass]
    public class CodeGenKeyAttributeTests
    {
        [TestMethod]
        [Description("Entity missing [Key] should pass")]
        public void CodeGen_Attribute_KeyAttribute_Missing_Pass()
        {
            var logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", typeof(Mock_CG_Attr_Entity_Missing_Key), logger);
            Assert.IsTrue(!string.IsNullOrEmpty(generatedCode));
            TestHelper.AssertCodeGenSuccess(generatedCode, logger);
        }

        [TestMethod]
        [Description("GetIdentity method is not generated for an Entity with KeyAttribute SharedAttribute on the same property")]
        public void CodeGen_Attribute_KeyAttribute_SharedAttribute()
        {
            ConsoleLogger logger = new ConsoleLogger();

            // For this test, consider K2 shared and K1 not shared
            ISharedCodeService sts = new MockSharedCodeService(
                    new Type[] { typeof(Mock_CG_Attr_Entity_Shared_Key) },
                    new MethodInfo[] { typeof(Mock_CG_Attr_Entity_Shared_Key).GetProperty("K2").GetGetMethod() },
                    new string[0]);

            string generatedCode = TestHelper.GenerateCode("C#", new Type[] { typeof(Mock_CG_Attr_Entity_Shared_Key) }, logger, sts);
            Assert.IsTrue(!string.IsNullOrEmpty(generatedCode));
            TestHelper.AssertGeneratedCodeDoesNotContain(generatedCode, "GetIdentity");
        }
    }

    public class Mock_CG_Attr_Entity_Missing_Key
    {
        public string StringProperty { get; set; }
    }

    public partial class Mock_CG_Attr_Entity_Shared_Key
    {
        [Key]
        public int K1 { get; set; }

        [Key]
        public string K2 { get; set; }
    }
}
