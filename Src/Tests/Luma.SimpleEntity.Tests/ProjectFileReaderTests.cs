﻿using System;
using System.Globalization;
using System.IO;
using Luma.SimpleEntity.TestHelpers;
using Luma.SimpleEntity.Tools;
using Microsoft.Build.BuildEngine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ConsoleLogger = Luma.SimpleEntity.Tests.Utilities.ConsoleLogger;

namespace Luma.SimpleEntity.Tests
{
    /// <summary>
    /// Tests the project file reader object
    /// </summary>
    [TestClass]
    public class ProjectFileReaderTests
    {
        [TestMethod]
        [Description("Tests all positive and negative ctor patterns for ProjectFileReader")]
        public void ProjectFileReader_Ctor()
        {
            ConsoleLogger logger = new ConsoleLogger();

            ExceptionHelper.ExpectArgumentNullExceptionStandard(() => new ProjectFileReader(null), "logger");

            using (ProjectFileReader projectFileReader = new ProjectFileReader(logger))
            {
            }
        }

        [TestMethod]
        [Description("ProjectFileReader survives multiple Dispose calls and GC")]
        public void ProjectFileReader_Dispose_Multiple()
        {
            ConsoleLogger logger = new ConsoleLogger();
            ProjectFileReader projectFileReader;
            using (projectFileReader = new ProjectFileReader(logger))
            {
            }
            projectFileReader.Dispose();
            projectFileReader.Dispose();
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            projectFileReader.Dispose();
        }

        [TestMethod]
        [Description("ProjectFileReader warns about non-existent project file")]
        public void ProjectFileReader_Nonexistent_Project_File_Warns()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string badProjectPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csproj");
            using (ProjectFileReader projectFileReader = new ProjectFileReader(logger))
            {
                try
                {
                    projectFileReader.LoadProject(badProjectPath);

                    string warningMessage = string.Format(CultureInfo.CurrentCulture, Resource.Project_Does_Not_Exist, badProjectPath);

                    TestHelper.AssertContainsWarnings(logger, new string[] { warningMessage });
                }
                finally
                {
                    File.Delete(badProjectPath);
                }
            }
        }


        [TestMethod]
        [Description("ProjectSourceFileCache warns about invalid project file")]
        public void ProjectFileReader_Bad_Project_File_Warns()
        {
            ConsoleLogger logger = new ConsoleLogger();
            using (ProjectFileReader projectFileReader = new ProjectFileReader(logger))
            {
                string badProjectPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csproj");
                File.WriteAllText(badProjectPath, "neener neener");

                try
                {
                    projectFileReader.LoadProject(badProjectPath);

                    // Simulate the exception so we get the exact text
                    string warningMessage = null;
                    try
                    {
                        Engine engine = new Engine();
                        Project project = new Project(engine);
                        project.Load(badProjectPath);

                    }
                    catch (InvalidProjectFileException ipfe)
                    {
                        warningMessage = string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Open_Project, badProjectPath, ipfe.Message);
                    }

                    TestHelper.AssertContainsWarnings(logger, new string[] { warningMessage });
                }
                finally
                {
                    File.Delete(badProjectPath);
                }
            }
        }
    }
}
