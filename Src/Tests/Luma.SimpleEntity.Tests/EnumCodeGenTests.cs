using System;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace Luma.SimpleEntity.Tests
{
    using EnumGen.Tests;
    using EnumGen.Tests2;

    /// <summary>
    /// Tests CustomAttributeGenerator
    /// </summary>
    [TestClass]
    public class EnumCodeGenTests
    {
        [TestMethod]
        [Description("Entity property returning enum type generates enum in C#")]
        [DeploymentItem(@"Luma.SimpleEntity.Tests\ProjectPath.txt", "CG_ENUM_CS")]
        public void Enum_Gen_Basic_CSharp()
        {
            using(var asmGen = new AssemblyGenerator("CG_ENUM_CS", /*isCSharp*/ true, /*useFullTypeNames*/ false, new[] {typeof (Enum_Basic_Entity)}))
            {
                VerifyEnumGenBasic(asmGen);
            }
        }

        [TestMethod]
        [Description("Entity property returning enum type generates enum in C# with full type names")]
        [DeploymentItem(@"Luma.SimpleEntity.Tests\ProjectPath.txt", "CG_ENUM_CS_FULL")]
        public void Enum_Gen_Basic_CSharp_Full()
        {
            using (var asmGen = new AssemblyGenerator("CG_ENUM_CS_FULL", /*isCSharp*/ true, /*useFullTypeNames*/ true, new Type[] { typeof(Enum_Basic_Entity) }))
            {
                VerifyEnumGenBasic(asmGen);
            }
        }

        //[TestMethod]
        //[Description("Entity property returning enum type generates enum in VB")]
        //[DeploymentItem(@"Luma.SimpleEntity.Tests\ProjectPath.txt", "CG_ENUM_VB")]
        //public void Enum_Gen_Basic_VB()
        //{
        //    using (var asmGen = new AssemblyGenerator("CG_ENUM_VB", /*isCSharp*/ false, /*useFullTypeNames*/ false, new[] { typeof(Enum_Basic_Entity) }))
        //    {
        //        VerifyEnumGenBasic(asmGen);
        //    }
        //}

        //[TestMethod]
        //[Description("Entity property returning enum type generates enum in VB with full type names")]
        //[DeploymentItem(@"Luma.SimpleEntity.Tests\ProjectPath.txt", "CG_ENUM_VB_FULL")]
        //public void Enum_Gen_Basic_VB_Full()
        //{
        //    using (var asmGen = new AssemblyGenerator("CG_ENUM_VB_FULL", /*isCSharp*/ false, /*useFullTypeNames*/ true, new[] { typeof(Enum_Basic_Entity) }))
        //    {
        //        VerifyEnumGenBasic(asmGen);
        //    }
        //}

        [TestMethod]
        [Description("If an entity has DataContract applied only enum properties with DataMember are generated")]
        [DeploymentItem(@"Luma.SimpleEntity.Tests\ProjectPath.txt", "CG_ENUM")]
        public void Enum_Gen_DataContract()
        {
            using (var asmGen = new AssemblyGenerator("CG_ENUM", true, false, new[] { typeof(Enum_DataContract_Entity) }))
            {
                Type clientEntityType = asmGen.GetGeneratedType(typeof(Enum_DataContract_Entity).FullName);

                PropertyInfo genThis = clientEntityType.GetProperty("GenThis");
                Assert.IsNotNull(genThis);

                PropertyInfo dontGen = clientEntityType.GetProperty("DontGen");
                Assert.IsNull(dontGen);
            }
        }

        private void VerifyEnumGenBasic(AssemblyGenerator asmGen)
        {
            // Force this type to be shared to force failure
            asmGen.MockSharedCodeService.AddSharedType(typeof(System.IO.FileAttributes));

            string generatedCode = asmGen.GeneratedCode;
            Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "Failed to generate code:\r\n" + asmGen.ConsoleLogger.Errors);

            Assembly assy = asmGen.GeneratedAssembly;
            Assert.IsNotNull(assy, "Assembly failed to build: " + asmGen.ConsoleLogger.Errors);

            TestHelper.AssertNoErrorsOrWarnings(asmGen.ConsoleLogger);

            // ------------------------------------------------------
            // Check the properties using enums were handled properly
            // ------------------------------------------------------
            Type clientEntityType = asmGen.GetGeneratedType(typeof(Enum_Basic_Entity).FullName);
            Assert.IsNotNull(clientEntityType, "Expected entity of type " + typeof(Enum_Basic_Entity));

            // Validate normal enum is generated
            this.ValidateGeneratedEnumProperty(typeof(Enum_Basic_Entity), "SizeProperty", typeof(SizeEnum), asmGen, /* expectNullable */ false);

            // Validate 2nd appearance of same enum is generated and does not gen 2nd decl of entity type (would fail compile)
            this.ValidateGeneratedEnumProperty(typeof(Enum_Basic_Entity), "SizeProperty2", typeof(SizeEnum), asmGen, /* expectNullable */ false);

            // Validate nullable form of same enum is generated and does not gen 2nd decl of entity type (would fail compile)
            this.ValidateGeneratedEnumProperty(typeof(Enum_Basic_Entity), "NullableSizeProperty", typeof(SizeEnum), asmGen, /* expectNullable */ true);

            // Validate nullable form of an enum *that is the only use of that enum type* generates the enum type
            // Regression test for 819356.
            this.ValidateGeneratedEnumProperty(typeof(Enum_Basic_Entity), "NullableOnlySizeProperty", typeof(SizeEnumNullableOnly), asmGen, /* expectNullable */ true);

            // Validate all integral forms of enum
            this.ValidateGeneratedEnumProperty(typeof(Enum_Basic_Entity), "SByteEnumProp", typeof(SByteEnum), asmGen, /* expectNullable */ false);
            this.ValidateGeneratedEnumProperty(typeof(Enum_Basic_Entity), "ByteEnumProp", typeof(ByteEnum), asmGen, /* expectNullable */ false);
            this.ValidateGeneratedEnumProperty(typeof(Enum_Basic_Entity), "ShortEnumProp", typeof(ShortEnum), asmGen, /* expectNullable */ false);
            this.ValidateGeneratedEnumProperty(typeof(Enum_Basic_Entity), "UShortEnumProp", typeof(UShortEnum), asmGen, /* expectNullable */ false);
            this.ValidateGeneratedEnumProperty(typeof(Enum_Basic_Entity), "IntEnumProp", typeof(IntEnum), asmGen, /* expectNullable */ false);
            this.ValidateGeneratedEnumProperty(typeof(Enum_Basic_Entity), "UIntEnumProp", typeof(UIntEnum), asmGen, /* expectNullable */ false);
            this.ValidateGeneratedEnumProperty(typeof(Enum_Basic_Entity), "LongEnumProp", typeof(LongEnum), asmGen, /* expectNullable */ false);
            this.ValidateGeneratedEnumProperty(typeof(Enum_Basic_Entity), "ULongEnumProp", typeof(ULongEnum), asmGen, /* expectNullable */ false);

            // Validate an enum with custom attributes on fields generates properly
            this.ValidateGeneratedEnumProperty(typeof(Enum_Basic_Entity), "CustomAttributeEnumProp", typeof(EnumWithCustomAttributes), asmGen, /* expectNullable */ false);

            // Validate an enum from another namespace propagates
            this.ValidateGeneratedEnumProperty(typeof(Enum_Basic_Entity), "SizePropertyOther", typeof(SizeEnumOther), asmGen, /* expectNullable */ false);

            // Validate a [Flags] enum propagates [Flags]
            this.ValidateGeneratedEnumProperty(typeof(Enum_Basic_Entity), "FlagProperty", typeof(FlagEnum), asmGen, /* expectNullable */ false);

            // DataContract enum was propagated
            this.ValidateGeneratedEnumProperty(typeof(Enum_Basic_Entity), "DCEnumProp", typeof(DataContractEnum), asmGen, /* expectNullable */ false);

            // Non-public property should not force enum to gen
            PropertyInfo propertyInfo = clientEntityType.GetProperty("SizePropertyNoGen");
            Assert.IsNull(propertyInfo, "The property SizePropertyNoGen should not have been generated because the property was not public.");

            Type clientEnumType = asmGen.GetGeneratedType(typeof(SizeEnumNoGen).FullName);
            Assert.IsNull(clientEnumType, "The SizeEnumNoGen type should not have been generated because only a private property exposed it.");

            // Non-public enum should not gen
            propertyInfo = clientEntityType.GetProperty("PrivateEnumProperty");
            Assert.IsNull(propertyInfo, "The property PrivateEnumProperty should not have been generated because the enum was not public.");

            clientEnumType = asmGen.GetGeneratedType(typeof(PrivateEnum).FullName);
            Assert.IsNull(clientEnumType, "The PrivateEnum type should not have been generated because it was not public.");

            // Shared System enum was used but not propagated
            propertyInfo = clientEntityType.GetProperty("DateTimeKindProp");
            Assert.IsNotNull(propertyInfo, "The property DateTimeKindProp should have been generated because the enum was shared.");
            Assert.AreEqual(typeof(DateTimeKind).FullName, propertyInfo.PropertyType.FullName, "Expected DateTimeKindProp to use FileAttributes");
        }

        [TestMethod]
        [Description("Entity exposing property with enum in System namespace emits warning if it is not shared")]
        [DeploymentItem(@"Luma.SimpleEntity.Tests\ProjectPath.txt", "CG_ENUM")]
        public void Enum_Gen_Warn_System_Property()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    "CG_ENUM",
                                                    isCSharp,
                                                    new Type[] { typeof(Enum_System_Prop_Entity) }))
                {
                    // Force this type to be unshared to force failure
                    asmGen.MockSharedCodeService.AddUnsharedType(typeof(System.IO.FileAttributes));

                    string generatedCode = asmGen.GeneratedCode;
                    Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "Failed to generate code:\r\n" + asmGen.ConsoleLogger.Errors);

                    string message = String.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Property_Enum_Error, "EnumGen.Tests.Enum_System_Prop_Entity", "FileAttrProp", "System.IO.FileAttributes", Resource.Enum_Type_Cannot_Gen_System);
                    TestHelper.AssertContainsWarnings(asmGen.ConsoleLogger, message);
                }
            }
        }

        [TestMethod]
        [Description("Entity exposing property with nested enum type is illegal")]
        [DeploymentItem(@"Luma.SimpleEntity.Tests\ProjectPath.txt", "CG_ENUM")]
        public void Enum_Gen_Warn_Nested_Property()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    "CG_ENUM",
                                                    isCSharp,
                                                    new Type[] { typeof(Enum_Nested_Prop_Entity) }))
                {
                    string generatedCode = asmGen.GeneratedCode;
                    Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "Failed to generate code:\r\n" + asmGen.ConsoleLogger.Errors);

                    string message = String.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Property_Enum_Error, "EnumGen.Tests.Enum_Nested_Prop_Entity", "NestedEnumProp", "EnumGen.Tests.Enum_Nested_Prop_Entity+NestedEnum", Resource.Enum_Type_Must_Be_Public);
                    TestHelper.AssertContainsWarnings(asmGen.ConsoleLogger, message);
                }
            }
        }

        private void ValidateGeneratedEnumProperty(Type serverEntityType, string propertyName, Type serverEnumType, AssemblyGenerator asmGen, bool expectedNullable)
        {
            // Validate we generated the enum type
            Type clientEnumType = this.ValidateGeneratedEnum(serverEnumType, asmGen);

            Type clientEntityType = asmGen.GetGeneratedType(serverEntityType.FullName);
            Assert.IsNotNull(clientEntityType, "Expected client entity of type " + serverEntityType);

            PropertyInfo propertyInfo = clientEntityType.GetProperty(propertyName);
            Assert.IsNotNull(propertyInfo, "Expected property " + propertyName + " on entity type " + clientEntityType);

            this.ValidateGeneratedEnumReference(clientEnumType, propertyInfo.PropertyType, expectedNullable);
        }

        private void ValidateGeneratedEnumReference(Type clientEnumType, Type referenceType, bool expectedNullable)
        {
            if (expectedNullable)
            {
                Assert.IsTrue(AssemblyGenerator.IsNullableType(referenceType), "Expected a nullable but saw " + referenceType);
                Type nonNullableType =  AssemblyGenerator.GetNonNullableType(referenceType);
                Assert.AreEqual(clientEnumType, nonNullableType, "Expected a nullable of " + clientEnumType + " but saw " + nonNullableType);
            }
            else
            {
                Assert.AreEqual(clientEnumType, referenceType, "Expected type reference to be " + clientEnumType + " but was " + referenceType);
            }
        }

        private Type ValidateGeneratedEnum(Type serverEnumType, AssemblyGenerator asmGen)
        {
            Type clientEnumType = asmGen.GetGeneratedType(serverEnumType.FullName);
            Assert.IsNotNull(clientEnumType, "Expected to see generated " + serverEnumType + " but saw " + asmGen.GeneratedTypeNames);

            // validate the enum integral type is the same
            Type serverUnderlyingType = serverEnumType.GetEnumUnderlyingType();
            Type clientUnderlyingType = clientEnumType.GetEnumUnderlyingType();
            Assert.AreEqual(serverUnderlyingType.FullName, clientUnderlyingType.FullName, "Mismatch in server enum type's underlying type and generated form");

            DataContractAttribute serverDCAttr = (DataContractAttribute) Attribute.GetCustomAttribute(serverEnumType, typeof(DataContractAttribute));
            if (serverDCAttr != null)
            {
                IList<CustomAttributeData> cads = AssemblyGenerator.GetCustomAttributeData(clientEnumType, typeof(DataContractAttribute));
                Assert.AreEqual(1, cads.Count, "Expected DataContract on " + clientEnumType);
                CustomAttributeData cad = cads[0];
                string serverAttrName = serverDCAttr.Name;
                string serverAttrNamespace = serverDCAttr.Namespace;
                string clientAttrName = AssemblyGenerator.GetCustomAttributeValue<string>(cad, "Name");
                string clientAttrNamespace = AssemblyGenerator.GetCustomAttributeValue<string>(cad, "Namespace");

                Assert.AreEqual(serverAttrName, clientAttrName, "Expected DC.Name to be the same on " + clientEnumType);
                Assert.AreEqual(serverAttrNamespace, clientAttrNamespace, "Expected DC.Namespace to be the same on " + clientEnumType);
            }

            string[] serverMemberNames = Enum.GetNames(serverEnumType);
            string[] clientMemberNames = Enum.GetNames(clientEnumType);
            Assert.AreEqual(serverMemberNames.Length, clientMemberNames.Length, "Different number of fields generated");

            for (int i = 0; i < serverMemberNames.Length; ++i)
            {
                Assert.AreEqual(serverMemberNames[i], clientMemberNames[i], "Member name difference");

                // We have to use GetRawConstantValue because the ReflectionOnlyLoad does not support Enum.GetValues
                FieldInfo serverFieldInfo = serverEnumType.GetField(serverMemberNames[i]);
                Assert.IsNotNull(serverFieldInfo, "Failed to find server's " + serverMemberNames[i] + " as field info");
                object serverMemberValue = serverFieldInfo.GetRawConstantValue(); 

                FieldInfo clientFieldInfo = clientEnumType.GetField(clientMemberNames[i]);
                Assert.IsNotNull(clientFieldInfo, "Failed to find client's " + clientMemberNames[i] + " as field info");
                object clientMemberValue = clientFieldInfo.GetRawConstantValue(); 

                Assert.AreEqual(serverMemberValue, clientMemberValue, "Different values for field " + serverMemberNames[i]);

                EnumMemberAttribute enumMemberAttr = (EnumMemberAttribute)Attribute.GetCustomAttribute(serverFieldInfo, typeof(EnumMemberAttribute));
                if (enumMemberAttr != null)
                {
                    IList<CustomAttributeData> cads = AssemblyGenerator.GetCustomAttributeData(clientFieldInfo, typeof(EnumMemberAttribute));
                    Assert.AreEqual(1, cads.Count, "Expected EnumMember on " + clientEnumType + "." + clientMemberNames[i]);
                    CustomAttributeData cad = cads[0];
                    string clientValue = null;
                    AssemblyGenerator.TryGetCustomAttributeValue<string>(cad, "Value", out clientValue);

                    string serverValue = enumMemberAttr.Value;
                    Assert.AreEqual(serverValue, clientValue, "EnumMember had different values for Value arg for " + clientEnumType + "." + clientMemberNames[i]);
                }

                // Validate Display custom attribute propagates correctly
                DisplayAttribute displayAttr = (DisplayAttribute)Attribute.GetCustomAttribute(serverFieldInfo, typeof(DisplayAttribute));
                if (displayAttr != null)
                {
                    IList<CustomAttributeData> cads = AssemblyGenerator.GetCustomAttributeData(clientFieldInfo, typeof(DisplayAttribute));
                    Assert.AreEqual(1, cads.Count, "Expected [Display] on " + clientEnumType + "." + clientMemberNames[i]);
                    CustomAttributeData cad = cads[0];
                    string clientValue = null;
                    AssemblyGenerator.TryGetCustomAttributeValue<string>(cad, "Name", out clientValue);

                    string serverValue = displayAttr.Name;
                    Assert.AreEqual(serverValue, clientValue, "[Display] had different values for Name arg for " + clientEnumType + "." + clientMemberNames[i]);
                }

                // Validate ServerOnlyAttribute does not propagate
                ServerOnlyAttribute serverOnlyAttr = (ServerOnlyAttribute)Attribute.GetCustomAttribute(serverFieldInfo, typeof(ServerOnlyAttribute));
                if (serverOnlyAttr != null)
                {
                    IList<CustomAttributeData> cads = AssemblyGenerator.GetCustomAttributeData(clientFieldInfo, typeof(ServerOnlyAttribute));
                    Assert.AreEqual(0, cads.Count, "Expected [ServerOnlyAttribute] *not* to be generated on " + clientEnumType + "." + clientMemberNames[i]);
                }
            }

            bool serverHasFlags = serverEnumType.GetCustomAttributes(false).OfType<FlagsAttribute>().Any();

            // Have to use CustomAttributeData due to ReflectionOnly load
            IList<CustomAttributeData> clientFlagsAttributes = AssemblyGenerator.GetCustomAttributeData(clientEnumType, typeof(FlagsAttribute));
            bool clientHasFlags = clientFlagsAttributes.Any();
            Assert.AreEqual(serverHasFlags, clientHasFlags, "Server and client differ in appearance of [Flags]");

            return clientEnumType;
        }

    }
}

