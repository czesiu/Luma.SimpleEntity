using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Luma.SimpleEntity.Tools.SharedTypes;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Luma.SimpleEntity.Tests
{

    public static class CodeGenHelper
    {
        public static void AssertGenerated(string generatedCode, string expected)
        {
            string normalizedGenerated = TestHelper.NormalizeWhitespace(generatedCode);
            string normalizedExpected = TestHelper.NormalizeWhitespace(expected);
            Assert.IsTrue(normalizedGenerated.IndexOf(normalizedExpected) >= 0, "Expected <" + expected + "> but saw\r\n<" + generatedCode + ">");
        }

        public static void AssertNotGenerated(string generatedCode, string notExpected)
        {
            string normalizedGenerated = TestHelper.NormalizeWhitespace(generatedCode);
            string normalizedNotExpected = TestHelper.NormalizeWhitespace(notExpected);
            Assert.IsTrue(normalizedGenerated.IndexOf(normalizedNotExpected) < 0, "Did not expect <" + notExpected + ">");
        }

        /// <summary>
        /// Assert that all the files named by shortNames exist in the files collection
        /// </summary>
        /// <param name="files">list of files to check</param>
        /// <param name="projectPath">project owning the short named files</param>
        /// <param name="shortNames">collection of file short names that must be in list</param>
        public static void AssertContainsOnlyFiles(List<string> files, string projectPath, string[] shortNames)
        {
            Assert.AreEqual(shortNames.Length, files.Count);
            AssertContainsFiles(files, projectPath, shortNames);
        }

        /// <summary>
        /// Assert that all the files named by shortNames exist in the files collection
        /// </summary>
        /// <param name="files">list of files to check</param>
        /// <param name="projectPath">project owning the short named files</param>
        /// <param name="shortNames">collection of file short names that must be in list</param>
        public static void AssertContainsFiles(List<string> files, string projectPath, string[] shortNames)
        {
            foreach (string shortName in shortNames)
            {
                string fullName = Path.Combine(Path.GetDirectoryName(projectPath), shortName);
                bool foundIt = false;
                foreach (string file in files)
                {
                    if (file.Equals(fullName, StringComparison.OrdinalIgnoreCase))
                    {
                        foundIt = true;
                        break;
                    }
                }
                if (!foundIt)
                {
                    string allFiles = string.Empty;
                    foreach (string file in files)
                        allFiles += ("\r\n" + file);

                    Assert.Fail("Expected to find " + fullName + " in list of files, but saw instead:" + allFiles);
                }
            }
        }

        public static string GetOutputFile(ITaskItem[] items, string shortName)
        {
            for (int i = 0; i < items.Length; ++i)
            {
                if (Path.GetFileName(items[i].ItemSpec).Equals(shortName, StringComparison.OrdinalIgnoreCase))
                {
                    string fileName = items[i].ItemSpec;
                    Assert.IsTrue(File.Exists(fileName), "Expected file " + fileName + " to have been created.");
                    return fileName;
                }
            }
            Assert.Fail("Expected to find output file " + shortName);
            return null;
        }

        /// <summary>
        /// Returns the name of the assembly built by the server project
        /// </summary>
        /// <param name="serverProjectPath"></param>
        /// <returns></returns>
        public static string ServerClassLibOutputAssembly(string serverProjectPath)
        {
            // We need to map any server side assembly references back to our deployment directory
            // if we have the same assembly there, otherwise the assembly load from calls end up
            // with multiple assemblies with the same types
            string deploymentDir = Path.GetDirectoryName(typeof(CodeGenHelper).Assembly.Location);
            string assembly = MsBuildHelper.GetOutputAssembly(serverProjectPath);
            return MapAssemblyReferenceToDeployment(deploymentDir, assembly);
        }

        /// <summary>
        /// Returns the collection of assembly references from the server project
        /// </summary>
        /// <param name="serverProjectPath"></param>
        /// <returns></returns>
        public static List<string> ServerClassLibReferences(string serverProjectPath)
        {
            // We need to map any server side assembly references back to our deployment directory
            // if we have the same assembly there, otherwise the assembly load from calls end up
            // with multiple assemblies with the same types
            string deploymentDir = Path.GetDirectoryName(typeof(CodeGenHelper).Assembly.Location);
            List<string> assemblies = MsBuildHelper.GetReferenceAssemblies(serverProjectPath);
            MapAssemblyReferencesToDeployment(deploymentDir, assemblies);
            return assemblies;
        }

        /// <summary>
        /// Returns the collection of source files from the server
        /// </summary>
        /// <param name="serverProjectPath"></param>
        /// <returns></returns>
        public static List<string> ServerClassLibSourceFiles(string serverProjectPath)
        {
            return MsBuildHelper.GetSourceFiles(serverProjectPath);
        }

        /// <summary>
        /// Returns the collection of assembly references from the client project
        /// </summary>
        /// <param name="clientProjectPath"></param>
        /// <returns></returns>
        public static List<string> ClientClassLibReferences(string clientProjectPath, bool includeClientOutputAssembly)
        {
            List<string> references = MsBuildHelper.GetReferenceAssemblies(clientProjectPath);

            // Note: we conditionally add the output assembly to enable this unit test to
            // define some shared types 
            if (includeClientOutputAssembly)
            {
                references.Add(MsBuildHelper.GetOutputAssembly(clientProjectPath));
            }

            // Remove mscorlib -- it causes problems using ReflectionOnlyLoad ("parent does not exist")
            for (int i = 0; i < references.Count; ++i)
            {
                if (Path.GetFileName(references[i]).Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase))
                {
                    references.RemoveAt(i);
                    break;
                }
            }
            return references;
        }

        /// <summary>
        /// Returns the collection of source files from the client
        /// </summary>
        /// <param name="clientProjectPath"></param>
        /// <returns></returns>
        public static List<string> ClientClassLibSourceFiles(string clientProjectPath)
        {
            return MsBuildHelper.GetSourceFiles(clientProjectPath);
        }


        /// <summary>
        /// Returns the full path of the server project based on our current project
        /// </summary>
        /// <param name="currentProjectPath"></param>
        /// <returns></returns>
        public static string ServerClassLibProjectPath(string currentProjectPath)
        {
            return Path.Combine(Path.GetDirectoryName(currentProjectPath), @"ServerClassLib\ServerClassLib.csproj");
        }

        /// <summary>
        /// Returns the full path of the server WAP project based on our current project
        /// </summary>
        /// <param name="currentProjectPath"></param>
        /// <returns></returns>
        public static string ServerWapProjectPath(string currentProjectPath)
        {
            return Path.Combine(Path.GetDirectoryName(currentProjectPath), @"TestWAP\TestWAP.csproj");
        }

        /// <summary>
        /// Returns the full path of the 2nd server project based on our current project
        /// </summary>
        /// <param name="currentProjectPath"></param>
        /// <returns></returns>
        public static string ServerClassLib2ProjectPath(string currentProjectPath)
        {
            return Path.Combine(Path.GetDirectoryName(currentProjectPath), @"ServerClassLib2\ServerClassLib2.csproj");
        }

        /// <summary>
        /// Returns the full path of the client project based on our current project
        /// </summary>
        /// <param name="currentProjectPath"></param>
        /// <returns></returns>
        public static string ClientClassLibProjectPath(string currentProjectPath)
        {
            return Path.Combine(Path.GetDirectoryName(currentProjectPath), @"ClientClassLib\ClientClassLib.csproj");
        }

        /// <summary>
        /// Returns the full path of the 2nd client project based on our current project
        /// </summary>
        /// <param name="currentProjectPath"></param>
        /// <returns></returns>
        public static string ClientClassLib2ProjectPath(string currentProjectPath)
        {
            return Path.Combine(Path.GetDirectoryName(currentProjectPath), @"ClientClassLib2\ClientClassLib2.csproj");
        }

        /// <summary>
        /// When running unit tests, assemblies we are analyzing may come from one place,
        /// but VSTT has copied a version locally that we are running.  This will cause
        /// confusion, so map all assembly references that have a local equivalent to
        /// that local version.
        /// </summary>
        /// <param name="referenceAssemblies"></param>
        public static void MapAssemblyReferencesToDeployment(string deploymentDir, IList<string> assemblies)
        {
            for (int i = 0; i < assemblies.Count; ++i)
            {
                assemblies[i] = MapAssemblyReferenceToDeployment(deploymentDir, assemblies[i]);
            }
        }

        public static string MapAssemblyReferenceToDeployment(string deploymentDir, string assembly)
        {
            string localPath = Path.Combine(deploymentDir, Path.GetFileName(assembly));
            if (File.Exists(localPath))
            {
                assembly = localPath;
            }
            return assembly;
        }

        internal static SharedCodeService CreateSharedCodeService(string clientProjectPath, ILoggingService logger)
        {
            var sourceFiles = ClientClassLibSourceFiles(clientProjectPath);
            var assemblies = ClientClassLibReferences(clientProjectPath, true);

            var parameters = new SharedCodeServiceParameters
            {
                SharedSourceFiles = sourceFiles.ToArray(),
                ClientAssemblies = assemblies.ToArray(),
                ClientAssemblyPathsNormalized = Enumerable.Empty<string>().ToArray()
            };

            var sts = new SharedCodeService(parameters, logger);

            return sts;
        }

        /// <summary>
        /// Generate a temporary folder for generating code
        /// </summary>
        /// <returns></returns>
        public static string GenerateTempFolder()
        {
            string rootPath = Path.GetTempPath();
            string tempFolder = Path.Combine(rootPath, Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolder);
            return tempFolder;
        }

        /// <summary>
        /// Delete the temporary folder provided by GenerateTempFolder
        /// </summary>
        /// <param name="tempFolder"></param>
        public static void DeleteTempFolder(string tempFolder)
        {
            try
            {
                if (tempFolder.StartsWith(Path.GetTempPath()))
                {
                    RecursiveDelete(tempFolder);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                System.Diagnostics.Debug.WriteLine("Failed to delete temp folder: " + tempFolder);
            }
        }

        /// <summary>
        /// Deletes all the files and folders created by the given CreteClientFilesTask
        /// </summary>
        /// <param name="task"></param>
        public static void DeleteTempFolder(CreateClientFilesTask task)
        {
            if (task != null)
            {
                string tempFolder = Path.GetDirectoryName(task.OutputPath);
                DeleteTempFolder(tempFolder);
            }
        }

        /// <summary>
        /// Deletes the given folder and everything inside it
        /// </summary>
        /// <param name="dir"></param>
        public static void RecursiveDelete(string dir)
        {
            if (!System.IO.Directory.Exists(dir))
            {
                return;
            }
            //get all the subdirectories in the given directory
            string[] dirs = Directory.GetDirectories(dir);
            for (int i = 0; i < dirs.Length; i++)
            {
                RecursiveDelete(dirs[i]);
            }
            string[] files = Directory.GetFiles(dir);

            foreach (string file in files)
            {
                FileInfo fInfo = new FileInfo(file);
                fInfo.Attributes &= ~(FileAttributes.ReadOnly);
                File.Delete(file);
            }

            Directory.Delete(dir);
        }

        /// <summary>
        /// Creates a new CreateClientFilesTask instance to use to generate code
        /// </summary>
        /// <param name="relativeTestDir"></param>
        /// <param name="includeClientOutputAssembly">if <c>true</c> include clients own output assembly in analysis</param>
        /// <returns>A new task instance that can be invoked to do code gen</returns>
        public static CreateClientFilesTask CreateClientFilesTaskInstance(string relativeTestDir, bool includeClientOutputAssembly)
        {
            string projectPath, outputPath;
            TestHelper.GetProjectPaths(relativeTestDir, out projectPath, out outputPath);

            Assert.IsTrue(File.Exists(projectPath), "Could not locate " + projectPath + " necessary for test.");
            string serverProjectPath = ServerClassLibProjectPath(projectPath);
            string clientProjectPath = ClientClassLibProjectPath(projectPath);

            return CreateClientFilesTaskInstance(serverProjectPath, clientProjectPath, includeClientOutputAssembly);
        }

        /// <summary>
        /// Creates a new CreateClientFilesTask instance to use to generate code
        /// </summary>
        /// <param name="serverProjectPath">The file path to the server project</param>
        /// <param name="clientProjectPath">The file path to the client project</param>
        /// <param name="includeClientOutputAssembly">if <c>true</c> include client's own output assembly in analysis</param>
        /// <returns>A new task instance that can be invoked to do code gen</returns>
        public static CreateClientFilesTask CreateClientFilesTaskInstance(string serverProjectPath, string clientProjectPath, bool includeClientOutputAssembly)
        {
            var task = new CreateClientFilesTask();

            MockBuildEngine mockBuildEngine = new MockBuildEngine();
            task.BuildEngine = mockBuildEngine;

            task.Language = "C#";

            task.ServerProjectPath = serverProjectPath;
            task.ServerAssemblies = new TaskItem[] { new TaskItem(CodeGenHelper.ServerClassLibOutputAssembly(task.ServerProjectPath)) };
            task.ServerReferenceAssemblies = MsBuildHelper.AsTaskItems(CodeGenHelper.ServerClassLibReferences(task.ServerProjectPath)).ToArray();

            task.ClientProjectPath = clientProjectPath;
            task.ClientReferenceAssemblies = MsBuildHelper.AsTaskItems(CodeGenHelper.ClientClassLibReferences(clientProjectPath, includeClientOutputAssembly)).ToArray();
            task.ClientSourceFiles = MsBuildHelper.AsTaskItems(CodeGenHelper.ClientClassLibSourceFiles(clientProjectPath)).ToArray();
            task.ClientFrameworkPath = GetRuntimeDirectory();

            // Generate the code to our deployment directory
            string tempFolder = CodeGenHelper.GenerateTempFolder();
            task.OutputPath = Path.Combine(tempFolder, "FileWrites");
            task.GeneratedCodePath = Path.Combine(tempFolder, "Generated_Code");

            return task;
        }

        public static string GetRuntimeDirectory()
        {
            return "C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETPortable\\v4.5\\Profile\\Profile78";
        }

        /// <summary>
        /// Creates a new CreateClientFilesTask instance to use to generate code
        /// </summary>
        /// <param name="relativeTestDir"></param>
        /// <param name="includeClientOutputAssembly">if <c>true</c> include clients own output assembly in analysis</param>
        /// <returns>A new task instance that can be invoked to do code gen</returns>
        public static CreateClientFilesTask CreateClientFilesTaskInstance_CopyClientProjectToOutput(string relativeTestDir, bool includeClientOutputAssembly)
        {
            string deploymentDir = Path.GetDirectoryName(typeof(CodeGenHelper).Assembly.Location);
            string projectPath = null;
            string outputPath = null;
            TestHelper.GetProjectPaths(relativeTestDir, out projectPath, out outputPath);

            Assert.IsTrue(File.Exists(projectPath), "Could not locate " + projectPath + " necessary for test.");
            string serverProjectPath = CodeGenHelper.ServerClassLibProjectPath(projectPath);
            string clientProjectPath = CodeGenHelper.ClientClassLibProjectPath(projectPath);

            return CodeGenHelper.CreateClientFilesTaskInstance_CopyClientProjectToOutput(serverProjectPath, clientProjectPath, includeClientOutputAssembly);
        }

        /// <summary>
        /// Creates a new CreateClientFilesTask instance to use to generate code
        /// </summary>
        /// <param name="relativeTestDir"></param>
        /// <param name="includeClientOutputAssembly">if <c>true</c> include clients own output assembly in analysis</param>
        /// <returns>A new task instance that can be invoked to do code gen</returns>
        public static CreateClientFilesTask CreateClientFilesTaskInstance_CopyClientProjectToOutput(string serverProjectPath, string clientProjectPath, bool includeClientOutputAssembly)
        {
            CreateClientFilesTask task = new CreateClientFilesTask();

            MockBuildEngine mockBuildEngine = new MockBuildEngine();
            task.BuildEngine = mockBuildEngine;

            task.Language = "C#";

            task.ServerProjectPath = serverProjectPath;
            task.ServerAssemblies = new TaskItem[] { new TaskItem(CodeGenHelper.ServerClassLibOutputAssembly(task.ServerProjectPath)) };
            task.ServerReferenceAssemblies = MsBuildHelper.AsTaskItems(CodeGenHelper.ServerClassLibReferences(task.ServerProjectPath)).ToArray();
            task.ClientFrameworkPath = CodeGenHelper.GetRuntimeDirectory();

            // Generate the code to our deployment directory
            string tempFolder = CodeGenHelper.GenerateTempFolder();
            task.OutputPath = Path.Combine(tempFolder, "FileWrites");
            task.GeneratedCodePath = Path.Combine(tempFolder, "Generated_Code");

            string clientProjectFileName = Path.GetFileName(clientProjectPath);
            string clientProjectDestPath = Path.Combine(tempFolder, clientProjectFileName);
            File.Copy(clientProjectPath, clientProjectDestPath);
            task.ClientProjectPath = clientProjectDestPath;
            task.ClientReferenceAssemblies = MsBuildHelper.AsTaskItems(CodeGenHelper.ClientClassLibReferences(clientProjectPath, includeClientOutputAssembly)).ToArray();
            task.ClientSourceFiles = MsBuildHelper.AsTaskItems(CodeGenHelper.ClientClassLibSourceFiles(clientProjectPath)).ToArray();

            return task;
        }

        /// <summary>
        /// Creates a new CreateClientFilesTask instance to use to generate code
        /// using the TestWap project.
        /// </summary>
        /// <param name="relativeTestDir"></param>
        /// <returns>A new task instance that can be invoked to do code gen</returns>
        public static CreateClientFilesTask CreateClientFilesTaskInstanceForWAP(string relativeTestDir)
        {
            var deploymentDir = Path.GetDirectoryName(typeof(CodeGenHelper).Assembly.Location);
            string projectPath, outputPath;
            TestHelper.GetProjectPaths(relativeTestDir, out projectPath, out outputPath);

            Assert.IsTrue(File.Exists(projectPath), "Could not locate " + projectPath + " necessary for test.");
            string serverProjectPath = ServerWapProjectPath(projectPath);
            string clientProjectPath = ClientClassLibProjectPath(projectPath);

            return CodeGenHelper.CreateClientFilesTaskInstance(serverProjectPath, clientProjectPath, false);
        }
    }
}
