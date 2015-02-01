using System;
using System.Linq;
using Luma.SimpleEntity;
using Luma.SimpleEntity.Helpers;
using Luma.SimpleEntity.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace Luma.SimpleEntity.Tests
{
    [TestClass]
    public class AssemblyUtilitiesTest
    {
        [TestMethod]
        [WorkItem(810123)]
        [Description("Verifies that common known system assemblies are reported as such, and others are not")]
        public void IsSystemAssemblyTest()
        {
            Assembly mscorlib = typeof(object).Assembly;
            Assembly system = typeof(System.Uri).Assembly;
            Assembly systemCore = typeof(System.Linq.IQueryable<>).Assembly;
            Assembly simpleEntity = typeof(EntityDescription).Assembly;
            Assembly dataAnnotations = typeof(System.ComponentModel.DataAnnotations.DisplayAttribute).Assembly;
            Assembly excutingAssembly = Assembly.GetExecutingAssembly();

            Assert.IsTrue(TypeUtility.IsSystemAssembly(mscorlib), "mscorlib");
            Assert.IsTrue(TypeUtility.IsSystemAssembly(system), "system");
            Assert.IsTrue(TypeUtility.IsSystemAssembly(systemCore), "systemCore");
            Assert.IsTrue(TypeUtility.IsSystemAssembly(simpleEntity), "simpleEntity");
            Assert.IsTrue(TypeUtility.IsSystemAssembly(dataAnnotations), "dataAnnotations");
            Assert.IsFalse(TypeUtility.IsSystemAssembly(excutingAssembly), "Executing Assembly");
        }

        [TestMethod]
        [WorkItem(810123)]
        [Description("Verifies that only mscorlib is reported as the MsCorlib assembly")]
        public void IsAssemblyMsCorlibTest()
        {
            var mscorlib = typeof(object).Assembly.GetName();
            var system = typeof(Uri).Assembly.GetName();
            var systemCore = typeof(IQueryable<>).Assembly.GetName();
            var simpleEntity = typeof(EntityDescription).Assembly.GetName();
            var dataAnnotations = typeof(System.ComponentModel.DataAnnotations.DisplayAttribute).Assembly.GetName();
            var executingAssembly = Assembly.GetExecutingAssembly().GetName();

            Assert.IsTrue(AssemblyUtilities.IsAssemblyMsCorlib(mscorlib), "mscorlib");
            Assert.IsFalse(AssemblyUtilities.IsAssemblyMsCorlib(system), "system");
            Assert.IsFalse(AssemblyUtilities.IsAssemblyMsCorlib(systemCore), "systemCore");
            Assert.IsFalse(AssemblyUtilities.IsAssemblyMsCorlib(simpleEntity), "simpleEntity");
            Assert.IsFalse(AssemblyUtilities.IsAssemblyMsCorlib(dataAnnotations), "dataAnnotations");
            Assert.IsFalse(AssemblyUtilities.IsAssemblyMsCorlib(executingAssembly), "Executing Assembly");
        }
    }
}
