using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Luma.SimpleEntity.Tests
{
    internal class CompilerHelper
    {
        /// <summary>
        /// Invokes CSC to build the given files against the given set of references
        /// </summary>
        /// <param name="files"></param>
        /// <param name="referenceAssemblies"></param>
        /// <param name="lang"></param>
        /// <param name="documentationFile">If nonblank, the documentation file to generate during the compile.</param>
        public static bool CompileCSharpSource(IEnumerable<string> files, IEnumerable<string> referenceAssemblies, string documentationFile)
        {
            List<ITaskItem> sources = new List<ITaskItem>();
            foreach (string f in files)
                sources.Add(new TaskItem(f));

            List<ITaskItem> references = new List<ITaskItem>();
            foreach (string s in referenceAssemblies)
                references.Add(new TaskItem(s));

            Csc csc = new Csc();
            MockBuildEngine buildEngine = new MockBuildEngine();
            csc.BuildEngine = buildEngine;  // needed before task can log

            csc.NoStandardLib = true;   // don't include std lib stuff -- we're feeding it pcl
            csc.NoConfig = true;        // don't load the csc.rsp file to get references
            csc.TargetType = "library";
            csc.Sources = sources.ToArray();
            csc.References = references.ToArray();

            if (!string.IsNullOrEmpty(documentationFile))
            {
                csc.DocumentationFile = documentationFile;
            }
 
            bool result = false;
            try
            {
                result = csc.Execute();
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception occurred invoking CSC task on " + sources[0].ItemSpec + ":\r\n" + ex);
            }
            
            Assert.IsTrue(result, "CSC failed to compile " + sources[0].ItemSpec + ":\r\n" + buildEngine.ConsoleLogger.Errors);
            return result;
        }

        /// <summary>
        /// Invokes VBC to build the given files against the given set of references
        /// </summary>
        /// <param name="files"></param>
        /// <param name="referenceAssemblies"></param>
        /// <param name="documentationFile">If nonblank, the documentation file to generate during the compile.</param>
        public static bool CompileVisualBasicSource(IEnumerable<string> files, IEnumerable<string> referenceAssemblies, string rootNamespace, string documentationFile)
        {
            var sources = new List<ITaskItem>();
            foreach (string f in files)
                sources.Add(new TaskItem(f));

            // Transform references into a list of ITaskItems.
            // Here, we skip over mscorlib explicitly because this is already included as a project reference.
            List<ITaskItem> references =
                referenceAssemblies
                    .Where(reference => !reference.EndsWith("mscorlib.dll", StringComparison.Ordinal))
                    .Select(reference => new TaskItem(reference) as ITaskItem)
                    .ToList();

            var buildEngine = new MockBuildEngine();
            var vbc = new Vbc();
            vbc.BuildEngine = buildEngine;  // needed before task can log

            vbc.NoStandardLib = true;   // don't include std lib stuff -- we're feeding it pcl
            vbc.NoConfig = true;        // don't load the vbc.rsp file to get references
            vbc.TargetType = "library";
            vbc.Sources = sources.ToArray();
            vbc.References = references.ToArray();
            //vbc.SdkPath = GetSdkReferenceAssembliesPath();

            if (!string.IsNullOrEmpty(rootNamespace))
            {
                vbc.RootNamespace = rootNamespace;
            }

            if (!string.IsNullOrEmpty(documentationFile))
            {
                vbc.DocumentationFile = documentationFile;
            }

            bool result = false;
            try
            {
                result = vbc.Execute();
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception occurred invoking VBC task on " + sources[0].ItemSpec + ":\r\n" + ex);
            }

            Assert.IsTrue(result, "VBC failed to compile " + sources[0].ItemSpec + ":\r\n" + buildEngine.ConsoleLogger.Errors);
            return result;
        }

        /// <summary>
        /// Extract the list of assemblies both generated and referenced by client.
        /// Not coincidently, this list is what a client project needs to reference.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetClientAssemblies(string relativeTestDir)
        {
            List<string> assemblies = new List<string>();

            string projectPath, outputPath;   // output path for current project, used to infer output path of test project
            TestHelper.GetProjectPaths(relativeTestDir, out projectPath, out outputPath);

            // Our current project's folder
            string projectDir = Path.GetDirectoryName(projectPath);

            // Folder of project we want to build
            string testProjectDir = Path.GetFullPath(Path.Combine(projectDir, @"..\..\Luma.SimpleEntity.Client"));

            string testProjectFile = Path.Combine(testProjectDir, @"Luma.SimpleEntity.Client.csproj");
            Assert.IsTrue(File.Exists(testProjectFile), "This test could not find its required project at " + testProjectFile);

            // Retrieve all the assembly references from the test project (follows project-to-project references too)
            MsBuildHelper.GetReferenceAssemblies(testProjectFile, assemblies);

            var outputAssembly = MsBuildHelper.GetOutputAssembly(testProjectFile);
            if (!string.IsNullOrEmpty(outputAssembly))
            {
                assemblies.Add(outputAssembly);
            }

            var frameworkDirectory = CodeGenHelper.GetRuntimeDirectory();
            var frameworkAssemblies = Directory.EnumerateFiles(frameworkDirectory, "*.dll");
            assemblies.AddRange(frameworkAssemblies);

            return assemblies;
        }
    }
}
