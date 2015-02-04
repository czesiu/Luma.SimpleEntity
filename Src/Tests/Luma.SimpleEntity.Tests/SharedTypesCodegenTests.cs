using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Luma.SimpleEntity.Tests.Utilities;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Luma.SimpleEntity.Tests
{
    /// <summary>
    /// Tests for custom build task to generate client proxies
    /// </summary>
    [TestClass]
    public class SharedTypesCodegenTests
    {
        [DeploymentItem(@"Luma.SimpleEntity.Tests\ProjectPath.txt", "STT")]
        [Description("CreateClientFilesTask does not codegen shared types or properties on entities")]
        [TestMethod]
        public void SharedTypes_CodeGen_Skips_Shared_Types_And_Properties()
        {
            CreateClientFilesTask task = null;
            var expectedOutputFiles = new[] {
                "ServerClassLib.g.cs",          // generated
                "TestEntity.shared.cs",         // via server project
                "ServerClassLib2.shared.cs"     // via P2P
            };

            try
            {
                task = CodeGenHelper.CreateClientFilesTaskInstance("STT", /*includeClientOutputAssembly*/ false);
                MockBuildEngine mockBuildEngine = task.BuildEngine as MockBuildEngine;

                // Work Item 199139:
                // We're stripping ServerClassLib2 from the reference assemblies since we cannot depend on Visual Studio
                // to reliably produce a full set of dependencies. This will force the assembly resolution code to
                // search for ServerClassLib2 during codegen.
                // Note: Our assembly resolution code is only exercised when running against an installed product. When
                // we're running locally, resolution occurs without error.
                task.ServerReferenceAssemblies = task.ServerReferenceAssemblies.Where(item => !item.ItemSpec.Contains("ServerClassLib2")).ToArray();

                bool success = task.Execute();
                if (!success)
                {
                    Assert.Fail("CreateClientFilesTask failed:\r\n" + mockBuildEngine.ConsoleLogger.Errors);
                }

                ITaskItem[] outputFiles = task.OutputFiles.ToArray();
                Assert.AreEqual(expectedOutputFiles.Length, outputFiles.Length);

                string generatedFile = CodeGenHelper.GetOutputFile(outputFiles, expectedOutputFiles[0]);

                string generatedCode = string.Empty;
                using (StreamReader t1 = new StreamReader(generatedFile))
                {
                    generatedCode = t1.ReadToEnd();
                }

                ConsoleLogger logger = new ConsoleLogger();
                logger.LogMessage(generatedCode);
                CodeGenHelper.AssertGenerated(generatedCode, "public sealed partial class TestEntity : Entity");
                CodeGenHelper.AssertGenerated(generatedCode, "public string TheKey");
                CodeGenHelper.AssertGenerated(generatedCode, "public int TheValue");

                // This property is in shared code (via link) and should not have been generated
                CodeGenHelper.AssertNotGenerated(generatedCode, "public int ServerAndClientValue");

                // The automatic property in shared code should have been generated because
                // the PDB would lack any info to know it was shared strictly at the source level
                CodeGenHelper.AssertGenerated(generatedCode, "public string AutomaticProperty");

                // The server-only IsValid method should have emitted a comment warning it is not shared
                CodeGenHelper.AssertGenerated(generatedCode, "// [CustomValidationAttribute(typeof(ServerClassLib.TestValidatorServer), \"IsValid\")]");

                // The TestDomainSharedService already had a matching TestDomainSharedContext DomainContext
                // pre-built into the client project.  Verify we did *NOT* regenerate a 2nd copy
                // TODO: Do it
                // CodeGenHelper.AssertNotGenerated(generatedCode, "TestDomainShared");
                // CodeGenHelper.AssertNotGenerated(generatedCode, "TestEntity2");

                // Test that we get an informational message about skipping this shared domain context
                // TODO: Do it
                // string msg = string.Format(CultureInfo.CurrentCulture, Resource.Shared_DomainContext_Skipped, "TestDomainSharedService");
                // TestHelper.AssertContainsMessages(mockBuildEngine.ConsoleLogger, msg);

                // This property is in shared code in a p2p referenced assembly and should not have been generated
                CodeGenHelper.AssertNotGenerated(generatedCode, "public int SharedProperty_CL2");
            }
            finally
            {
                CodeGenHelper.DeleteTempFolder(task);
            }
        }

        [DeploymentItem(@"Luma.SimpleEntity.Tests\ProjectPath.txt", "STT")]
        [Description("CreateClientFilesTask produces error when detecting existing generated entity")]
        [TestMethod]
        public void SharedTypes_CodeGen_Errors_On_Existing_Generated_Entity()
        {
            CreateClientFilesTask task = null;

            try
            {
                task = CodeGenHelper.CreateClientFilesTaskInstance("STT", /*includeClientOutputAssembly*/ true);
                var mockBuildEngine = (MockBuildEngine)task.BuildEngine;

                bool success = task.Execute();
                Assert.IsFalse(success, "Expected build to fail");
                string entityMsg = string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_EntityTypesCannotBeShared_Reference, "ServerClassLib.TestEntity");
                TestHelper.AssertContainsErrors(mockBuildEngine.ConsoleLogger, entityMsg);
            }
            finally
            {
                CodeGenHelper.DeleteTempFolder(task);
            }
        }

        [TestMethod]
        [Description("Codegen emits a warning if an entity property is not shared")]
        public void SharedTypes_CodeGen_Warns_Unshared_Property_Type()
        {
            var logger = new ConsoleLogger();

            // Create a shared type service that says the entity's attribute is "shared" when asked whether it is shared
            var mockSts = new MockSharedCodeService(new Type[0], new MethodBase[0], new string[0]);

            var generatedCode = TestHelper.GenerateCode("C#", new[] { typeof(Mock_CG_Shared_Entity) }, logger, mockSts);

            Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "Code should have been generated");

            string entityWarning = String.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_PropertyType_Not_Shared, "XElementProperty", typeof(Mock_CG_Shared_Entity).FullName, typeof(X).FullName, "MockProject");
            TestHelper.AssertContainsWarnings(logger, entityWarning);
        }
    }

    public struct X
    {
        
    }

    public class Mock_CG_Shared_Entity
    {
        [Key]
        public string TheKey { get; set; }

        // This property type will be defined as "unshared" in the unit tests
        public X XElementProperty { get; set; }
    }
}