// Avoid the System namespace or VB treats System as its root namespace
namespace EnumGen.Tests
{
    using System.Runtime.Serialization;
    using EnumGen.Tests2;

    #region Enum_Basic

    // ----------------------------------------------------------------
    // Enum_Basic
    //    exposes one property that uses enum
    public enum SizeEnum
    {
        Small,
        Medium,
        Large
    }

    public enum SizeEnumNoGen
    {
        Small2,
        Large2
    }

    public enum SizeEnumNullableOnly
    {
        Small,
        Large
    }

    [Flags]
    public enum FlagEnum
    {
        None = 0x0,
        One = 0x1,
        Two = 0x2,
        Three = One | Two,
        Mask = 0xaaaa
    }

    public enum ByteEnum : byte
    {
        None = 0x0,
        MinValue = byte.MinValue,
        MaxValue = byte.MaxValue
    }

    public enum SByteEnum : sbyte
    {
        None = 0x0,
        MinValue = SByte.MinValue,
        MaxValue = SByte.MaxValue
    }

    public enum ShortEnum : short
    {
        None = 0x0,
        MinValue = Int16.MinValue,
        MaxValue = Int16.MaxValue
    }

    public enum UShortEnum : ushort
    {
        None = 0x0,
        MinValue = UInt16.MinValue,
        MaxValue = UInt16.MaxValue
    }

