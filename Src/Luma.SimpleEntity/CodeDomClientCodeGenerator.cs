﻿using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Luma.SimpleEntity.Generators;
using Luma.SimpleEntity.Helpers;
using Luma.SimpleEntity.Server;
using Microsoft.CSharp;
using Microsoft.VisualBasic;
using Luma.SimpleEntity.Tools;

namespace Luma.SimpleEntity
{
    /// <summary>
    /// Helper base class to generate the client proxy code using a combination of Reflection and CodeDom
    /// </summary>
    public abstract class CodeDomClientCodeGenerator : IClientCodeGenerator, ILogger
    {
        // This is the logical name of this code generator, which is used by the ClientCodeGenerationDispatcher
        // when choosing a code generator.  We use our own type name to minimize the chance of duplication
        // by a customer.  Do not change this value because customers will be told to use this string when they
        // want to name the default code generator.
        public const string GeneratorName = "Luma.SimpleEntity.CodeDomClientCodeGenerator";

        /// <summary>
        /// These imports will be added to all namespaces generated in the client proxy file
        /// </summary>
        private static readonly string[] FixedImports = 
        { 
            "System", 
            "System.Collections.Generic", 
            "System.Collections.ObjectModel",
            "System.ComponentModel",
            "System.ComponentModel.DataAnnotations",                       // [Key], [Validation] 
            "System.Linq",
            "System.Threading.Tasks"                                       // Task
        };

        private CodeCompileUnit _compileUnit;
        private CodeDomProvider _provider;
        private CodeGeneratorOptions _options;
        private Dictionary<string, CodeNamespace> _namespaces;
        private HashSet<Type> _enumTypesToGenerate;
        private ClientCodeGenerationOptions _clientProxyCodeGenerationOptions;
        private ICodeGenerationHost _host;
        private List<EntityDescription> _entityDescriptions;

        #region ClientCodeGenerator Members

        public string GenerateCode(ICodeGenerationHost host, IEnumerable<EntityDescription> entityDescriptions, ClientCodeGenerationOptions options)
        {
            try
            {
                // Initialize all instance state
                this.Initialize(host, entityDescriptions, options);

                // Generate the code
                return this.GenerateProxyClass();
            }
            finally
            {
                // Dispose and release all instance state
                this.Cleanup();
            }
        }

