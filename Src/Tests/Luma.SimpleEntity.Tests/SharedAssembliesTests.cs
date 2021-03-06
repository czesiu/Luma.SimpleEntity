﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
    public class SharedAssembliesTests
    {
        [DeploymentItem(@"Luma.SimpleEntity.Tests\ProjectPath.txt", "SAT")]
        [Description("SharedAssemblies service locates shared types between projects")]
        [TestMethod]
        public void SharedAssemblies_Types()
        {
            string projectPath = null;
            string outputPath = null;
            TestHelper.GetProjectPaths("SAT", out projectPath, out outputPath);
            string clientProjectPath = CodeGenHelper.ClientClassLibProjectPath(projectPath);
            List<string> assemblies = CodeGenHelper.ClientClassLibReferences(clientProjectPath, true);

            var logger = new ConsoleLogger();
            var sa = new SharedAssemblies(assemblies, Enumerable.Empty<string>(), logger);

            Type sharedType = sa.GetSharedType(typeof(TestEntity).AssemblyQualifiedName);
            Assert.IsNotNull(sharedType, "Expected TestEntity type to be shared");
            Assert.IsTrue(sharedType.Assembly.Location.Contains("ClientClassLib"), "Expected to find type in client class lib");

            sharedType = sa.GetSharedType(typeof(TestValidator).AssemblyQualifiedName);
            Assert.IsNotNull(sharedType, "Expected TestValidator type to be shared");
            Assert.IsTrue(sharedType.Assembly.Location.Contains("ClientClassLib"), "Expected to find type in client class lib");

            sharedType = sa.GetSharedType(typeof(TestValidatorServer).AssemblyQualifiedName);
            Assert.IsNull(sharedType, "Expected TestValidatorServer type not to be shared");

            TestHelper.AssertNoErrorsOrWarnings(logger);
        }

        [DeploymentItem(@"Luma.SimpleEntity.Tests\ProjectPath.txt", "SAT")]
        [Description("SharedAssemblies service locates shared methods between projects")]
        [TestMethod]
        public void SharedAssemblies_Methods()
        {
            string projectPath, outputPath;
            TestHelper.GetProjectPaths("SAT", out projectPath, out outputPath);
            var clientProjectPath = CodeGenHelper.ClientClassLibProjectPath(projectPath);
            var assemblies = CodeGenHelper.ClientClassLibReferences(clientProjectPath, true);

            var logger = new ConsoleLogger();
            var sa = new SharedAssemblies(assemblies, Enumerable.Empty<string>(), logger);

            var sharedMethod = sa.GetSharedMethod(typeof(TestValidator).AssemblyQualifiedName, "IsValid", new[] { typeof(TestEntity).AssemblyQualifiedName, typeof(ValidationContext).AssemblyQualifiedName });
            Assert.IsNotNull(sharedMethod, "Expected TestValidator.IsValid to be shared");
            Assert.IsTrue(sharedMethod.DeclaringType.Assembly.Location.Contains("ClientClassLib"), "Expected to find method in client class lib");

            sharedMethod = sa.GetSharedMethod(typeof(TestEntity).AssemblyQualifiedName, "ServerAndClientMethod", new string[0]);
            Assert.IsNotNull(sharedMethod, "Expected TestEntity.ServerAndClientMethod to be shared");
            Assert.IsTrue(sharedMethod.DeclaringType.Assembly.Location.Contains("ClientClassLib"), "Expected to find method in client class lib");

            sharedMethod = sa.GetSharedMethod(typeof(TestValidator).AssemblyQualifiedName, "ServertMethod", new string[0]);
            Assert.IsNull(sharedMethod, "Expected TestValidator.ServerMethod not to be shared");

            TestHelper.AssertNoErrorsOrWarnings(logger);

        }

        [DeploymentItem(@"Luma.SimpleEntity.Tests\ProjectPath.txt", "SAT")]
        [Description("SharedAssemblies matches mscorlib types and methods")]
        [WorkItem(723391)]  // XElement entry below is regression for this
        [TestMethod]
        public void SharedAssemblies_Matches_MsCorLib()
        {
            Type[] sharedTypes = new Type[] {
                typeof(Int32),
                typeof(string),
                typeof(Decimal),
            };

            Type[] nonSharedTypes = new Type[] {
                typeof(SerializableAttribute),
                typeof(System.Xml.Linq.XElement)
            };

            MethodBase[] sharedMethods = new MethodBase[] {
                typeof(string).GetMethod("CopyTo"),
            };

            string projectPath, outputPath;

            TestHelper.GetProjectPaths("SAT", out projectPath, out outputPath);
            string clientProjectPath = CodeGenHelper.ClientClassLibProjectPath(projectPath);
            List<string> assemblies = CodeGenHelper.ClientClassLibReferences(clientProjectPath, true);

            var logger = new ConsoleLogger();
            var sa = new SharedAssemblies(assemblies, Enumerable.Empty<string>(), logger);

            foreach (Type t in sharedTypes)
            {
                Type sharedType = sa.GetSharedType(t.AssemblyQualifiedName);
                Assert.IsNotNull(sharedType, "Expected type " + t.Name + " to be considered shared.");
            }

            foreach (MethodBase m in sharedMethods)
            {
                string[] parameterTypes = m.GetParameters().Select<ParameterInfo, string>(p => p.ParameterType.AssemblyQualifiedName).ToArray();
                MethodBase sharedMethod = sa.GetSharedMethod(m.DeclaringType.AssemblyQualifiedName, m.Name, parameterTypes);
                Assert.IsNotNull(sharedMethod, "Expected method " + m.DeclaringType.Name + "." + m.Name + " to be considered shared.");
            }

            foreach (Type t in nonSharedTypes)
            {
                Type sType = sa.GetSharedType(t.AssemblyQualifiedName);
                Assert.IsNull(sType, "Expected type " + t.Name + " to be considered *not* shared.");
            }

        }



        [DeploymentItem(@"Luma.SimpleEntity.Tests\ProjectPath.txt", "SAT")]
        [Description("SharedAssemblies service logs an info message for nonexistent assembly file")]
        [TestMethod]
        public void SharedAssemblies_Logs_Message_NonExistent_Assembly()
        {
            var logger = new ConsoleLogger();
            var file = "DoesNotExist.dll";
            var sa = new SharedAssemblies(new[] { file }, Enumerable.Empty<string>(), logger);

            Type sharedType = sa.GetSharedType(typeof(TestEntity).AssemblyQualifiedName);
            Assert.IsNull(sharedType, "Should not have detected any shared type.");

            string errorMessage = null;
            try{
                Assembly.ReflectionOnlyLoadFrom(file);
            }
            catch (FileNotFoundException fnfe)
            {
                errorMessage = fnfe.Message;
            }
            string message = string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Assembly_Load_Error, file, errorMessage);
            TestHelper.AssertContainsMessages(logger, message);
        }

        [DeploymentItem(@"Luma.SimpleEntity.Tests\ProjectPath.txt", "SAT")]
        [Description("SharedAssemblies service logs an info message for bad image format assembly file")]
        [TestMethod]
        public void SharedAssemblies_Logs_Message_BadImageFormat_Assembly()
        {
            // Create fake DLL with bad image 
            var assemblyFileName = Path.Combine(Path.GetTempPath(), (Guid.NewGuid().ToString() + ".dll"));
            File.WriteAllText(assemblyFileName, "neener neener neener");

            var logger = new ConsoleLogger();
            var sa = new SharedAssemblies(new[] { assemblyFileName }, Enumerable.Empty<string>(), logger);

            Type sharedType = sa.GetSharedType(typeof(TestEntity).AssemblyQualifiedName);
            Assert.IsNull(sharedType, "Should not have detected any shared type.");

            string errorMessage = null;
            try
            {
                Assembly.ReflectionOnlyLoadFrom(assemblyFileName);
            }
            catch (BadImageFormatException bife)
            {
                errorMessage = bife.Message;
            }
            finally
            {
                File.Delete(assemblyFileName);
            }
            string message = string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Assembly_Load_Error, assemblyFileName, errorMessage);
            TestHelper.AssertContainsMessages(logger, message);
        }
    }

}