    public enum IntEnum : int
    {
        None = 0x0,
        MinValue = Int32.MinValue,
        MaxValue = Int32.MaxValue
    }

    public enum UIntEnum : uint
    {
        None = 0x0,
        MinValue = UInt32.MinValue,
        MaxValue = UInt32.MaxValue
    }

    public enum LongEnum : long
    {
        None = 0x0,
        MinValue = Int64.MinValue,
        MaxValue = Int64.MaxValue
    }

    public enum ULongEnum : ulong
    {
        None = 0x0,
        MinValue = UInt64.MinValue,
        MaxValue = UInt64.MaxValue
    }

    public enum EnumWithCustomAttributes
    {
        [Display(Name="Display_of_None")]
        [ServerOnlyAttribute]
        None = 0
    }

    public enum EnumWithThrowingCustomAttributes
    {
        [ThrowingAttribute]
        None = 0
    }

    [DataContract(Name="TheName", Namespace="TheNamespace")]
    public enum DataContractEnum
    {
        [EnumMember] Zero,
        [EnumMember(Value="1")] One
    }

    internal enum PrivateEnum
    {
        PrivateValue1,
        PrivateValue2
    }

    public enum InvokeEnumArg
    {
        InvokeEnumArg1,
        InvokeEnumArg2,
    }

