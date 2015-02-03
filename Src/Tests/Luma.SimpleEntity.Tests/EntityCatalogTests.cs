using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Luma.SimpleEntity.Server;
using Luma.SimpleEntity.TestHelpers;
using Luma.SimpleEntity.Tests.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestNamespace.TypeNameConflictResolution;

namespace Luma.SimpleEntity.Tests
{
    /// <summary>
    /// Summary description for domain service catalog
    /// </summary>
    [TestClass]
    public class EntityCatalogTests
    {
        [TestMethod]
        [Description("EntityCatalog ctors work properly")]
        public void EntityCatalog_Ctors()
        {
            IEnumerable<string> empty = new string[0];
            ConsoleLogger logger = new ConsoleLogger();

            // Ctor taking assemblies -- null arg tests
            ExceptionHelper.ExpectArgumentNullExceptionStandard(() => new EntityCatalog((IEnumerable<string>)null, logger), "assembliesToLoad");
            ExceptionHelper.ExpectArgumentNullExceptionStandard(() => new EntityCatalog(empty, null), "logger");

            // Ctor taking multiple types -- null arg tests
            ExceptionHelper.ExpectArgumentNullExceptionStandard(() => new EntityCatalog((IEnumerable<Type>)null, logger), "entityTypes");
            ExceptionHelper.ExpectArgumentNullExceptionStandard(() => new EntityCatalog(new[] { typeof(DSC_Entity) }, null), "logger");

            // Ctor taking assemblies -- legit
            string[] realAssemblies = new string[] { this.GetType().Assembly.Location,
                                                     typeof(string).Assembly.Location };

            // Assembly based ctors are tested more deeply in other test methods

            // Ctor taking multiple type -- legit
            var dsc = new EntityCatalog(new[] { typeof(DSC_Entity) }, logger);
            var descriptions = dsc.EntityDescriptions;
            Assert.IsNotNull(descriptions, "Did not expect null descriptions");
            Assert.AreEqual(1, descriptions.Count(), "Expected exactly one domain service description");
        }

        [Ignore]
        [TestMethod]
        [Description("EntityCatalog finds all Entity subtypes")]
        public void EntityCatalog_Finds_All_Entities()
        {
            var logger = new ConsoleLogger();
            var assemblies = new List<string>();

            // Add our current unit test assembly to those to load
            assemblies.Add(GetType().Assembly.Location);

            var expectedEntities = 0;
            foreach (var t in GetType().Assembly.GetExportedTypes())
            {
                if (IsEntity(t))
                {
                    ++expectedEntities;
                }
            }

            // Add all our assy references and also count any Entities there (don't expect any)
            foreach (AssemblyName an in GetType().Assembly.GetReferencedAssemblies())
            {
                var a = Assembly.Load(an);
                assemblies.Add(a.Location);
                foreach (var t in a.GetExportedTypes())
                {
                    if (IsEntity(t))
                    {
                        ++expectedEntities;
                    }
                }
            }

            EntityCatalog dsc = new EntityCatalog(assemblies, logger);
            ICollection<EntityDescription> descriptions = dsc.EntityDescriptions;
            Assert.IsNotNull(descriptions);
            Assert.IsTrue(descriptions.Count >= expectedEntities);
        }

        [TestMethod]
        [Description("EntityCatalog catches FileNotFoundException and emits an info message")]
        public void EntityCatalog_Message_FileNotFound()
        {
            string assemblyFileName = @"c:\Nowhere\DontExist.dll";
            ConsoleLogger logger = new ConsoleLogger();
            EntityCatalog dsc = new EntityCatalog(new string[] { assemblyFileName }, logger);
            ICollection<EntityDescription> descriptions = dsc.EntityDescriptions;
            Assert.IsNotNull(descriptions);
            Assert.AreEqual(0, descriptions.Count);
            Assert.AreEqual(0, logger.ErrorMessages.Count);
            Assert.AreEqual(0, logger.WarningMessages.Count);

            // Need to synthesize exactly the same message we'd expect from failed assembly load
            string exceptionMessage = null;
            try
            {
                AssemblyName.GetAssemblyName(assemblyFileName);
            }
            catch (FileNotFoundException fnfe)
            {
                exceptionMessage = fnfe.Message;
            }
            string expectedMessage = string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Assembly_Load_Error, assemblyFileName, exceptionMessage);
            TestHelper.AssertContainsMessages(logger, expectedMessage);
        }

        [Ignore]
        [TestMethod]
        [Description("EntityCatalog catches FileNotFoundException and emits an info message but continues processing")]
        public void EntityCatalog_Message_FileNotFound_Continues()
        {
            string assemblyFileName = @"c:\Nowhere\DontExist.dll";

            ConsoleLogger logger = new ConsoleLogger();
            IEnumerable<string> assemblies = new[] { assemblyFileName, this.GetType().Assembly.Location };
            EntityCatalog dsc = new EntityCatalog(assemblies, logger);
            ICollection<EntityDescription> descriptions = dsc.EntityDescriptions;
            Assert.IsNotNull(descriptions);

            // Need to synthesize exactly the same message we'd expect from failed assembly load
            string exceptionMessage = null;
            try
            {
                AssemblyName.GetAssemblyName(assemblyFileName);
            }
            catch (FileNotFoundException fnfe)
            {
                exceptionMessage = fnfe.Message;
            }
            string expectedMessage = string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Assembly_Load_Error, assemblyFileName, exceptionMessage);
            TestHelper.AssertContainsMessages(logger, expectedMessage);

            Assert.IsTrue(descriptions.Count > 0);
        }

        [TestMethod]
        [Description("EntityCatalog catches BadImageFormatException and emits an info message")]
        public void EntityCatalog_Message_BadImageFormat()
        {
            // Create fake DLL with bad image 
            string assemblyFileName = Path.Combine(Path.GetTempPath(), (Guid.NewGuid().ToString() + ".dll"));
            File.WriteAllText(assemblyFileName, "neener neener neener");

            ConsoleLogger logger = new ConsoleLogger();
            EntityCatalog dsc = new EntityCatalog(new string[] { assemblyFileName }, logger);
            ICollection<EntityDescription> descriptions = dsc.EntityDescriptions;
            Assert.IsNotNull(descriptions);
            Assert.AreEqual(0, descriptions.Count);
            Assert.AreEqual(0, logger.ErrorMessages.Count);
            Assert.AreEqual(0, logger.WarningMessages.Count);

            // Need to synthesize exactly the same message we'd expect from failed assembly load
            string exceptionMessage = null;
            try
            {
                AssemblyName.GetAssemblyName(assemblyFileName);
            }
            catch (BadImageFormatException bife)
            {
                exceptionMessage = bife.Message;
            }
            finally
            {
                File.Delete(assemblyFileName);
            }
            string expectedMessage = string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Assembly_Load_Error, assemblyFileName, exceptionMessage);
            TestHelper.AssertContainsMessages(logger, expectedMessage);
        }

        /// <summary>
        /// Returns true if the given type is a Entity
        /// </summary>
        /// <param name="t">The type to test</param>
        /// <returns><c>true</c> if it is a Entity type</returns>
        private static bool IsEntity(Type t)
        {
            if (t == null || t.IsAbstract || t.IsGenericTypeDefinition)
            {
                return false;
            }

            if (!typeof(Entity).IsAssignableFrom(t))
            {
                return false;
            }

            object[] attrs = t.GetCustomAttributes(typeof(KeyAttribute), false);

            return attrs.Length > 0;
        }
    }

    public class DSC_Entity
    {
       [Key] public string TheKey {get;set;}
    }
}
