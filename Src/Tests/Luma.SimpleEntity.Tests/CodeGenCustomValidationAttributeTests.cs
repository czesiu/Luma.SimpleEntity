using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Luma.SimpleEntity;
using Luma.SimpleEntity.Tests.Server.Test.Utilities;
using Luma.SimpleEntity.Tests.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Luma.SimpleEntity.Tests
{
    /// <summary>
    /// Summary description for domain service code gen
    /// </summary>
    [TestClass]
    public class CodeGenCustomValidationAttributeTests
    {
        [TestMethod]
        [Description("Multiple CustomValidationAttribute generates all")]
        public void CodeGen_Attribute_CustomValidation_Multiple()
        {

            ISharedCodeService sts = new MockSharedCodeService(
                    new Type[] { typeof(Mock_CG_Attr_Validator) },
                    new MethodBase[] {  typeof(Mock_CG_Attr_Validator).GetMethod("IsValid"),
                                        typeof(Mock_CG_Attr_Validator).GetMethod("IsValid2") },
                    new string[0]);

            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", new[] { typeof(Mock_CG_Attr_Entity_Multiple) }, sts);
            TestHelper.AssertGeneratedCodeContains(generatedCode,
                                                    @"[CustomValidation(typeof(Mock_CG_Attr_Validator), ""IsValid"")]",
                                                    @"[CustomValidation(typeof(Mock_CG_Attr_Validator), ""IsValid2"")]");
        }

        [TestMethod]
        [Description("Verify that CustomValidationAttributes referencing non shared types are not propagated")]
        public void CodeGen_Attribute_CustomValidation_UnsharedValidator()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", typeof(UnsharedValidatorTestEntity), logger);
            TestHelper.AssertNoErrorsOrWarnings(logger);
            Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "Expected generated code");
            TestHelper.AssertGeneratedCodeDoesNotContain(generatedCode, "typeof(UnsharedValidator)");
        }
    }

    public class Mock_CG_Attr_Validator
    {
        public static ValidationResult IsValid(string value, ValidationContext context) { return ValidationResult.Success; }
        public static ValidationResult IsValid2(string value) { return ValidationResult.Success; }
    }

    // No shared attribute to verify that non-shared validation is not propagated during
    // codegen
    public class UnsharedValidator
    {
        public static ValidationResult IsValid(string value, ValidationContext context)
        {
            return ValidationResult.Success;
        }
    }

    public class UnsharedValidatorTestEntity
    {
        [Key]
        [CustomValidation(typeof(UnsharedValidator), "IsValid")]
        public string StringProperty
        {
            get;
            set;
        }
    }

    public partial class Mock_CG_Attr_Entity_Unshared_ResourceType
    {
        public Mock_CG_Attr_Entity_Unshared_ResourceType() { }

        [Key]
        [CustomValidation(typeof(Mock_CG_Attr_Validator), "IsValid", ErrorMessageResourceType = typeof(Mock_CG_Attr_Unshared_ResourceType), ErrorMessageResourceName = "StringResource")]
        public string StringProperty { get; set; }
    }


    public class Mock_CG_Attr_Unshared_ResourceType
    {
        public static string StringResource { get { return "fred"; } }
    }

    public partial class Mock_CG_Attr_Entity_Multiple
    {
        public Mock_CG_Attr_Entity_Multiple() { }

        [Key]
        [CustomValidation(typeof(Mock_CG_Attr_Validator), "IsValid")]
        [CustomValidation(typeof(Mock_CG_Attr_Validator), "IsValid2")]
        public string StringProperty { get; set; }
    }

}