        internal void Initialize(ICodeGenerationHost host, IEnumerable<EntityDescription> descriptions, ClientCodeGenerationOptions options)
        {
            if (host == null)
            {
                throw new ArgumentNullException("host");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (descriptions == null)
            {
                throw new ArgumentNullException("descriptions");
            }

            // Initialize all the instance variables
            this._host = host;
            this._clientProxyCodeGenerationOptions = options;

            this._entityDescriptions = descriptions.ToList();
            this._compileUnit = new CodeCompileUnit();

            this._namespaces = new Dictionary<string, CodeNamespace>();
            this._enumTypesToGenerate = new HashSet<Type>();

            CodeDomClientCodeGenerator.ValidateOptions(this._clientProxyCodeGenerationOptions);

            // Unconditionally initialize some options
            CodeGeneratorOptions cgo = new CodeGeneratorOptions();
            cgo.IndentString = "    ";
            cgo.VerbatimOrder = false;
            cgo.BlankLinesBetweenMembers = true;
            cgo.BracingStyle = "C";
            this._options = cgo;

            // Choose the provider for the language.  C# is the default if unspecified.
            string language = this.ClientProxyCodeGenerationOptions.Language;
            bool isCSharp = String.IsNullOrEmpty(language) || String.Equals(language, "C#", StringComparison.OrdinalIgnoreCase);
            this._provider = isCSharp ? (CodeDomProvider)new CSharpCodeProvider() : (CodeDomProvider)new VBCodeProvider();

            // Configure our code gen utility package
            CodeGenUtilities.Initialize(!this.IsCSharp, this.ClientProxyCodeGenerationOptions.UseFullTypeNames, this.ClientProxyCodeGenerationOptions.ClientRootNamespace);
        }

        private void Cleanup()
        {
            // Dispose and release all instance variables
            CodeDomProvider provider = this._provider;
            this._provider = null;
            if (provider != null)
            {
                provider.Dispose();
            }
            this._compileUnit = null;
            this._namespaces = null;
            this._enumTypesToGenerate = null;
            this._entityDescriptions = null;
            this._host = null;
            this._options = null;
            this._clientProxyCodeGenerationOptions = null;
        }

        #endregion ClientCodeGenerator Members

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <remarks>
        /// This form of the constructor exists to allow construction by the <see cref="System.Web.Compilation.ClientBuildManager"/> to
        /// create an ASP.NET app domain.  Callers of this contructor must call <see cref="Initialize"/> after instantiation before using
        /// this instance.
        /// </remarks>
        public CodeDomClientCodeGenerator()
        {
        }

        internal ICodeGenerationHost CodeGenerationHost
        {
            get
            {
                return this._host;
            }
        }

        internal IEnumerable<EntityDescription> EntityDescriptions
        {
            get
            {
                return this._entityDescriptions.ToArray();
            }
        }

        /// <summary>
        /// Gets the options used to initialize this context.
        /// </summary>
        internal ClientCodeGenerationOptions ClientProxyCodeGenerationOptions
        {
            get
            {
                return this._clientProxyCodeGenerationOptions;
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not this instance is generating C# code.
        /// </summary>
        internal bool IsCSharp
        {
            get
            {
                return this.Provider is CSharpCodeProvider;
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not this instance is generating Visual basic code.
        /// </summary>
        internal bool IsVB
        {
            get
            {
                return this.Provider is VBCodeProvider;
            }
        }

        internal string ClientProjectName
        {
            get
            {
                return Path.GetFileNameWithoutExtension(this.ClientProxyCodeGenerationOptions.ClientProjectPath);
            }
        }
        private CodeCompileUnit CompileUnit
        {
            get
            {
                return this._compileUnit;
            }
        }

        private CodeDomProvider Provider
        {
            get
            {
                return this._provider;
            }
        }

        private string GenerateProxyClass()
        {
            var generatedCode = string.Empty;

            // Analyze the assemblies to extract all the EntityDescriptions
            var allDescriptions = _entityDescriptions;
            var generatedEntityTypes = new List<Type>();
            var generatedComplexTypes = new List<Type>();
            var typeMapping = new Dictionary<Type, CodeTypeDeclaration>();

            // Used to queue CodeProcessor invocations
            var codeProcessorQueue = new Queue<CodeProcessorWorkItem>();

            // Before we begin codegen, we want to register type names with our codegen
            // utilities so that we can avoid type name conflicts later.
            PreprocessProxyTypes();

            // Generate a new proxy class for each entity we found.
            // OrderBy type name of entity to give code-gen predictability
            foreach (var dsd in allDescriptions)
            {
                // If we detect the client already has the DomainContext we would have
                // generated, skip it. This condition arises when the client has references
                // to class libraries as well as a code-gen link to the server which has
                // references to the server-side equivalent libraries.  Without this check, we would
                // re-generate the same DomainContext that already lives in the class library.
                CodeMemberShareKind domainContextShareKind = GetEntityTypeMemberShareKind(dsd);
                if ((domainContextShareKind & CodeMemberShareKind.Shared) != 0)
                {
                    LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.Shared_DomainContext_Skipped));
                    continue;
                }

                // Log information level message to help users see progress and debug code-gen issues
                LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_Generating));

                // Generate all entities.
                GenerateDataContractTypes(dsd.EntityTypes, generatedEntityTypes, Resource.ClientCodeGen_EntityTypesCannotBeShared_Reference, t =>
                {
                    new EntityProxyGenerator(this, t, allDescriptions, typeMapping).Generate();
                });
            }

            // If there are no entityDescriptions, we do not generate any client proxies
            // We don't consider this an error, since this task might be invoked before the user has created any.
            if (allDescriptions.Count == 0)
            {
                return generatedCode;
            }

            // Generate any enum types we have decided we need to generate
            this.GenerateAllEnumTypes();

            // Fix up CodeDOM graph before invoking CodeProcessors
            this.FixUpCompileUnit(this.CompileUnit);

            // Invoke CodeProcessors after we've completed our CodeDOM graph
            while (codeProcessorQueue.Count > 0)
            {
                // Allow CodeProcessors to do post processing work
                CodeProcessorWorkItem workItem = codeProcessorQueue.Dequeue();
                this.InvokeCodeProcessor(workItem);
            }

            // Write the entire "file" to a single string to permit us to redirect it
            // to a file, a TextBuffer, etc
            if (!this.CodeGenerationHost.HasLoggedErrors)
            {
                using (TextWriter t = new StringWriter(CultureInfo.InvariantCulture))
                {
                    this.Provider.GenerateCodeFromCompileUnit(this.CompileUnit, t, this._options);
                    generatedCode = this.FixupVBOptionStatements(t.ToString());
                }
            }

            return generatedCode;
        }

        private CodeMemberShareKind GetEntityTypeMemberShareKind(EntityDescription dsd)
        {
            // TODO: Do it
            return 0;
        }

        /// <summary>
        /// Generates a new CodeNamespace or reuses an existing one of the given name.
        /// </summary>
        /// <param name="namespaceName">The namespace name. <c>null</c> is allowed.</param>
        /// <returns>namespace with the given name.</returns>
        internal CodeNamespace GetOrGenNamespace(string namespaceName)
        {
            this.EnsureInitialized();

            CodeNamespace ns = null;
            string namespaceKey = namespaceName;

            // Types in the global namespace will have a null namespace name
            if (namespaceKey == null)
            {
                namespaceKey = string.Empty;
            }

            if (!this._namespaces.TryGetValue(namespaceKey, out ns))
            {
                ns = new CodeNamespace(namespaceName);
                this._namespaces[namespaceKey] = ns;

                if (!this.ClientProxyCodeGenerationOptions.UseFullTypeNames)
                {
                    // Add all the fixed namespace imports
                    foreach (string fixedImport in FixedImports)
                    {
                        var import = new CodeNamespaceImport(fixedImport);
                        ns.Imports.Add(import);
                    }
                }

                this.CompileUnit.Namespaces.Add(ns);
            }
            return ns;
        }

        /// <summary>
        /// Gets the <see cref="CodeNamespace"/> for a <see cref="CodeTypeDeclaration"/>.
        /// </summary>
        /// <param name="typeDecl">A <see cref="CodeTypeDeclaration"/>.</param>
        /// <returns>A <see cref="CodeNamespace"/> or null.</returns>
        internal CodeNamespace GetNamespace(CodeTypeDeclaration typeDecl)
        {
            this.EnsureInitialized();
            string namespaceKey = typeDecl.UserData["Namespace"] as string;
            // Types in the global namespace will have a null namespace name
            if (namespaceKey == null)
            {
                namespaceKey = string.Empty;
            }
            CodeNamespace ns;
            this._namespaces.TryGetValue(namespaceKey, out ns);
            return ns;
        }

        /// <summary>
        /// Generates a new CodeNamespace or reuses an existing one of the given name correponding to the namespace
        /// of the given type.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to get or generate a <see cref="CodeNamespace"/> for.</param>
        /// <returns><see cref="CodeNamespace"/> with the given type's namespace.</returns>
        internal CodeNamespace GetOrGenNamespace(Type type)
        {
            this.EnsureInitialized();
            return this.GetOrGenNamespace(type.Namespace);
        }

        /// <summary>
        /// Registers that the given <paramref name="enumType"/>
        /// will be referenced in code generation.  This will cause that
        /// type to be registered for deferred generation if we determine
        /// that is required.
        /// </summary>
        /// <param name="enumType">The type of an enum we need to generate.</param>
        internal void RegisterUseOfEnumType(Type enumType)
        {
            // Quick out if already registered, else probe to see if client sees it
            if (!this._enumTypesToGenerate.Contains(enumType) && this.NeedToGenerateEnumType(enumType))
            {
                this._enumTypesToGenerate.Add(enumType);
            }
        }

        /// <summary>
        /// Generates all the enum type declarations we have determined
        /// are necessary in the client code.
        /// </summary>
        internal void GenerateAllEnumTypes()
        {
            foreach (Type enumType in this._enumTypesToGenerate)
            {
                CodeNamespace codeNamespace = this.GetOrGenNamespace(enumType.Namespace);
                CodeTypeDeclaration enumTypeDecl = CodeGenUtilities.CreateEnumTypeDeclaration(enumType, this);
                codeNamespace.Types.Add(enumTypeDecl);
            }
        }

        /// <summary>
        /// Determines whether the given enum type may be exposed to the client
        /// proxy classes.
        /// </summary>
        /// <param name="enumType">The enum type to test.</param>
        /// <param name="errorMessage">The error message that describes the problem (if <c>false</c> is returned).</param>
        /// <returns><c>true</c> if the proxy classes can legally refer to this enum type.</returns>
        internal bool CanExposeEnumType(Type enumType, out string errorMessage)
        {
            errorMessage = null;

            // We do not expose enum's unless they are public and not nested
            if (!enumType.IsPublic || enumType.IsNested)
            {
                errorMessage = Resource.Enum_Type_Must_Be_Public;
                return false;
            }

            // Determine whether it is visible to the client.  If so,
            // it is legal to expose without generating it.
            if ((this.GetTypeShareKind(enumType) & CodeMemberShareKind.Shared) != 0)
            {
                return true;
            }

            // The enum is not shared (i.e. not visible to the client).
            // We block attempts to generate anything from system assemblies
            if (enumType.Assembly.IsSystemAssembly())
            {
                errorMessage = Resource.Enum_Type_Cannot_Gen_System;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the given <paramref name="enumType"/>
        /// must be generated in client code in order to generate
        /// references to it.
        /// </summary>
        /// <remarks>
        /// This method checks whether the enum type is already visible
        /// to the client via shared source files or assembly references.
        /// </remarks>
        /// <param name="enumType">The enum type in question.</param>
        /// <returns><c>true</c> if it's necessary to generate the enum, 
        /// <c>false</c> if it was detected as being visible to the client project.</returns>
        internal bool NeedToGenerateEnumType(Type enumType)
        {
            Debug.Assert(enumType != null && enumType.IsEnum, "enumType must be non-null and an enum type");

            // If we made a positive decision about this before, immediate "yes"
            if (!this._enumTypesToGenerate.Contains(enumType))
            {
                // Ask whether client can see this enum type.
                // A positive response means we do not need to generate it.
                if ((this.GetTypeShareKind(enumType) & CodeMemberShareKind.Shared) != 0)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Generates a data contract type (entity or complex type).
        /// </summary>
        /// <param name="typesToGenerate">The enumerable of types to generate.</param>
        /// <param name="generatedTypes">The types already generated. This keeps track of
        /// types we've generated across domain service instances.</param>
        /// <param name="sharedError">The error to emit if the type is visible through a reference.</param>
        /// <param name="generateType">The method that code gens the type.</param>
        private void GenerateDataContractTypes(IEnumerable<Type> typesToGenerate, List<Type> generatedTypes, string sharedError, Action<Type> generateType)
        {
            foreach (Type t in typesToGenerate.OrderBy(e => e.Name))
            {
                // Type has already been generated, continue.
                if (generatedTypes.Contains(t))
                {
                    continue;
                }

                // More expensive check -- determine whether some type already
                // visible to the client has generated this same type. This condition arises
                // when entity living in different assembly become visible
                // to this code gen process. We disallow code gen when the type is already visible.
                // The user can acheive accessing multiple types through references to libraries.
                CodeMemberShareKind typeShareKind = GetTypeShareKind(t);

                if (typeShareKind == CodeMemberShareKind.SharedByReference)
                {
                    // Log error, but continue as to allow other errors/warnings to collect.
                    LogError(string.Format(CultureInfo.CurrentCulture, sharedError, t));
                    continue;
                }

                generatedTypes.Add(t);

                // Generate the entity type proxy
                generateType(t);
            }
        }

        /// <summary>
        /// Validates whether the given <see cref="ClientCodeGenerationOptions"/> options are correct.
        /// </summary>
        /// <param name="clientProxyCodeGenerationOptions">Options to validate</param>
        private static void ValidateOptions(ClientCodeGenerationOptions clientProxyCodeGenerationOptions)
        {
            // A null is not acceptable
            if (clientProxyCodeGenerationOptions == null)
            {
                throw new ArgumentNullException("clientProxyCodeGenerationOptions");
            }

            // The language property may not be null.
            if (String.IsNullOrEmpty(clientProxyCodeGenerationOptions.Language))
            {
                throw new ArgumentException(Resource.Null_Language_Property, "clientProxyCodeGenerationOptions");
            }
        }

        /// <summary>
        /// Validates the <see cref="Initialize"/> method was called before attempting to use this instance.
        /// </summary>
        /// <remarks>
        /// <see cref="Initialize"/> is called from the normal ctor, but it is bypassed when the parameterless
        /// ctor is used to instantiate this in another AppDomain using the parameterless ctor.
        /// </remarks>
        private void EnsureInitialized()
        {
            if (this._clientProxyCodeGenerationOptions == null)
            {
                throw new InvalidOperationException(Resource.ClientProxyGenerator_Initialize_Not_Called);
            }
        }

        /// <summary>
        /// Examines a <see cref="EntityDescription"/> to discover and invoke <see cref="CodeProcessor"/> types.
        /// </summary>
        /// <param name="workItem">The <see cref="CodeProcessorWorkItem"/> unit of work.</param>
        private void InvokeCodeProcessor(CodeProcessorWorkItem workItem)
        {
            Type codeProcessorType = workItem.CodeProcessorType;
            EntityDescription entityDescription = workItem.EntityDescription;
            IDictionary<Type, CodeTypeDeclaration> typeMapping = workItem.TypeMapping;

            // Verify the type defined is a valid CodeProcessor
            if (!typeof(CodeProcessor).IsAssignableFrom(codeProcessorType))
            {
                this.LogError(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resource.ClientCodeGen_CodeProcessor_NotValidType,
                        codeProcessorType));
                return;
            }

            // Verify the type has a default constructor that accepts a CodeDomProvider
            ConstructorInfo codeProcessorCtor =
                codeProcessorType.GetConstructor(
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(CodeDomProvider) },
                    null);

            if (codeProcessorCtor == null)
            {
                this.LogError(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resource.ClientCodeGen_CodeProcessor_InvalidConstructorSignature,
                        codeProcessorType,
                        typeof(CodeDomProvider)));
                return;
            }

            try
            {
                // Create a new CodeProcessor and invoke it
                var postProcessor = codeProcessorCtor.Invoke(new object[] { this._provider }) as CodeProcessor;
                postProcessor.ProcessGeneratedCode(entityDescription, this.CompileUnit, typeMapping);
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }

                // Unwrap TargetInvocationExceptions
                TargetInvocationException tie = ex as TargetInvocationException;
                if (tie != null && tie.InnerException != null)
                {
                    ex = tie.InnerException;
                }

                // Intercept the exception to log it
                this.LogError(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resource.ClientCodeGen_CodeProcessor_ExceptionCaught,
                        codeProcessorType,
                        ex.Message));

                // Since we can't handle the exception, let it continue.
                throw;
            }
        }

