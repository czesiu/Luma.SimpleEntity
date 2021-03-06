﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Luma.SimpleEntity.Tests.Utilities;
using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Luma.SimpleEntity.Tests
{
    internal class AssemblyGenerator : IDisposable
    {
        private readonly string _relativeTestDir;
        private readonly bool _isCSharp;
        private string _outputAssemblyName;
        private readonly IEnumerable<Type> _entityTypes;
        private EntityCatalog _entityCatalog;
        private MockBuildEngine _mockBuildEngine;
        private MockSharedCodeService _mockSharedCodeService;
        private string _generatedCode;
        private Assembly _generatedAssembly;
        private IList<string> _referenceAssemblies;
        private string _generatedCodeFile;
        private Type[] _generatedTypes;
        private string _userCode;
        private string _userCodeFile;
        private readonly bool _useFullTypeNames;

        public AssemblyGenerator(string relativeTestDir, bool isCSharp, IEnumerable<Type> entityTypes) :
            this(relativeTestDir, isCSharp, false, entityTypes)
        {
        }

        public AssemblyGenerator(string relativeTestDir, bool isCSharp, bool useFullTypeNames, IEnumerable<Type> entityTypes)
        {
            Assert.IsFalse(string.IsNullOrEmpty(relativeTestDir), "relativeTestDir required");

            this._relativeTestDir = relativeTestDir;
            this._isCSharp = isCSharp;
            this._useFullTypeNames = useFullTypeNames;
            this._entityTypes = entityTypes;
        }


        internal bool IsCSharp
        {
            get
            {
                return this._isCSharp;
            }
        }

        internal bool UseFullTypeNames
        {
            get
            {
                return this._useFullTypeNames;
            }
        }

        internal string OutputAssemblyName
        {
            get
            {
                if (this._outputAssemblyName == null)
                {
                    this._outputAssemblyName = Path.GetTempFileName() + ".dll";
                }
                return this._outputAssemblyName;
            }
        }

        internal string UserCode
        {
            get
            {
                return this._userCode;
            }
            private set
            {
                this._userCode = value;
            }
        }

        internal string UserCodeFile
        {
            get
            {
                if (this._userCodeFile == null)
                {
                    if (!string.IsNullOrEmpty(this.UserCode))
                    {
                        this._userCodeFile = Path.GetTempFileName();
                        File.WriteAllText(this._userCodeFile, this.UserCode);
                    }
                }
                return this._userCodeFile;
            }
        }


        internal MockBuildEngine MockBuildEngine
        {
            get
            {
                if (this._mockBuildEngine == null)
                {
                    this._mockBuildEngine = new MockBuildEngine();
                }
                return this._mockBuildEngine;
            }
        }

        internal MockSharedCodeService MockSharedCodeService
        {
            get
            {
                if (this._mockSharedCodeService == null)
                {
                    this._mockSharedCodeService = new MockSharedCodeService(new Type[0], new MethodBase[0], new string[0]);
                }
                return this._mockSharedCodeService;
            }
        }


        internal ConsoleLogger ConsoleLogger
        {
            get
            {
                return this.MockBuildEngine.ConsoleLogger;
            }
        }

        internal IList<string> ReferenceAssemblies
        {
            get
            {
                if (_referenceAssemblies == null)
                {
                    _referenceAssemblies = CompilerHelper.GetClientAssemblies(_relativeTestDir);
                }
                return _referenceAssemblies;
            }
        }

        internal string GeneratedCode
        {
            get
            {
                if (this._generatedCode == null)
                {
                    this._generatedCode = this.GenerateCode();
                }
                return this._generatedCode;
            }
        }

        internal EntityCatalog EntityCatalog
        {
            get
            {
                if (_entityCatalog == null)
                {
                    // slightly orthogonal, but they are set together
                    _generatedCode = GenerateCode();
                }
                return _entityCatalog;
            }
        }
        internal Assembly GeneratedAssembly
        {
            get
            {
                if (_generatedAssembly == null)
                {
                    _generatedAssembly = GenerateAssembly();
                }
                return _generatedAssembly;
            }
        }

        internal Type[] GeneratedTypes
        {
            get
            {
                if (_generatedTypes == null)
                {
                    _generatedTypes = GeneratedAssembly.GetExportedTypes();
                }
                return _generatedTypes;
            }
        }

        internal string GeneratedTypeNames
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (Type t in this.GeneratedTypes)
                {
                    sb.AppendLine("    " + t.FullName);
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Adds the specified source code into the next compilation
        /// request.  This allows a test to inject source code into
        /// the compile to test things like partial methods.
        /// </summary>
        /// <param name="userCode"></param>
        internal void AddUserCode(string userCode)
        {
            string s = this.UserCode ?? string.Empty;
            this.UserCode = s + userCode;
        }

        /// <summary>
        /// Converts the full type name of a server-side type to
        /// the name in the generated code.  This handles the invisible
        /// prepend of the VS namespace.
        /// </summary>
        /// <param name="fullTypeName"></param>
        /// <returns></returns>
        internal string GetGeneratedTypeName(string fullTypeName)
        {
            return this._isCSharp ? fullTypeName : "TestRootNS." + fullTypeName;
        }

        internal Type GetGeneratedType(string fullTypeName)
        {
            fullTypeName = GetGeneratedTypeName(fullTypeName);

            foreach (Type t in this.GeneratedTypes)
            {
                if (string.Equals(fullTypeName, t.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    return t;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns <c>true</c> if the given type is a <see cref="Nullable"/>
        /// </summary>
        /// <param name="type">The type to test</param>
        /// <returns><c>true</c> if the given type is a nullable type</returns>
        public static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// If the given type is <see cref="Nullable"/>, returns the element type,
        /// otherwise simply returns the input type
        /// </summary>
        /// <param name="type">The type to test that may or may not be Nullable</param>
        /// <returns>Either the input type or, if it was Nullable, its element type</returns>
        public static Type GetNonNullableType(Type type)
        {
            return IsNullableType(type) ? type.GetGenericArguments()[0] : type;
        }

        /// <summary>
        /// <summary>
        /// Returns the list of <see cref="CustomAttributeData"/> for the custom attributes
        /// attached to the given type of the given attribute type.
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <param name="attributeType"></param>
        /// <returns></returns>
        internal static IList<CustomAttributeData> GetCustomAttributeData(MemberInfo memberInfo, Type attributeType)
        {
            List<CustomAttributeData> result = new List<CustomAttributeData>(); ;

            IList<CustomAttributeData> attrs = CustomAttributeData.GetCustomAttributes(memberInfo);
            foreach (CustomAttributeData cad in attrs)
            {
                Type attrType = cad.Constructor.DeclaringType;
                if (string.Equals(attrType.FullName, attributeType.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(cad);
                }
            }
            return result;
        }

        /// <summary>
        /// Helper method to extract a named value from a <see cref="CustomAttributeData"/> instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="attribute"></param>
        /// <param name="valueName"></param>
        /// <returns></returns>
        internal static T GetCustomAttributeValue<T>(CustomAttributeData attribute, string valueName)
        {
            T value;
            if (TryGetCustomAttributeValue<T>(attribute, valueName, out value))
            {
                return value;
            }
            Assert.Fail("Failed to find a value named " + valueName + " in the CustomAttributeData " + attribute);
            return default(T);
        }

        /// <summary>
        /// Helper method to extract a named value from a <see cref="CustomAttributeData"/> instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="attribute"></param>
        /// <param name="valueName"></param>
        /// <param name="value">Output parameter to receive value</param>
        /// <returns><c>true</c> if the value was found</returns>
        internal static bool TryGetCustomAttributeValue<T>(CustomAttributeData attribute, string valueName, out T value)
        {
            value = default(T);
            ConstructorInfo ctor = attribute.Constructor;
            var ctorArgs = attribute.ConstructorArguments;
            if (ctor != null && ctorArgs != null && ctorArgs.Count > 0)
            {
                int ctorArgIndex = -1;
                ParameterInfo[] pInfos = ctor.GetParameters();
                for (int i = 0; i < pInfos.Length; ++i)
                {
                    if (string.Equals(valueName, pInfos[i].Name, StringComparison.OrdinalIgnoreCase))
                    {
                        ctorArgIndex = i;
                        break;
                    }
                }

                if (ctorArgIndex >= 0)
                {
                    var ctorArg = ctorArgs[ctorArgIndex];
                    if (typeof(T).IsAssignableFrom(ctorArg.ArgumentType))
                    {
                        value = (T)ctorArg.Value;
                        return true;
                    }
                }
            }

            foreach (var namedArg in attribute.NamedArguments)
            {
                if (string.Equals(valueName, namedArg.MemberInfo.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (typeof(T).IsAssignableFrom(namedArg.TypedValue.ArgumentType))
                    {
                        value = (T)namedArg.TypedValue.Value;
                        return true;
                    }
                }
            }

            return false;
        }

        private string GeneratedCodeFile
        {
            get
            {
                if (this._generatedCodeFile == null)
                {
                    this._generatedCodeFile = Path.GetTempFileName();
                    File.WriteAllText(this._generatedCodeFile, this.GeneratedCode);
                }
                return this._generatedCodeFile;
            }

        }

        private string GenerateCode()
        {
            var options = new ClientCodeGenerationOptions
            {
                Language = _isCSharp ? "C#" : "VisualBasic",
                ClientProjectPath = "MockProject.proj",
                ClientRootNamespace = "TestRootNS",
                UseFullTypeNames = _useFullTypeNames
            };

            var host = TestHelper.CreateMockCodeGenerationHost(ConsoleLogger, MockSharedCodeService);
            var generator = (_isCSharp) ? (CodeDomClientCodeGenerator)new CSharpCodeDomClientCodeGenerator() : (CodeDomClientCodeGenerator) new VisualBasicCodeDomClientCodeGenerator();
            _entityCatalog = new EntityCatalog(_entityTypes, ConsoleLogger);

            string generatedCode = generator.GenerateCode(host, _entityCatalog.EntityDescriptions, options);
            return generatedCode;
        }

        private Assembly GenerateAssembly()
        {
            // Failure to generate code results in no assembly
            if (string.IsNullOrEmpty(this.GeneratedCode))
            {
                return null;
            }

            string generatedAssemblyFileName = _isCSharp ? CompileCSharpSource() : CompileVisualBasicSource();
            if (string.IsNullOrEmpty(generatedAssemblyFileName))
            {
                Assert.Fail("Expected compile to succeed");
            }

            Assembly assy = null;
            var loadedAssemblies = new Dictionary<AssemblyName, Assembly>();

            try
            {
                foreach (var refAssyName in ReferenceAssemblies)
                {
                    if (refAssyName.Contains("mscorlib") || refAssyName.Contains("System.Runtime.dll"))
                    {
                        continue;
                    }

                    try
                    {
                        Assembly refAssy = Assembly.ReflectionOnlyLoadFrom(refAssyName);
                        loadedAssemblies[refAssy.GetName()] = refAssy;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(" failed to load " + refAssyName + ":\r\n" + ex.Message);
                    }
                }

                assy = Assembly.ReflectionOnlyLoadFrom(generatedAssemblyFileName);
                Assert.IsNotNull(assy);

                var refNames = assy.GetReferencedAssemblies();
                foreach (var refName in refNames)
                {
                    if (refName.FullName.Contains("mscorlib"))
                    {
                        continue;
                    }
                    if (!loadedAssemblies.ContainsKey(refName))
                    {
                        try
                        {
                            Assembly refAssy = Assembly.ReflectionOnlyLoad(refName.FullName);
                            loadedAssemblies[refName] = refAssy;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(" failed to load " + refName + ":\r\n" + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Assert.Fail("Encountered exception doing reflection only loads:\r\n" + ex.Message);
            }

            return assy;
        }

        internal string CompileCSharpSource()
        {
            List<ITaskItem> sources = new List<ITaskItem>();
            string codeFile = this.GeneratedCodeFile;
            sources.Add(new TaskItem(codeFile));

            // If client has added extra user code into the
            // compile request, add it in now
            string userCodeFile = this.UserCodeFile;
            if (!string.IsNullOrEmpty(userCodeFile))
            {
                sources.Add(new TaskItem(userCodeFile));
            }

            List<ITaskItem> references = new List<ITaskItem>();
            foreach (string s in ReferenceAssemblies)
                references.Add(new TaskItem(s));

            var buildEngine = MockBuildEngine;

            var csc = new Csc
            {
                BuildEngine = buildEngine,
                TargetType = "library",
                Sources = sources.ToArray(),
                References = references.ToArray(),
                OutputAssembly = new TaskItem(OutputAssemblyName)
            };

            csc.NoStandardLib = true;   // don't include std lib stuff -- we're feeding it pcl
            csc.NoConfig = true;        // don't load the csc.rsp file to get references

            var result = false;
            try
            {
                result = csc.Execute();
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception occurred invoking CSC task on " + sources[0].ItemSpec + ":\r\n" + ex);
            }

            Assert.IsTrue(result, "CSC failed to compile " + sources[0].ItemSpec + ":\r\n" + buildEngine.ConsoleLogger.Errors);
            return csc.OutputAssembly.ItemSpec;
        }

        internal string CompileVisualBasicSource()
        {
            var sources = new List<ITaskItem>();
            sources.Add(new TaskItem(GeneratedCodeFile));

            // If client has added extra user code into the
            // compile request, add it in now
            string userCodeFile = UserCodeFile;
            if (!string.IsNullOrEmpty(userCodeFile))
            {
                sources.Add(new TaskItem(userCodeFile));
            }

            // Transform references into a list of ITaskItems.
            // Here, we skip over mscorlib explicitly because this is already included as a project reference.
            var references = ReferenceAssemblies
                    .Where(reference => !reference.EndsWith("mscorlib.dll", StringComparison.Ordinal))
                    .Select(reference => new TaskItem(reference) as ITaskItem)
                    .ToList();

            var buildEngine = MockBuildEngine;
            var vbc = new Vbc
            {
                BuildEngine = buildEngine,
                NoStandardLib = true,
                NoConfig = true,
                TargetType = "library",
                Sources = sources.ToArray(),
                References = references.ToArray(),
                RootNamespace = "TestRootNS",
                OutputAssembly = new TaskItem(OutputAssemblyName)
            };

            //vbc.SdkPath = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\";

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
            return vbc.OutputAssembly.ItemSpec;
        }

        #region IDisposable Members

        public void Dispose()
        {
            this._generatedTypes = null;
            this._generatedAssembly = null;
            this.SafeDelete(this._generatedCodeFile);
            this.SafeDelete(this._userCodeFile);
            if (this._generatedAssembly != null)
            {
                this.SafeDelete(this._generatedAssembly.Location);
            }
        }

        #endregion

        private void SafeDelete(string file)
        {
            if (!string.IsNullOrEmpty(file) && File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                    System.Diagnostics.Debug.WriteLine("Deleted test file: " + file);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Could not delete " + file + ":\r\n" + ex.Message);
                }
            }
        }
    }
}