    public enum InvokeEnumRet
    {
        InvokeEnumRet1,
        InvokeEnumRet2,
    }

    public enum NamedEnumArg
    {
        NamedEnumArg1,
        NamedEnumArg2,
    }

    public enum QueryEnumArg
    {
        QueryEnumArg1,
        QueryEnumArg2,
    }

    // Demonstrates custom attribute on enum field that will not be generated
    public class ServerOnlyAttribute : Attribute { };

    // Demonstrates custom attribute on enum field that throws during code gen
    public class ThrowingAttribute : Attribute
    {
        // for unit test
        internal static readonly Exception ThrowingAttributeException = new InvalidOperationException("ThrowingAttribute threw this");

        public string Name { get { throw ThrowingAttribute.ThrowingAttributeException; } set { } }
    }

    public partial class Enum_Basic_Entity
    {
        [Key] public string TheKey { get; set; }
        public SizeEnum SizeProperty { get; set; }                          // normal
        public SizeEnum SizeProperty2 { get; set; }                         // validate does not re-gen SizeEnum
        public SizeEnum? NullableSizeProperty { get; set; }                 // validate nullable handling
        public SizeEnumNullableOnly? NullableOnlySizeProperty { get; set; } // corner case: if nullable is only exposure, verify we gen the type
        private SizeEnumNoGen SizePropertyNoGen { get; set; }               // validate private prop does not cause generation of enum
        public FlagEnum FlagProperty { get; set; }                          // validate [Flags] works
        internal PrivateEnum PrivateEnumProperty { get; set; }              // validate private enum is not generated
        public SizeEnumOther SizePropertyOther { get; set; }                // different namespace
        public DateTimeKind DateTimeKindProp { get; set; }                    // shared type
        public DataContractEnum DCEnumProp { get; set; }