        private void FixUpCompileUnit(CodeCompileUnit compileUnit)
        {
            CodeDomVisitor visitor = new ClientProxyFixupCodeDomVisitor(this.ClientProxyCodeGenerationOptions);
            visitor.Visit(compileUnit);
        }

        /// <summary>
        /// This method adds explicity VB Option statements:
        /// Option Explicit On
        /// Option Strict On
        /// Option Infer On
        /// Option Compare Binary
        /// </summary>
        /// <remarks>
        /// The VBCodeProvider automatically adds the Option Explicit and Strict to the generaged code and it
        /// has an undocumented way to explicitly set these two options:
        /// 
        ///  ' Option Strict On
        ///  CompileUnit.UserData.Add("AllowLateBound", False)
        ///  ' Option Explicit On
        ///  CompileUnit.UserData.Add("RequireVariableDeclaration", True)
        ///  
        /// but it does not generate the Option Infer and Compare code and provides no way to do it.
        /// This method provides a work-around this limitation and for code clarity we are setting all
        /// the options in one place.
        /// 
        /// Setting all these options explicitly serves two purposes:
        /// - make sure the generated code is type-safe and 
        /// - reduce the test matrix particularly since the VB designer allows for setting these options at
        ///   project level.
        /// </remarks>
        /// <param name="code">the VB code</param>
        /// <returns>the fixed up VB code</returns>
        private string FixupVBOptionStatements(string code)
        {
            if (!this.IsCSharp && code != null)
            {
                StringBuilder strBuilder = new StringBuilder(code);

                // We need to change Option Strict from Off to On and add
                // Option Infer and Compare.  Option Explict is ok.
                string optionStrictOff = "Option Strict Off";
                string optionStrictOn = "Option Strict On";
                string optionInferOn = "Option Infer On";
                string optionCompareBinary = "Option Compare Binary";

                int idx = code.IndexOf(optionStrictOff, StringComparison.Ordinal);

                if (idx != -1)
                {
                    strBuilder.Replace(optionStrictOff, optionStrictOn, idx, optionStrictOff.Length);
                    strBuilder.Insert(idx, optionInferOn + Environment.NewLine);
                    strBuilder.Insert(idx, optionCompareBinary + Environment.NewLine);
                }
                return strBuilder.ToString();
            }
            return code;
        }

