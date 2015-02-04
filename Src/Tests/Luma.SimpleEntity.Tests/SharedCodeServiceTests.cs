using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Luma.SimpleEntity;
using Luma.SimpleEntity.Tests.Server.Test.Utilities;
using Luma.SimpleEntity.Tests.Utilities;
using Luma.SimpleEntity.Tools.SharedTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServerClassLib;

namespace Luma.SimpleEntity.Tests
{
    /// <summary>
    /// Tests for SharedAssemblies service
    /// </summary>
    [TestClass]
    public class SharedCodeServiceTests
    {
        public SharedCodeServiceTests()
        {
        }

        [Description("SharedCodeServiceParameter properties can be set")]
        [TestMethod]
        public void SharedCodeServiceParameter_Properties()
        {
            SharedCodeServiceParameters parameters = new SharedCodeServiceParameters()
            {
                ClientAssemblies = new string[] { "clientAssembly" },
                ServerAssemblies = new string[] { "serverAssembly" },
                ClientAssemblyPathsNormalized = new string[] { "clientPaths" },
                SharedSourceFiles = new string[] { "sharedSourceFiles" },
                SymbolSearchPaths = new string[] { "symSearch" }
            };

            Assert.AreEqual("clientAssembly", parameters.ClientAssemblies.First());
            Assert.AreEqual("serverAssembly", parameters.ServerAssemblies.First());
            Assert.AreEqual("clientPaths", parameters.ClientAssemblyPathsNormalized.First());
            Assert.AreEqual("sharedSourceFiles", parameters.SharedSourceFiles.First());
            Assert.AreEqual("symSearch", parameters.SymbolSearchPaths.First());
        }


        [Description("SharedCodeService ctors can be called")]
        [TestMethod]
        public void SharedCodeService_Ctor()
        {
            SharedCodeServiceParameters parameters = new SharedCodeServiceParameters()
            {
                ClientAssemblies = new string[] { "clientAssembly" },
                ServerAssemblies = new string[] { "serverAssembly" },
                ClientAssemblyPathsNormalized = new string[] { "clientPaths" },
                SharedSourceFiles = new string[] { "sharedSourceFiles" },
                SymbolSearchPaths = new string[] { "symSearch" }
            };
            ConsoleLogger logger = new ConsoleLogger();

            using (SharedCodeService sts = new SharedCodeService(parameters, logger))
            {
            }
        }

        [DeploymentItem(@"Luma.SimpleEntity.Tests\ProjectPath.txt", "STS1")]
        [Description("SharedCodeService locates shared types between projects")]
        [TestMethod]
        public void SharedCodeService_Types()
        {
            string projectPath, outputPath;
            TestHelper.GetProjectPaths("STS1", out projectPath, out outputPath);
            var clientProjectPath = CodeGenHelper.ClientClassLibProjectPath(projectPath);

            var logger = new ConsoleLogger();
            using (var scs = CodeGenHelper.CreateSharedCodeService(clientProjectPath, logger))
            {
                // TestEntity is shared because it is linked
                CodeMemberShareKind shareKind = scs.GetTypeShareKind(typeof(TestEntity).AssemblyQualifiedName);
                Assert.AreEqual(CodeMemberShareKind.SharedByReference, shareKind, "Expected TestEntity type to be shared by reference");

                // TestValidator is shared because it is linked
                shareKind = scs.GetTypeShareKind(typeof(TestValidator).AssemblyQualifiedName);
                Assert.AreEqual(CodeMemberShareKind.SharedByReference, shareKind, "Expected TestValidator type to be shared by reference");

                // SharedClass is shared because it is linked
                shareKind = scs.GetTypeShareKind(typeof(SharedClass).AssemblyQualifiedName);
                Assert.IsTrue(shareKind == CodeMemberShareKind.SharedBySource, "Expected SharedClass type to be shared in source");

                // TestValidatorServer exists only on the server and is not shared
                shareKind = scs.GetTypeShareKind(typeof(TestValidatorServer).AssemblyQualifiedName);
                Assert.IsTrue(shareKind == CodeMemberShareKind.NotShared, "Expected TestValidatorServer type not to be shared");

                // CodelessType exists on both server and client, but lacks all user code necessary
                // to determine whether it is shared.  Because it compiles into both projects, it should
                // be considered shared by finding the type in both assemblies
                shareKind = scs.GetTypeShareKind(typeof(CodelessType).AssemblyQualifiedName);
                Assert.IsTrue(shareKind == CodeMemberShareKind.SharedByReference, "Expected CodelessType type to be shared in assembly");
            }
        }

