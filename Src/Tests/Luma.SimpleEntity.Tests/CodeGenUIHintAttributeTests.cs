using System.ComponentModel.DataAnnotations;
using Luma.SimpleEntity.Tests.Server.Test.Utilities;
using Luma.SimpleEntity.Tests.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Luma.SimpleEntity.Tests
{
    using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

    /// <summary>
    /// Summary description for domain service code gen
    /// </summary>
    [TestClass]
    public class CodeGenUIHintAttributeTests
    {
        [TestMethod]
        [Description("[UIHint] code gens properly")]
        public void CodeGen_Attribute_UIHint()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(Mock_CG_Attr_Entity_UIHint));
            TestHelper.AssertGeneratedCodeContains(generatedCode, @"[UIHint(""theUIHint"", ""thePresentationLayer"")]");
        }

        [TestMethod]
        [Description("[UIHint] and set of control parameters code gens properly")]
        public void CodeGen_Attribute_UIHint_ControlParameters()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(Mock_CG_Attr_Entity_UIHint_ControlParameters));
            TestHelper.AssertGeneratedCodeContains(generatedCode,
                            @"[UIHint(""theUIHint"", ""thePresentationLayer"",",
                            @", ""key1"", 100",
                            @", ""key2"", ((double)(2D))");      // odd syntax reflect CodeDom workaround
        }

        [TestMethod]
        [Description("[UIHint] and odd number of control parameters should fail gracefully")]
        public void CodeGen_Attribute_UIHint_ControlParameters_Fail_Odd_Count()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", typeof(Mock_CG_Attr_Entity_UIHint_ControlParameters_Odd), logger);
            TestHelper.AssertContainsErrors(logger, "xxx");
            Assert.AreEqual(string.Empty, generatedCode);
        }
    }

    public partial class Mock_CG_Attr_Entity_UIHint
    {
        [Key]
        public int KeyField { get; set; }

        [UIHint("theUIHint", "thePresentationLayer")]
        public string StringProperty { get; set; }
    }

    public partial class Mock_CG_Attr_Entity_UIHint_ControlParameters
    {
        [Key]
        public int KeyField { get; set; }

        [UIHint("theUIHint", "thePresentationLayer", "key1", 100, "key2", 2.0)]
        public string StringProperty { get; set; }
    }

    public partial class Mock_CG_Attr_Entity_UIHint_ControlParameters_Odd
    {
        [Key]
        public int KeyField { get; set; }

        [UIHint("theUIHint", "thePresentationLayer", "key1", 100, "key2")]
        public string StringProperty { get; set; }
    }
}