        /// <summary>
        /// Preprocesses Entity type names to enforce nesting restrictions and avoid conflicts later in codegen.
        /// </summary>
        private void PreprocessProxyTypes()
        {
            foreach (EntityDescription dsd in this.EntityDescriptions)
            {
                // Register all associated Entity type names
                foreach (Type entityType in dsd.EntityTypes)
                {
                    this.RegisterTypeName(entityType, entityType.Namespace);
                }
            }
        }

        /// <summary>
        /// Registers an individual type name with the underlying codegen infrastructure.
        /// </summary>
        /// <param name="type">The type to register.</param>
        /// <param name="containingNamespace">The containing namespace.</param>
        private void RegisterTypeName(Type type, string containingNamespace)
        {
            if (string.IsNullOrEmpty(type.Namespace))
            {
                this.LogError(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Namespace_Required, type));
                return;
            }

            // Check if we're in conflict
            if (!CodeGenUtilities.RegisterTypeName(type, containingNamespace))
            {
                // Aggressively check for potential conflicts across other entity types.
                IEnumerable<Type> potentialConflicts =
                    // Entity types with namespace matches
                    EntityDescriptions
                        .SelectMany(d => d.EntityTypes)
                            .Where(entity => entity.Namespace == type.Namespace).Distinct();

                foreach (var potentialConflict in potentialConflicts)
                {
                    // Register potential conflicts so we qualify type names correctly
                    // later during codegen.
                    CodeGenUtilities.RegisterTypeName(potentialConflict, containingNamespace);
                }
            }
        }


