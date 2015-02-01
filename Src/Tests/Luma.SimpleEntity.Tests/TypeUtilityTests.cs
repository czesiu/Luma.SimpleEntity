using Luma.SimpleEntity.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Luma.SimpleEntity.Tests
{
    [TestClass]
    public class TypeUtilityTests
    {
        [TestMethod]
        [Description("Checks that Simple Entity assembly is identified as a system assembly.")]
        public void TestSimpleEntityAssemblyIsSystemAssembly()
        {
            var assemblyName = typeof(IClientCodeGenerator).Assembly.FullName;
            var result = TypeUtility.IsSystemAssembly(assemblyName);
            Assert.IsTrue(result, "The assembly " + assemblyName + " is not identified as a system assembly");
        }
    }
}