        // all integral permutations
        public ByteEnum ByteEnumProp { get; set; }
        public SByteEnum SByteEnumProp { get; set; }
        public ShortEnum ShortEnumProp { get; set; }
        public UShortEnum UShortEnumProp { get; set; }
        public IntEnum IntEnumProp { get; set; }
        public UIntEnum UIntEnumProp { get; set; }
        public LongEnum LongEnumProp { get; set; }
        public ULongEnum ULongEnumProp { get; set; }

        // enum with custom attributes
        public EnumWithCustomAttributes CustomAttributeEnumProp { get; set; }
    }
    #endregion // Enum_Basic

    #region Enum_Errors

    // Exposing System property that is not shared will cause a warning
    public partial class Enum_System_Prop_Entity
    {
        [Key]
        public string TheKey { get; set; }
        public FileAttributes FileAttrProp { get; set; }
    }

    // Nested enums are illegal
    public partial class Enum_Nested_Prop_Entity
    {
        [Key]
        public string TheKey { get; set; }
        public NestedEnum NestedEnumProp { get; set; }
        public enum NestedEnum
        {
            NestedValue1,
            NestedValue2
        }
    }

    #endregion // Enum_Errors

    #region Enum_DataContract
    [DataContract]
    public partial class Enum_DataContract_Entity
    {
        [Key]
        [DataMember]
        public int K { get; set; }

        [DataMember]
        public SizeEnum GenThis { get; set; }

        public SizeEnum DontGen { get; set; } // when DataContract is applied to the class, we only generate members marked with DataMember
    }

    #endregion // Enum_DataContract
}

// Validate we propagate enum from other namespace
namespace EnumGen.Tests2
{
    public enum SizeEnumOther
    {
        Small,
        Medium,
        Large
    }
}