        internal CodeMemberShareKind GetTypeShareKind(Type type)
        {
            return this.CodeGenerationHost.GetTypeShareKind(type.AssemblyQualifiedName);
        }

        internal CodeMemberShareKind GetPropertyShareKind(Type type, string propertyName)
        {
            return this.CodeGenerationHost.GetPropertyShareKind(type.AssemblyQualifiedName, propertyName);
        }

        internal CodeMemberShareKind GetMethodShareKind(MethodBase methodBase)
        {
            IEnumerable<string> parameterTypeNames = methodBase.GetParameters().Select<ParameterInfo, string>(p => p.ParameterType.AssemblyQualifiedName);
            return this.CodeGenerationHost.GetMethodShareKind(methodBase.DeclaringType.AssemblyQualifiedName, methodBase.Name, parameterTypeNames);
        }

        #region ILogger Members
        public bool HasLoggedErrors
        {
            get { return this.CodeGenerationHost.HasLoggedErrors; }
        }

        public void LogError(string message)
        {
            this.CodeGenerationHost.LogError(message);
        }

        public void LogException(Exception ex)
        {
            this.CodeGenerationHost.LogException(ex);
        }

        public void LogWarning(string message)
        {
            this.CodeGenerationHost.LogWarning(message);
        }

        public void LogMessage(string message)
        {
            this.CodeGenerationHost.LogMessage(message);
        }
        #endregion ILogger Members
 
