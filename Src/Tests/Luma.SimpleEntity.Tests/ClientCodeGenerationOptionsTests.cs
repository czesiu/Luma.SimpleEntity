﻿using Luma.SimpleEntity.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Luma.SimpleEntity.Tests
{

    /// <summary>
    /// Tests for custom build task to generate client proxies
    /// </summary>
    [TestClass]
    public class ClientCodeGenerationOptionTests
    {
        [Description("ClientCodeGenerationOptionTests properties getter and setter tests")]
        [TestMethod]
        public void ClientCodeGenerationOptionTests_Properties()
        {
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions();

            // Verify defaults
            Assert.IsNull(options.Language);
            Assert.IsNull(options.ClientRootNamespace);
            Assert.IsNull(options.ServerRootNamespace);
            Assert.IsNull(options.ClientProjectPath);
            Assert.IsNull(options.ServerProjectPath);
            Assert.IsFalse(options.UseFullTypeNames);

            // Null languge throws
            ExceptionHelper.ExpectArgumentNullException(() => options.Language = null, Resource.Null_Language_Property, "value");
            ExceptionHelper.ExpectArgumentNullException(() => options.Language = string.Empty, Resource.Null_Language_Property, "value");

            // Now test a range of values for each property
            foreach (string language in new string[] { "C#", "VB", "notALanguage" })
            {
                options.Language = language;
                Assert.AreEqual(language, options.Language);
            }

            foreach (string rootNamespace in new string[] { null, string.Empty, "testRoot" })
            {
                options.ClientRootNamespace = rootNamespace;
                Assert.AreEqual(rootNamespace, options.ClientRootNamespace);
            }

            foreach (string rootNamespace in new string[] { null, string.Empty, "testRoot" })
            {
                options.ServerRootNamespace = rootNamespace;
                Assert.AreEqual(rootNamespace, options.ServerRootNamespace);
            }

            foreach (string projectName in new string[] { null, string.Empty, "testProj" })
            {
                options.ClientProjectPath = projectName;
                Assert.AreEqual(projectName, options.ClientProjectPath);
            }

            foreach (string projectName in new string[] { null, string.Empty, "testProj" })
            {
                options.ServerProjectPath = projectName;
                Assert.AreEqual(projectName, options.ServerProjectPath);
            }

            foreach (bool theBool in new bool[] { true, false })
            {
                options.UseFullTypeNames = theBool;
                Assert.AreEqual(theBool, options.UseFullTypeNames);
            }
        }
    }
}