        [DeploymentItem(@"Luma.SimpleEntity.Tests\ProjectPath.txt", "STS2")]
        [Description("SharedCodeService locates shared properties between projects")]
        [TestMethod]
        public void SharedCodeService_Properties()
        {
            string projectPath, outputPath;
            TestHelper.GetProjectPaths("STS2", out projectPath, out outputPath);
            var clientProjectPath = CodeGenHelper.ClientClassLibProjectPath(projectPath);

            var logger = new ConsoleLogger();
            using (var scs = CodeGenHelper.CreateSharedCodeService(clientProjectPath, logger))
            {
                CodeMemberShareKind shareKind = scs.GetPropertyShareKind(typeof(TestEntity).AssemblyQualifiedName, "ServerAndClientValue");
                Assert.AreEqual(CodeMemberShareKind.SharedByReference, shareKind, "Expected TestEntity.ServerAndClientValue property to be shared by reference.");

                shareKind = scs.GetPropertyShareKind(typeof(TestEntity).AssemblyQualifiedName, "TheValue");
                Assert.AreEqual(CodeMemberShareKind.NotShared, shareKind, "Expected TestEntity.TheValue property not to be shared in source.");
            }
        }


        [DeploymentItem(@"Luma.SimpleEntity.Tests\ProjectPath.txt", "STS4")]
        [Description("SharedCodeService locates shared methods between projects")]
        [TestMethod]
        public void SharedCodeService_Methods()
        {
            string projectPath = null;
            string outputPath = null;
            TestHelper.GetProjectPaths("STS4", out projectPath, out outputPath);
            string clientProjectPath = CodeGenHelper.ClientClassLibProjectPath(projectPath);

            ConsoleLogger logger = new ConsoleLogger();
            using (SharedCodeService sts = CodeGenHelper.CreateSharedCodeService(clientProjectPath, logger))
            {
                CodeMemberShareKind shareKind = sts.GetMethodShareKind(typeof(TestValidator).AssemblyQualifiedName, "IsValid", new string[] { typeof(TestEntity).AssemblyQualifiedName, typeof(ValidationContext).AssemblyQualifiedName });
                Assert.AreEqual(CodeMemberShareKind.SharedByReference, shareKind, "Expected TestValidator.IsValid to be shared by reference");

                shareKind = sts.GetMethodShareKind(typeof(TestEntity).AssemblyQualifiedName, "ServerAndClientMethod", new string[0]);
                Assert.AreEqual(CodeMemberShareKind.SharedByReference, shareKind, "Expected TestValidator.ServerAndClientMethod to be shared by reference");

                shareKind = sts.GetMethodShareKind(typeof(TestEntity).AssemblyQualifiedName, "ServerMethod", new string[0]);
                Assert.AreEqual(CodeMemberShareKind.NotShared, shareKind, "Expected TestValidator.ServerMethod not to be shared");

                shareKind = sts.GetMethodShareKind(typeof(TestValidatorServer).AssemblyQualifiedName, "IsValid", new string[] { typeof(TestEntity).AssemblyQualifiedName, typeof(ValidationContext).AssemblyQualifiedName });
                Assert.AreEqual(CodeMemberShareKind.NotShared, shareKind, "Expected TestValidator.IsValid not to be shared");

                TestHelper.AssertNoErrorsOrWarnings(logger);
            }
        }

        [DeploymentItem(@"Luma.SimpleEntity.Tests\ProjectPath.txt", "STS5")]
        [Description("SharedCodeService locates shared ctors between projects")]
        [TestMethod]
        public void SharedCodeService_Ctors()
        {
            string projectPath = null;
            string outputPath = null;
            TestHelper.GetProjectPaths("STS5", out projectPath, out outputPath);
            string clientProjectPath = CodeGenHelper.ClientClassLibProjectPath(projectPath);

            ConsoleLogger logger = new ConsoleLogger();
            using (SharedCodeService sts = CodeGenHelper.CreateSharedCodeService(clientProjectPath, logger))
            {
                ConstructorInfo ctor = typeof(TestValidator).GetConstructor(new Type[] { typeof(string)});
                Assert.IsNotNull("Failed to find string ctor on TestValidator");
                CodeMemberShareKind shareKind = sts.GetMethodShareKind(typeof(TestValidator).AssemblyQualifiedName, ctor.Name , new string[] { typeof(string).AssemblyQualifiedName });
                Assert.AreEqual(CodeMemberShareKind.SharedByReference, shareKind, "Expected TestValidator ctor to be shared by reference");
                TestHelper.AssertNoErrorsOrWarnings(logger);
            }
        }
    }
}