        #region Nested Types

        /// <summary>
        /// Used to queue up <see cref="CodeProcessor"/> work items.
        /// </summary>
        private class CodeProcessorWorkItem
        {
            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="codeProcessorType">The <see cref="CodeProcessor"/> <see cref="Type"/>.</param>
            /// <param name="entityDescription">The <see cref="EntityDescription"/> associated with the provided <paramref name="codeProcessorType"/>.</param>
            /// <param name="typeMapping">The type-mapping that will be provided to the <see cref="CodeProcessor"/>.</param>
            public CodeProcessorWorkItem(Type codeProcessorType, EntityDescription entityDescription, Dictionary<Type, CodeTypeDeclaration> typeMapping)
            {
                this.CodeProcessorType = codeProcessorType;
                this.EntityDescription = entityDescription;
                this.TypeMapping = typeMapping;
            }

            /// <summary>
            /// Gets the <see cref="CodeProcessor"/> <see cref="Type"/>.
            /// </summary>
            public Type CodeProcessorType
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets the <see cref="EntityDescription"/> associated with <see cref="CodeProcessorType"/>.
            /// </summary>
            public EntityDescription EntityDescription
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets the <see cref="CodeProcessor"/> type-mapping.
            /// </summary>
            public Dictionary<Type, CodeTypeDeclaration> TypeMapping
            {
                get;
                private set;
            }
        }

        #endregion Nested Types
    }
}
