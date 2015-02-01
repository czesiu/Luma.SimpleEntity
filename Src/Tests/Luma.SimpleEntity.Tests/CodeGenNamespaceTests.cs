using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public class Mock_CG_Attr_Entity_Missing_Namespace
{
    [Key]
    public string StringProperty { get; set; }
}

namespace Luma.SimpleEntity.Tests
{
    /// <summary>
    /// Summary description for domain service code gen
    /// </summary>
    [TestClass]
    public class CodeGenNamespaceTests
    {
        [TestMethod]
        [Description("ntity outside of namespace fails")]
        public void CodeGen_Namespace_Fails_Missing()
        {
            string error = string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Namespace_Required, "Mock_CG_Attr_Entity_Missing_Namespace");
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_CG_Attr_Entity_Missing_Namespace), error);
        }
    }
}
