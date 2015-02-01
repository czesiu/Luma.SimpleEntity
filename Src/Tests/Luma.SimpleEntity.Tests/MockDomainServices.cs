using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Linq;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using System.Xml.Linq;
using Luma.SimpleEntity.Helpers;
using TestNamespace.Saleテ;

[assembly: ContractNamespace("http://TestNamespace/ForNoClrNamespace")]

namespace TestNamespace
{
    #region Inheritance scenarios
    public class InheritanceBase
    {
        [Key]
        public int ID
        {
            get;
            set;
        }

        public int T1_ID
        {
            get;
            set;
        }

        [Association("InheritanceBase_InheritanceT1", "T1_ID", "ID", IsForeignKey = true)]
        public InheritanceT1 T1
        {
            get;
            set;
        }

        [Association("InheritanceT1_InheritanceBase", "ID", "InheritanceBase_ID")]
        public IEnumerable<InheritanceT1> T1s
        {
            get;
            set;
        }
    }

    public class InheritanceT1
    {
        [Key]
        public int ID
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        [Association("InheritanceBase_InheritanceT1", "ID", "T1_ID")]
        public InheritanceBase InheritanceBase
        {
            get;
            set;
        }

        public int InheritanceBase_ID
        {
            get;
            set;
        }

        [Association("InheritanceT1_InheritanceBase", "InheritanceBase_ID", "ID", IsForeignKey = true)]
        public InheritanceBase InheritanceBase2
        {
            get;
            set;
        }
    }

    // Base class for two derived children
    public class InheritanceA<TProp> : InheritanceBase
    {
        // Association in base class, which is inherited by all children
        [Association("InheritanceA_InheritanceD", "InheritanceD_ID", "ID", IsForeignKey = true)]
        public InheritanceD D
        {
            get;
            set;
        }

        public int InheritanceD_ID
        {
            get;
            set;
        }

        public TProp InheritanceAProp
        {
            get;
            set;
        }
    }

    public class InheritanceB : InheritanceA<string>
    {
        public string InheritanceBProp
        {
            get;
            set;
        }
    }

    public class InheritanceC : InheritanceA<string>
    {
        public string InheritanceCProp
        {
            get;
            set;
        }
    }

    public class InheritanceC2 : InheritanceC
    {
        public string InheritanceC2Prop
        {
            get;
            set;
        }
    }

    public class InheritanceD : InheritanceBase
    {
        [Association("InheritanceA_InheritanceD", "ID", "InheritanceD_ID")]
        public List<InheritanceA<string>> As
        {
            get;
            set;
        }

        public string InheritanceDProp
        {
            get;
            set;
        }
    }

    public class InheritanceE : InheritanceC
    {
        public string InheritanceEProp
        {
            get;
            set;
        }
    }

    public class InheritanceTestData
    {
        private List<InheritanceC> cs = new List<InheritanceC>();
        private List<InheritanceE> es = new List<InheritanceE>();

        public InheritanceTestData()
        {
            es.Add(new InheritanceE
            {
                ID = 1,
                InheritanceAProp = "AVal",
                InheritanceCProp = "CVal",
                InheritanceEProp = "EVal"
            });
        }

        public IEnumerable<InheritanceC> Cs
        {
            get
            {
                return cs;
            }
        }

        public IEnumerable<InheritanceE> Es
        {
            get
            {
                return es;
            }
        }
    }

    /// <summary>
    /// In this test scenario, DataMemberAttribute for Prop1 is applied via
    /// the buddy class.
    /// </summary>
    [MetadataType(typeof(TestEntity_DataMemberBuddy_Metadata))]
    public partial class TestEntity_DataMemberBuddy
    {
        public int ID { get; set; }

        public int Prop1 { get; set; }
    }

    public partial class TestEntity_DataMemberBuddy_Metadata
    {
        [Key]
        public static int ID;

        [DataMember(Name = "P1", IsRequired = true)]
        public static int Prop1;
    }

    /// <summary>
    /// This class is used in perf tests and shouldn't have any validation
    /// attributes added to it.
    /// </summary>
    public class POCONoValidation
    {
        [Key]
        public int ID { get; set; }

        public string A { get; set; }
        public string B { get; set; }
        public string C { get; set; }
        public string D { get; set; }
        public string E { get; set; }
    }

    // This enum is not shared and will be generated on the client
    public enum ImageKindEnum
    {
        ThumbNail,
        Full
    }

    public class MultipartKeyTestEntity1
    {
        // don't expect this as part of the generated GetIdentity
        // null check
        [Key]
        public int A { get; set; }

        // should be part of the check
        [Key]
        public int? C { get; set; }

        // should be part of the check
        [Key]
        public string B { get; set; }

        // shouldn't be part of the check
        [Key]
        public char D { get; set; }
    }

    public class MultipartKeyTestEntity2
    {
        // this should be part of the generated GetIdentity null check
        [Key]
        public string B { get; set; }

        // should not be part of the check
        [Key]
        public int A { get; set; }

    }

    public class MultipartKeyTestEntity3
    {
        // don't expect this as part of the generated GetIdentity
        // null check
        [Key]
        public int A { get; set; }

        // should be part of the check
        [Key]
        public int? C { get; set; }

        // should not be part of the check
        [Key]
        public char B { get; set; }
    }

    public partial class SpecialDataTypes
    {
        [Key]
        public int Id
        {
            get;
            set;
        }

        public IEnumerable<DateTime?> DateTimeProperty
        {
            get;
            set;
        }

        public List<bool?> BooleanProperty
        {
            get;
            set;
        }
    }

    public class TestSideEffects
    {
        [Key]
        public string Name
        {
            get;
            set;
        }

        public string Verb
        {
            get;
            set;
        }

        public Uri URL
        {
            get;
            set;
        }
    }

    public class TestCycles
    {
        [Key]
        public string Name
        {
            get;
            set;
        }

        public string ParentName
        {
            get;
            set;
        }

        // In this case neither the T nor the
        // Ts members are marked as Associations or Included. 
        // This test scenario verifies that we handle this properly
        public TestCycles T
        {
            get;
            set;
        }
        public List<TestCycles> Ts
        {
            get;
            set;
        }

        // Here, we are marking these cyclic properties with Include
        [Association("TestCycle_Parent", "ParentName", "Name", IsForeignKey = true)]
        public TestCycles IncludedT
        {
            get;
            set;
        }

        [Association("TestCycle_Parent", "Name", "ParentName")]
        public List<TestCycles> IncludedTs
        {
            get;
            set;
        }
    }

    public class RoundtripQueryEntity
    {
        [Key]
        public int ID { get; set; }

        public string PropB { get; set; }
        public string PropC { get; set; }
        public string Query { get; set; }
    }

    [MetadataType(typeof(EntityWithCyclicMetadataTypeAttributeB))]
    public class EntityWithCyclicMetadataTypeAttributeA
    {
        [Key]
        public int Key { get; set; }
    }

    [MetadataType(typeof(EntityWithCyclicMetadataTypeAttributeC))]
    public class EntityWithCyclicMetadataTypeAttributeB
    {
        [Key]
        public int Key { get; set; }
    }

    [MetadataType(typeof(EntityWithCyclicMetadataTypeAttributeA))]
    public class EntityWithCyclicMetadataTypeAttributeC
    {
        [Key]
        public int Key { get; set; }
    }

    [MetadataType(typeof(EntityWithSelfReferencingcMetadataTypeAttribute))]
    public class EntityWithSelfReferencingcMetadataTypeAttribute
    {
        [Key]
        public int Key { get; set; }
    }

    public class EntityWithDefaultDefaultValue
    {
        [Key]
        [DefaultValue(0)]
        public int ID { get; set; }

        [DefaultValue(false)]
        public bool BoolProp { get; set; }

        [DefaultValue(0)]
        public float FloatProp { get; set; }

        [DefaultValue('\0')]
        public char CharProp { get; set; }

        [DefaultValue(0)]
        public byte ByteProp { get; set; }
    }

    public class NullableFKParent
    {
        [Key]
        public int ID { get; set; }

        public string Data { get; set; }

        [Association("Parent_Child", "ID", "ParentID")]
        public IEnumerable<NullableFKChild> Children { get; set; }

        [Association("Parent_Child_Singleton", "ID", "ParentID_Singleton")]
        public NullableFKChild Child { get; set; }
    }

    public class NullableFKChild
    {
        [Key]
        public int ID { get; set; }

        public string Data { get; set; }

        // nullable FK
        public int? ParentID { get; set; }

        // nullable FK
        public int? ParentID_Singleton { get; set; }

        [Association("Parent_Child", "ParentID", "ID", IsForeignKey = true)]
        public NullableFKParent Parent { get; set; }

        [Association("Parent_Child_Singleton", "ParentID_Singleton", "ID", IsForeignKey = true)]
        public NullableFKParent Parent2 { get; set; }
    }

    #region Attribute Throwing Entity, Attributes, and Exceptions

    [ThrowingEntity]
    public class AttributeThrowingEntity
    {
        public const string ThrowingPropertyName = "ThrowingProperty";
        public const string ThrowingAssociationProperty = "ThrowingAssociation";
        public const string ThrowingAssociationCollectionProperty = "ThrowingAssociationCollection";

        public AttributeThrowingEntity() { }

        [Key]
        public string NonThrowingProperty { get; set; }

        [ThrowingEntityProperty]
        public string ThrowingProperty { get; set; }

        [ThrowingEntityAssociation]
        [Association("Association", "ThrowingProperty", "NonThrowingProperty", IsForeignKey = true)]
        public AttributeThrowingEntity ThrowingAssociation { get; set; }

        [ThrowingEntityAssociationCollection]
        [Association("AssociationCollection", "NonThrowingProperty", "ThrowingProperty")]
        public IEnumerable<AttributeThrowingEntity> ThrowingAssociationCollection { get; set; }
    }

    public class ThrowingServiceAttribute : Attribute
    {
        public const string ThrowingPropertyName = "ThrowingServiceAttributeProperty";
        public const string ExceptionMessage = "ThrowingServiceAttributeProperty throws a ThrowingServiceAttributeException";

        public ThrowingServiceAttribute() { }

        public string ThrowingServiceAttributeProperty
        {
            get { throw new ThrowingServiceAttributeException(ExceptionMessage); }
            set { }
        }
    }

    public class ThrowingServiceAttributeException : Exception
    {
        public ThrowingServiceAttributeException(string message) : base(message) { }
    }

    public class ThrowingEntityAttribute : Attribute
    {
        public const string ThrowingPropertyName = "ThrowingEntityAttributeProperty";
        public const string ExceptionMessage = "ThrowingEntityAttributeProperty throws a ThrowingEntityAttributeException";

        public ThrowingEntityAttribute() { }

        public string ThrowingEntityAttributeProperty
        {
            get { throw new ThrowingEntityAttributeException(ExceptionMessage); }
            set { }
        }
    }

    public class ThrowingEntityAttributeException : Exception
    {
        public ThrowingEntityAttributeException(string message) : base(message) { }
    }

    public class ThrowingEntityPropertyAttribute : Attribute
    {
        public const string ThrowingPropertyName = "ThrowingEntityPropertyAttributeProperty";
        public const string ExceptionMessage = "ThrowingEntityPropertyAttributeProperty throws a ThrowingEntityPropertyAttributeException";

        public ThrowingEntityPropertyAttribute() { }

        public string ThrowingEntityPropertyAttributeProperty
        {
            get { throw new ThrowingEntityPropertyAttributeException(ExceptionMessage); }
            set { }
        }
    }

    public class ThrowingEntityPropertyAttributeException : Exception
    {
        public ThrowingEntityPropertyAttributeException(string message) : base(message) { }
    }

    public class ThrowingEntityAssociationAttribute : Attribute
    {
        public const string ThrowingPropertyName = "ThrowingEntityAssociationAttributeProperty";
        public const string ExceptionMessage = "ThrowingEntityAssociationAttributeProperty throws a ThrowingEntityAssociationAttributeException";

        public ThrowingEntityAssociationAttribute() { }

        public string ThrowingEntityAssociationAttributeProperty
        {
            get { throw new ThrowingEntityAssociationAttributeException(ExceptionMessage); }
            set { }
        }
    }

    public class ThrowingEntityAssociationAttributeException : Exception
    {
        public ThrowingEntityAssociationAttributeException(string message) : base(message) { }
    }

    public class ThrowingEntityAssociationCollectionAttribute : Attribute
    {
        public const string ThrowingPropertyName = "ThrowingEntityAssociationCollectionAttributeProperty";
        public const string ExceptionMessage = "ThrowingEntityAssociationCollectionAttributeProperty throws a ThrowingEntityAssociationCollectionAttributeException";

        public ThrowingEntityAssociationCollectionAttribute() { }

        public string ThrowingEntityAssociationCollectionAttributeProperty
        {
            get { throw new ThrowingEntityAssociationCollectionAttributeException(ExceptionMessage); }
            set { }
        }
    }

    public class ThrowingEntityAssociationCollectionAttributeException : Exception
    {
        public ThrowingEntityAssociationCollectionAttributeException(string message) : base(message) { }
    }

    public class ThrowingQueryMethodAttribute : Attribute
    {
        public const string ThrowingPropertyName = "ThrowingQueryMethodProperty";
        public const string ExceptionMessage = "ThrowingQueryMethodProperty throws a ThrowingQueryMethodAttributeException";

        public ThrowingQueryMethodAttribute() { }

        public string ThrowingQueryMethodProperty
        {
            get { throw new ThrowingQueryMethodAttributeException(ExceptionMessage); }
            set { }
        }
    }

    public class ThrowingQueryMethodAttributeException : Exception
    {
        public ThrowingQueryMethodAttributeException(string message) : base(message) { }
    }

    public class ThrowingQueryMethodParameterAttribute : Attribute
    {
        public const string ThrowingPropertyName = "ThrowingQueryMethodParameterProperty";
        public const string ExceptionMessage = "ThrowingQueryMethodParameterProperty throws a ThrowingQueryMethodParameterAttributeException";

        public ThrowingQueryMethodParameterAttribute() { }

        public string ThrowingQueryMethodParameterProperty
        {
            get { throw new ThrowingQueryMethodParameterAttributeException(ExceptionMessage); }
            set { }
        }
    }

    public class ThrowingQueryMethodParameterAttributeException : Exception
    {
        public ThrowingQueryMethodParameterAttributeException(string message) : base(message) { }
    }

    public class ThrowingUpdateMethodAttribute : Attribute
    {
        public const string ThrowingPropertyName = "ThrowingUpdateMethodProperty";
        public const string ExceptionMessage = "ThrowingUpdateMethodProperty throws a ThrowingUpdateMethodAttributeException";

        public ThrowingUpdateMethodAttribute() { }

        public string ThrowingUpdateMethodProperty
        {
            get { throw new ThrowingUpdateMethodAttributeException(ExceptionMessage); }
            set { }
        }
    }

    public class ThrowingUpdateMethodAttributeException : Exception
    {
        public ThrowingUpdateMethodAttributeException(string message) : base(message) { }
    }

    public class ThrowingUpdateMethodParameterAttribute : Attribute
    {
        public const string ThrowingPropertyName = "ThrowingUpdateMethodParameterProperty";
        public const string ExceptionMessage = "ThrowingUpdateMethodParameterProperty throws a ThrowingUpdateMethodParameterAttributeException";

        public ThrowingUpdateMethodParameterAttribute() { }

        public string ThrowingUpdateMethodParameterProperty
        {
            get { throw new ThrowingUpdateMethodParameterAttributeException(ExceptionMessage); }
            set { }
        }
    }

    public class ThrowingUpdateMethodParameterAttributeException : Exception
    {
        public ThrowingUpdateMethodParameterAttributeException(string message) : base(message) { }
    }

    public class ThrowingInvokeMethodAttribute : Attribute
    {
        public const string ThrowingPropertyName = "ThrowingInvokeMethodProperty";
        public const string ExceptionMessage = "ThrowingInvokeMethodProperty throws a ThrowingInvokeMethodAttributeException";

        public ThrowingInvokeMethodAttribute() { }

        public string ThrowingInvokeMethodProperty
        {
            get { throw new ThrowingInvokeMethodAttributeException(ExceptionMessage); }
            set { }
        }
    }

    public class ThrowingInvokeMethodAttributeException : Exception
    {
        public ThrowingInvokeMethodAttributeException(string message) : base(message) { }
    }

    public class ThrowingInvokeMethodParameterAttribute : Attribute
    {
        public const string ThrowingPropertyName = "ThrowingInvokeMethodParameterProperty";
        public const string ExceptionMessage = "ThrowingInvokeMethodParameterProperty throws a ThrowingInvokeMethodParameterAttributeException";

        public ThrowingInvokeMethodParameterAttribute() { }

        public string ThrowingInvokeMethodParameterProperty
        {
            get { throw new ThrowingInvokeMethodParameterAttributeException(ExceptionMessage); }
            set { }
        }
    }

    public class ThrowingInvokeMethodParameterAttributeException : Exception
    {
        public ThrowingInvokeMethodParameterAttributeException(string message) : base(message) { }
    }

    #endregion Attribute Throwing Entity, Attributes, and Exceptions

    public partial class Entity_TestEditableAttribute
    {
        /// <summary>
        /// Generated as [Key, Editable(false, AllowInitialValue = true)]
        /// </summary>
        [Key]
        public int KeyField { get; set; }

        /// <summary>
        /// Generated as [Editable(true)]
        /// </summary>
        [Editable(true)]
        public string EditableTrue { get; set; }

        /// <summary>
        /// Generated as [Editable(false)]
        /// </summary>
        [Editable(false)]
        public string EditableFalse { get; set; }

        /// <summary>
        /// Generated as [Editable(true)]
        /// </summary>
        [Editable(true, AllowInitialValue = true)]
        public string EditableTrue_AllowInitialValueTrue { get; set; }

        /// <summary>
        /// Generated as [Editable(true, AllowInitialValue = false)]
        /// </summary>
        [Editable(true, AllowInitialValue = false)]
        public string EditableTrue_AllowInitialValueFalse { get; set; }

        /// <summary>
        /// Generated as [Editable(false, AllowInitialValue = true)]
        /// </summary>
        [Editable(false, AllowInitialValue = true)]
        public string EditableFalse_AllowInitialValueTrue { get; set; }

        /// <summary>
        /// Generated as [Editable(false)]
        /// </summary>
        [Editable(false, AllowInitialValue = false)]
        public string EditableFalse_AllowInitialValueFalse { get; set; }

        /// <summary>
        /// Generated as [Key, Editable(false)]
        /// </summary>
        [Key]
        [Editable(false)]
        public int Key_EditableFalse { get; set; }

        /// <summary>
        /// Generated as [Key, Editable(true)]
        /// </summary>
        [Key]
        [Editable(true)]
        public int Key_EditableTrue { get; set; }

        /// <summary>
        /// Generated as [Key, Editable(false, AllowInitialValue = true)]
        /// </summary>
        [Key]
        [Editable(false, AllowInitialValue = true)]
        public int Key_EditableFalse_AllowInitialValueTrue { get; set; }

        /// <summary>
        /// Generated as [Key, Editable(true, AllowInitialValue = false)]
        /// </summary>
        [Key]
        [Editable(true, AllowInitialValue = false)]
        public int Key_EditableTrue_AllowInitialValueFalse { get; set; }

        /// <summary>
        /// Generated as [Timestamp, Editable(false)]
        /// </summary>
        [Timestamp]
        [Editable(false)]
        public int Timestamp_EditableFalse { get; set; }

        /// <summary>
        /// Generated as [Timestamp, Editable(true)]
        /// </summary>
        [Timestamp]
        [Editable(true)]
        public int Timestamp_EditableTrue { get; set; }

        /// <summary>
        /// Generated as [Timestamp, Editable(false, AllowInitialValue = true)]
        /// </summary>
        [Timestamp]
        [Editable(false, AllowInitialValue = true)]
        public int Timestamp_EditableFalse_AllowInitialValueTrue { get; set; }

        /// <summary>
        /// Generated as [Timestamp, Editable(true, AllowInitialValue = false)]
        /// </summary>
        [Timestamp]
        [Editable(true, AllowInitialValue = false)]
        public int Timestamp_EditableTrue_AllowInitialValueFalse { get; set; }

        /// <summary>
        /// Generated as [ReadOnly(true), Editable(false)]
        /// </summary>
        [System.ComponentModel.ReadOnly(true)]
        [Editable(false)]
        public int ReadOnlyTrue_EditableFalse { get; set; }

        /// <summary>
        /// Generated as [ReadOnly(true), Editable(true)]
        /// </summary>
        [System.ComponentModel.ReadOnly(true)]
        [Editable(true)]
        public int ReadOnlyTrue_EditableTrue { get; set; }

        /// <summary>
        /// Generated as [ReadOnly(true), Editable(false, AllowInitialValue = true)]
        /// </summary>
        [System.ComponentModel.ReadOnly(true)]
        [Editable(false, AllowInitialValue = true)]
        public int ReadOnlyTrue_EditableFalse_AllowInitialValueTrue { get; set; }

        /// <summary>
        /// Generated as [ReadOnly(true), Editable(true, AllowInitialValue = false)]
        /// </summary>
        [System.ComponentModel.ReadOnly(true)]
        [Editable(true, AllowInitialValue = false)]
        public int ReadOnlyTrue_EditableTrue_AllowInitialValueFalse { get; set; }

        /// <summary>
        /// Generated as [ReadOnly(false), Editable(false)]
        /// </summary>
        [System.ComponentModel.ReadOnly(false)]
        [Editable(false)]
        public int ReadOnlyFalse_EditableFalse { get; set; }

        /// <summary>
        /// Generated as [ReadOnly(false), Editable(true)]
        /// </summary>
        [System.ComponentModel.ReadOnly(false)]
        [Editable(true)]
        public int ReadOnlyFalse_EditableTrue { get; set; }

        /// <summary>
        /// Generated as [ReadOnly(false), Editable(false, AllowInitialValue = true)]
        /// </summary>
        [System.ComponentModel.ReadOnly(false)]
        [Editable(false, AllowInitialValue = true)]
        public int ReadOnlyFalse_EditableFalse_AllowInitialValueTrue { get; set; }

        /// <summary>
        /// Generated as [ReadOnly(false), Editable(true, AllowInitialValue = false)]
        /// </summary>
        [System.ComponentModel.ReadOnly(false)]
        [Editable(true, AllowInitialValue = false)]
        public int ReadOnlyFalse_EditableTrue_AllowInitialValueFalse { get; set; }

        /// <summary>
        /// Generated as [Key, ReadOnly(true), Editable(false, AllowInitialValue = true)]
        /// </summary>
        [Key]
        [System.ComponentModel.ReadOnly(true)]
        public int Key_ReadOnlyTrue { get; set; }

        /// <summary>
        /// Generated as [Key, ReadOnly(false), Editable(false, AllowInitialValue = true)]
        /// </summary>
        [Key]
        [System.ComponentModel.ReadOnly(false)]
        public int Key_ReadOnlyFalse { get; set; }

        /// <summary>
        /// Generated as [Key, Timestamp, Editable(false, AllowInitialValue = true)]
        /// </summary>
        [Key]
        [Timestamp]
        public int Key_Timestamp { get; set; }

        /// <summary>
        /// Generated as [Key, Timestamp, ReadOnly(true), Editable(false, AllowInitialValue = true)]
        /// </summary>
        [Key]
        [Timestamp]
        [System.ComponentModel.ReadOnly(true)]
        public int Key_Timestamp_ReadOnlyTrue { get; set; }

        /// <summary>
        /// Generated as [Key, Timestamp, ReadOnly(true), Editable(false, AllowInitialValue = true)]
        /// </summary>
        [Key]
        [Timestamp]
        [System.ComponentModel.ReadOnly(false)]
        public int Key_Timestamp_ReadOnlyFalse { get; set; }

        /// <summary>
        /// Generated as [Timestamp, Editable(false)]
        /// </summary>
        [Timestamp]
        public int Timestamp { get; set; }

        /// <summary>
        /// Generated as [Timestamp, ReadOnly(true), Editable(false)]
        /// </summary>
        [Timestamp]
        [System.ComponentModel.ReadOnly(true)]
        public int Timestamp_ReadOnlyTrue { get; set; }

        /// <summary>
        /// Generated as [Timestamp, ReadOnly(false), Editable(false)]
        /// </summary>
        [Timestamp]
        [System.ComponentModel.ReadOnly(false)]
        public int Timestamp_ReadOnlyFalse { get; set; }

        /// <summary>
        /// Generated as [ReadOnly(true), Editable(false)]
        /// </summary>
        [System.ComponentModel.ReadOnly(true)]
        public int ReadOnlyTrue { get; set; }

        /// <summary>
        /// Generated as [ReadOnly(false), Editable(true)]
        /// </summary>
        [System.ComponentModel.ReadOnly(false)]
        public int ReadOnlyFalse { get; set; }
    }

    /// <summary>
    /// Test class used to verify codegen propagates ConcurrencyCheck,
    /// Timestamp and RoundtripAttributes correctly
    /// </summary>
    public class TimestampEntityA
    {
        [Key]
        public int ID { get; set; }

        /// <summary>
        /// We expect the client property to be marked with both
        /// Timestamp, ConcurrencyCheck, and RoundtripOriginal
        /// </summary>
        [Timestamp]
        [ConcurrencyCheck]
        public byte[] Version { get; set; }

        public string ValueA { get; set; }

        public string ValueB { get; set; }
    }

    public class TimestampEntityB
    {
        [Key]
        public int ID { get; set; }

        /// <summary>
        /// We expect the client property to be marked with both
        /// Timestamp, ConcurrencyCheck, and RoundtripOriginal
        /// </summary>
        [Timestamp]
        [ConcurrencyCheck]
        public byte[] Version { get; set; }

        /// <summary>
        /// Non concurrency member that is still roundtripped. Since
        /// this is present, we still expect an original entity instance
        /// to be sent back to the server.
        /// </summary>
        public string ValueA { get; set; }

        public string ValueB { get; set; }
    }

    public class RoundtripOriginal_TestEntity
    {
        [Key]
        public int ID { get; set; }

        public int RoundtrippedMember { get; set; }

        public int NonRoundtrippedMember { get; set; }
    }

    public class RoundtripOriginal_TestEntity2
    {
        [Key]
        public int ID { get; set; }

        // This member level RTO attribute is to test that the member level RTOs dont 
        // get propagated to the client if there is an RTO on the type. 
        public int RoundtrippedMember1 { get; set; }

        public int RoundtrippedMember2 { get; set; }

        [Association("RTO_RTO2", "ID", "ID")]
        public RoundtripOriginal_TestEntity AssocProp { get; set; }
    }

    public class TestEntityForInvokeOperations
    {
        [Key]
        public int Key { get; set; }
        public string StrProp { get; set; }
        public TestCT CTProp { get; set; }
    }

    public class TestCT
    {
        public int CTProp1 { get; set; }
        public string CTProp2 { get; set; }
    }

    public class Cart
    {
        [Key]
        public int CartId
        {
            get;
            set;
        }

        [Association("CartItem_Cart", "CartId", "CartItemId")]
        public IEnumerable<CartItem> Items
        {
            get
            {
                // Returns null for test purposes. See InsertThrows_AssociationCollectionPropertyIsNull.
                return null;
            }
        }

        // Indexer should be ignored by code generator and other parts of the code.
        public string this[int index]
        {
            get
            {
                return null;
            }
            set
            {
            }
        }
    }

    public class CartItem
    {
        [Key]
        public int CartItemId
        {
            get;
            set;
        }

        public int CartId
        {
            get;
            set;
        }

        [Association("CartItem_Cart", "CartItemId", "CartId", IsForeignKey = true)]
        public Cart Cart
        {
            get;
            set;
        }

        public string Data
        {
            get;
            set;
        }
    }

    public class EntityWithXElement
    {
        [Key]
        public int ID
        {
            get;
            set;
        }

        public XElement XElem
        {
            get;
            set;
        }
    }

    public class EntityWithIndexer
    {
        [Key]
        public int Prop1 { get; set; }
        // Indexer property. Should be ignored.
        public int this[int index]
        {
            get { return 0; }
            set { }
        }
    }

    public class CityWithCacheData
    {
        public CityWithCacheData()
        {
        }

        [Key]
        public string Name { get; set; }

        [Key]
        public string StateName { get; set; }

        public string CacheData { get; set; }
    }

    /// <summary>
    /// This server-only valicator should not be exposed as a shared type.
    /// </summary>
    public static class ServerOnlyValidator
    {
        public static ValidationResult IsStringValid(string name, ValidationContext context)
        {
            return ValidationResult.Success;
        }

        public static ValidationResult IsObjectValid(A a, ValidationContext context)
        {
            return ValidationResult.Success;
        }
    }

    [CustomValidation(typeof(ServerOnlyValidator), "IsObjectValid")]
    [DataContract]
    public class A
    {
        private string readOnlyData_NoSetter;
        private string readOnlyData_WithSetter;
        private string readOnlyData_NoReadOnlyAttribute;
        private int excludedMember = 42;

        public A()
        {
        }

        public A(string readOnlyData_NoSetter, string readOnlyData_WithSetter, string readOnlyData_NoReadOnlyAttribute)
        {
            this.readOnlyData_NoSetter = readOnlyData_NoSetter;
            this.readOnlyData_WithSetter = readOnlyData_WithSetter;
            this.readOnlyData_NoReadOnlyAttribute = readOnlyData_NoReadOnlyAttribute;
        }

        [Key]
        [DataMember]
        public int ID
        {
            get;
            set;
        }

        [DataMember]
        public int BID1
        {
            get;
            set;
        }

        [DataMember]
        public int BID2
        {
            get;
            set;
        }

        [StringLength(1234, ErrorMessageResourceType = typeof(string), ErrorMessageResourceName = "NonExistentProperty")]
        [CustomValidation(typeof(ServerOnlyValidator), "IsStringValid")]
        [Required]
        [Editable(true)]
        [DataMember]
        [CustomNamespace.Custom]
        public string RequiredString
        {
            get;
            set;
        }

        // Verify a one way singleton association (B doesn't have a collection for this association A_B)
        [Association("A_B", "BID1, BID2", "ID1, ID2", IsForeignKey = true)]
        public B B
        {
            get;
            set;
        }

        /// <summary>
        /// Read only because of the [Editable(false)] attribute and the fact that
        /// there is no setter - we expect [Editable(false)] to be applied to 
        /// the generated member
        /// </summary>
        [DataMember]
        [Editable(false)]
        public string ReadOnlyData_NoSetter
        {
            get
            {
                return this.readOnlyData_NoSetter;
            }
        }

        /// <summary>
        /// Read only because of the [Editable(false)] attribute - we expect [Editable(false)]
        /// to be applied to the generated member
        /// </summary>
        [DataMember]
        [Editable(false)]
        public string ReadOnlyData_WithSetter
        {
            get
            {
                return this.readOnlyData_WithSetter;
            }
            set
            {
                this.readOnlyData_WithSetter = value;
            }
        }

        /// <summary>
        /// Read only because there is no setter - we expect [Editable(false)]
        /// to be applied to the generated member
        /// </summary>
        [DataMember]
        public string ReadOnlyData_NoReadOnlyAttribute
        {
            get
            {
                return this.readOnlyData_NoReadOnlyAttribute;
            }
        }

        /// <summary>
        /// This member is used in a test to verify that even if the client
        /// sends a value for an excluded member, it is never set.
        /// </summary>
        [DataMember]
        [Exclude]
        public int ExcludedMember
        {
            get
            {
                return this.excludedMember;
            }
            set
            {
                this.excludedMember = value;
                // this exception will verify that during deserialization
                // the setter is never called
                //throw new Exception("Excluded member should not be set!");
            }
        }
    }

    [DataContract]
    public class B
    {
        [Key]
        [DataMember]
        public int ID1
        {
            get;
            set;
        }
        [Key]
        [DataMember]
        public int ID2
        {
            get;
            set;
        }

        // verify a one way collection association (C doesn't have a ref back for this association B_C)
        [Association("B_C", "ID1, ID2", "BID1, BID2")]
        [Display(Description = "Cs")]
        public IEnumerable<C> Cs
        {
            get;
            set;
        }
    }

    [DataContract]
    public class C
    {
        [Key]
        [DataMember]
        public int ID
        {
            get;
            set;
        }

        // Below we have two FK values referencing a B, but
        // no actual association member.  These are used for a
        // one way collection association from B to C.
        [DataMember]
        public int BID1
        {
            get;
            set;
        }

        [DataMember]
        public int BID2
        {
            get;
            set;
        }

        [DataMember]
        public int DID_Ref1
        {
            get;
            set;
        }

        [DataMember]
        public int DID_Ref2
        {
            get;
            set;
        }

        // verify below that we can have two different 1:1 associations between two entities
        [Association("C_D_Ref1", "DID_Ref1", "ID", IsForeignKey = true)]
        [Display(Description = "D_Ref1")]
        public D D_Ref1
        {
            get;
            set;
        }

        [Association("C_D_Ref2", "DID_Ref2", "ID", IsForeignKey = true)]
        public D D_Ref2
        {
            get;
            set;
        }
    }

    [DataContract]
    public class D
    {
        [Key]
        [DataMember]
        [UIHint("TextBlock")]
        [Range(0, 99999)]
        public int ID
        {
            get;
            set;
        }

        [DataMember]
        public int DSelfRef_ID1
        {
            get;
            set;
        }

        [DataMember]
        public int DSelfRef_ID2
        {
            get;
            set;
        }

        // verify that we can have a non FK singleton association
        [Association("C_D_Ref1", "ID", "DID_Ref1")]
        public C C
        {
            get;
            set;
        }

        // verify that we can have a bi-directional self reference,
        // with singleton and collection-sides.
        // For example Employee->Employee (employee's manager reference)
        // For this test, it's important that the collection side of the
        // association is defined first
        [Association("D_D", "ID", "DSelfRef_ID1")]
        public IEnumerable<D> Ds
        {
            get;
            set;
        }

        [Association("D_D", "DSelfRef_ID1", "ID", IsForeignKey = true)]
        public D D1
        {
            get;
            set;
        }

        // verify that we can have a bi-directional self reference,
        // with both singleton sides
        // For example Employee->Employee (employee has a single mentor, and a 
        // mentor has a single employee)
        [Association("D_D2", "ID", "DSelfRef_ID2")]
        public D D2_BackRef
        {
            get;
            set;
        }

        [Association("D_D2", "DSelfRef_ID2", "ID", IsForeignKey = true)]
        public D D2
        {
            get;
            set;
        }

        [DataMember]
        public Binary BinaryData
        {
            get;
            set;
        }
    }

    [DataContract]
    public class Turkishİ2
    {
        [DataMember]
        [Key]
        public int Id
        {
            get;
            set;
        }

        [DataMember]
        public string Data
        {
            get;
            set;
        }
    }

    // This entity should only be used by an invoke operation in TestProvider_Scenarios_CodeGen to 
    // verify that we generate the right code for this type of scenario.
    [DataContract]
    public class EntityUsedInOnlineMethod
    {
        [Key]
        public int Id
        {
            get;
            set;
        }
    }

    [DataContract(Namespace = "CustomNamespace", Name = "CustomName")]
    public class EntityWithDataContract
    {
        [DataMember]
        [Key]
        public int Id
        {
            get;
            set;
        }

        [DataMember]
        public string Data
        {
            get;
            set;
        }

        public string IgnoredData
        {
            get;
            set;
        }
    }

    // Tests [IgnoreDataMember]
    public class EntityWithDataContract2
    {
        [Key]
        public int Id
        {
            get;
            set;
        }

        public string Data
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public string IgnoredData
        {
            get;
            set;
        }
    }

    namespace Saleテ
    {
        public class EntityWithSpecialTypeName
        {
            [DataMember]
            [Key]
            public int Id
            {
                get;
                set;
            }

            [DataMember]
            public string Data
            {
                get;
                set;
            }

            public string IgnoredData
            {
                get;
                set;
            }
        }
    }
    #endregion

    #region Include scenarios
    public partial class IncludesA
    {
        [Key]
        public int ID
        {
            get;
            set;
        }

        public string P1
        {
            get;
            set;
        }

        public string P2
        {
            get;
            set;
        }

        // Three projections off of B, one directly, and the
        // other two via metadata below
        public IncludesB B
        {
            get;
            set;
        }
    }

    [MetadataType(typeof(IncludesAMetadata))]
    public partial class IncludesA
    {

    }

    public class IncludesAMetadata
    {
        public static object B;
    }

    public class IncludesB
    {
        [Key]
        public int ID
        {
            get;
            set;
        }

        public string P1
        {
            get;
            set;
        }

        public IncludesC C
        {
            get;
            set;
        }
    }

    public class IncludesC
    {
        [Key]
        public int ID
        {
            get;
            set;
        }

        public string P1
        {
            get;
            set;
        }

        // non-public property
        private string P2
        {
            get;
            set;
        }

        public IncludesD D
        {
            get;
            set;
        }
    }

    public class IncludesD
    {
        [Key]
        public int ID
        {
            get;
            set;
        }

        public string P1
        {
            get;
            set;
        }

        public string P2
        {
            get;
            set;
        }

        // Unsupported member type
        public object P3
        {
            get;
            set;
        }
    }

    #endregion

    #region Test classes using supported types as properties
    [DataContract]
    public class MixedType
    {
        [Key]
        [DataMember]
        public string ID { get; set; }

        #region supported primitive types
        [DataMember]
        public bool BooleanProp { get; set; }

        [DataMember]
        public byte ByteProp { get; set; }

        [DataMember]
        public sbyte SByteProp { get; set; }

        [DataMember]
        public Int16 Int16Prop { get; set; }

        [DataMember]
        public UInt16 UInt16Prop { get; set; }

        [DataMember]
        public Int32 Int32Prop { get; set; }

        [DataMember]
        public UInt32 UInt32Prop { get; set; }

        [DataMember]
        public Int64 Int64Prop { get; set; }

        [DataMember]
        public UInt64 UInt64Prop { get; set; }

        [DataMember]
        public char CharProp { get; set; }

        [DataMember]
        public double DoubleProp { get; set; }

        [DataMember]
        public Single SingleProp { get; set; }
        #endregion

        #region predefined types
        [DataMember]
        public string StringProp { get; set; }

        [DataMember]
        public decimal DecimalProp { get; set; }

        [DataMember]
        public DateTime DateTimeProp { get; set; }

        [DataMember]
        public TimeSpan TimeSpanProp { get; set; }

        [DataMember]
        public DateTimeOffset DateTimeOffsetProp { get; set; }

        [DataMember]
        public IEnumerable<string> StringsProp { get; set; }

        [DataMember]
        public IEnumerable<DateTime> DateTimesCollectionProp { get; set; }

        [DataMember]
        public IEnumerable<DateTimeOffset> DateTimeOffsetsCollectionProp { get; set; }

        [DataMember]
        public List<TimeSpan> TimeSpanListProp { get; set; }

        [DataMember]
        public Guid[] GuidsProp { get; set; }

        [DataMember]
        public ulong[] UInt64sProp { get; set; }

        [DataMember]
        public int[] IntsProp { get; set; }

        [DataMember]
        public TestEnum[] EnumsProp { get; set; }

        [DataMember]
        public Uri UriProp { get; set; }

        [DataMember]
        public Guid GuidProp { get; set; }

        [DataMember]
        public Binary BinaryProp { get; set; }

        [DataMember]
        public byte[] ByteArrayProp { get; set; }

        [DataMember]
        public XElement XElementProp { get; set; }

        [DataMember]
        public TestEnum EnumProp { get; set; }

        [DataMember]
        public IDictionary<string, string> DictionaryStringProp { get; set; }

        [DataMember]
        public IDictionary<DateTime, DateTime> DictionaryDateTimeProp { get; set; }

        [DataMember]
        public IDictionary<DateTimeOffset, DateTimeOffset> DictionaryDateTimeOffsetProp { get; set; }

        [DataMember]
        public IDictionary<Guid, Guid> DictionaryGuidProp { get; set; }

        [DataMember]
        public IDictionary<XElement, XElement> DictionaryXElementProp { get; set; }

        [DataMember]
        public IDictionary<TestEnum, TestEnum> DictionaryTestEnumProp { get; set; }
        #endregion

        #region nullable primitive
        [DataMember]
        public bool? NullableBooleanProp { get; set; }

        [DataMember]
        public byte? NullableByteProp { get; set; }

        [DataMember]
        public sbyte? NullableSByteProp { get; set; }

        [DataMember]
        public Int16? NullableInt16Prop { get; set; }

        [DataMember]
        public UInt16? NullableUInt16Prop { get; set; }

        [DataMember]
        public Int32? NullableInt32Prop { get; set; }

        [DataMember]
        public UInt32? NullableUInt32Prop { get; set; }

        [DataMember]
        public Int64? NullableInt64Prop { get; set; }

        [DataMember]
        public UInt64? NullableUInt64Prop { get; set; }

        [DataMember]
        public char? NullableCharProp { get; set; }

        [DataMember]
        public double? NullableDoubleProp { get; set; }

        [DataMember]
        public Single? NullableSingleProp { get; set; }
        #endregion

        #region nullable predefined
        [DataMember]
        public decimal? NullableDecimalProp { get; set; }

        [DataMember]
        public DateTime? NullableDateTimeProp { get; set; }

        [DataMember]
        public TimeSpan? NullableTimeSpanProp { get; set; }

        [DataMember]
        public DateTimeOffset? NullableDateTimeOffsetProp { get; set; }

        [DataMember]
        public Guid? NullableGuidProp { get; set; }

        [DataMember]
        public TestEnum? NullableEnumProp { get; set; }

        [DataMember]
        public DateTime?[] NullableEnumsArrayProp { get; set; }

        [DataMember]
        public IEnumerable<DateTime?> NullableDateTimesCollectionProp { get; set; }

        [DataMember]
        public List<TimeSpan?> NullableTimeSpanListProp { get; set; }

        [DataMember]
        public IEnumerable<DateTimeOffset?> NullableDateTimeOffsetCollectionProp { get; set; }

        [DataMember]
        public IDictionary<DateTime, DateTime?> NullableDictionaryDateTimeProp { get; set; }

        [DataMember]
        public IDictionary<DateTimeOffset, DateTimeOffset?> NullableDictionaryDateTimeOffsetProp { get; set; }
        #endregion
    }

    // helper class that fully instantiates a few MixedTypes
    public class MixedTypeData
    {
        private MixedType[] _values;

        public MixedTypeData()
        {
            #region instantiation of a few MixedType objects
            _values = new MixedType[]
            {
                new MixedType()
                {
                    ID = "MixedType_Other",
                    BooleanProp = true,
                    ByteProp = 123,
                    SByteProp = 123,
                    Int16Prop = 123,
                    UInt16Prop = 123,
                    Int32Prop = 123,
                    UInt32Prop = 123,
                    Int64Prop = 123,
                    UInt64Prop = 123,
                    CharProp = (char)123,
                    DoubleProp = 123.123,
                    SingleProp = 123,
                    StringProp = "other string",
                    DecimalProp = 123,
                    DateTimeProp = new DateTime(2008, 09, 03),
                    TimeSpanProp = new TimeSpan(123),
                    DateTimeOffsetProp = new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)),
                    StringsProp = new string[] { "hello", "world" },
                    IntsProp = new int[] { 4, 2 },
                    EnumsProp = new TestEnum[] { TestEnum.Value0, TestEnum.Value1 },
                    DateTimesCollectionProp = new List<DateTime>() { DateTime.Now, DateTime.Now },
                    DateTimeOffsetsCollectionProp = new List<DateTimeOffset> { DateTimeOffset.Now, DateTimeOffset.Now },
                    TimeSpanListProp = new List<TimeSpan>() { new TimeSpan(123), new TimeSpan(456) },
                    UriProp = new Uri("http://localhost"),
                    GuidProp = new Guid("12345678-1234-1234-1234-123456789012"),
                    BinaryProp = new Binary(new byte[]{byte.MaxValue, byte.MinValue, 123}),
                    ByteArrayProp = new byte[]{byte.MaxValue, byte.MinValue, 123},
                    XElementProp = XElement.Parse("<someElement>element text</someElement>"),
                    EnumProp = TestEnum.Value2,

                    NullableBooleanProp = true,
                    NullableByteProp = 123,
                    NullableSByteProp = 123,
                    NullableInt16Prop = 123,
                    NullableUInt16Prop = 123,
                    NullableInt32Prop = 123,
                    NullableUInt32Prop = 123,
                    NullableInt64Prop = 123,
                    NullableUInt64Prop = 123,
                    NullableCharProp = (char)123,
                    NullableDoubleProp = 123.123,
                    NullableSingleProp = 123,
                    NullableDecimalProp = 123,
                    NullableDateTimeProp = new DateTime(2008, 09, 03),
                    NullableDateTimesCollectionProp = new List<DateTime?>() { DateTime.Now, null },
                    NullableDateTimeOffsetProp = new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)),
                    NullableDateTimeOffsetCollectionProp = new List<DateTimeOffset?>(){new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)), DateTimeOffset.Now, null },
                    NullableEnumsArrayProp = new DateTime?[] { DateTime.Now, null },
                    NullableTimeSpanListProp = new List<TimeSpan?>() { new TimeSpan(123), null },
                    NullableTimeSpanProp = new TimeSpan(123),
                    NullableGuidProp = new Guid("12345678-1234-1234-1234-123456789012"),
                    NullableEnumProp = TestEnum.Value2,

                    DictionaryStringProp = CreateDictionary("some string"),
                    DictionaryDateTimeProp = CreateDictionary(new DateTime(2008, 09, 03)),
                    DictionaryGuidProp = CreateDictionary(new Guid("12345678-1234-1234-1234-123456789012")),
                    DictionaryTestEnumProp = CreateDictionary(TestEnum.Value2),
                    DictionaryXElementProp = CreateDictionary(XElement.Parse("<someElement>element text</someElement>")),
                    DictionaryDateTimeOffsetProp = CreateDictionary(new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)))
                },
                new MixedType()
                {
                    ID = "MixedType_Max",
                    BooleanProp = true,
                    ByteProp = byte.MaxValue,
                    SByteProp = sbyte.MaxValue,
                    Int16Prop = Int16.MaxValue,
                    UInt16Prop = UInt16.MaxValue,
                    Int32Prop = Int32.MaxValue,
                    UInt32Prop = UInt32.MaxValue,
                    Int64Prop = Int64.MaxValue,
                    UInt64Prop = UInt64.MaxValue,
                    CharProp = (char)0xFFFD, //char.MaxValue,
                    DoubleProp = double.MaxValue,
                    SingleProp = Single.MaxValue,
                    StringProp = "some string",
                    DecimalProp = decimal.MaxValue,
                    DateTimeProp = DateTime.MaxValue,
                    TimeSpanProp = TimeSpan.MaxValue,
                    DateTimeOffsetProp = DateTimeOffset.MaxValue,
                    StringsProp = new string[] { "hello", "world" },
                    IntsProp = new int[] { 4, 2 },
                    EnumsProp = new TestEnum[] { TestEnum.Value0, TestEnum.Value1 },
                    DateTimesCollectionProp = new List<DateTime>() { DateTime.Now, DateTime.Now },
                    DateTimeOffsetsCollectionProp = new List<DateTimeOffset> { DateTimeOffset.MaxValue, DateTimeOffset.MaxValue },
                    TimeSpanListProp = new List<TimeSpan>() { new TimeSpan(123), new TimeSpan(456) },
                    UriProp = new Uri("http://localhost"),
                    GuidProp = new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                    BinaryProp = new Binary(new byte[]{byte.MaxValue}),
                    ByteArrayProp = new byte[]{byte.MaxValue},
                    XElementProp = XElement.Parse("<someElement>max value</someElement>"),
                    EnumProp = TestEnum.Value3,

                    NullableBooleanProp = true,
                    NullableByteProp = byte.MaxValue,
                    NullableSByteProp = sbyte.MaxValue,
                    NullableInt16Prop = Int16.MaxValue,
                    NullableUInt16Prop = UInt16.MaxValue,
                    NullableInt32Prop = Int32.MaxValue,
                    NullableUInt32Prop = UInt32.MaxValue,
                    NullableInt64Prop = Int64.MaxValue,
                    NullableUInt64Prop = UInt64.MaxValue,
                    NullableCharProp = (char)0xFFFD, //char.MaxValue,
                    NullableDoubleProp = double.MaxValue,
                    NullableSingleProp = Single.MaxValue,
                    NullableDecimalProp = decimal.MaxValue,
                    NullableDateTimeProp = DateTime.MaxValue,
                    NullableTimeSpanProp = TimeSpan.MaxValue,
                    NullableDateTimeOffsetProp = DateTimeOffset.MaxValue,
                    NullableDateTimesCollectionProp = new List<DateTime?>() { DateTime.Now, null },
                    NullableDateTimeOffsetCollectionProp = new List<DateTimeOffset?> { DateTimeOffset.MaxValue, null },
                    NullableEnumsArrayProp = new DateTime?[] { DateTime.Now, null },
                    NullableTimeSpanListProp = new List<TimeSpan?>() { new TimeSpan(123), null },
                    NullableGuidProp = new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                    NullableEnumProp = TestEnum.Value3,
                    
                    DictionaryDateTimeProp = CreateDictionary(DateTime.MaxValue),
                    DictionaryDateTimeOffsetProp = CreateDictionary(DateTimeOffset.MaxValue),
                    DictionaryGuidProp = CreateDictionary(new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff")),
                    DictionaryStringProp = CreateDictionary("max string"), 
                    DictionaryTestEnumProp = CreateDictionary(TestEnum.Value2),
                    DictionaryXElementProp = CreateDictionary(XElement.Parse("<someElement>max string</someElement>")),
                },
                new MixedType()
                {
                    ID = "MixedType_Min",
                    BooleanProp = false,
                    ByteProp = byte.MinValue,
                    SByteProp = sbyte.MinValue,
                    Int16Prop = Int16.MinValue,
                    UInt16Prop = UInt16.MinValue,
                    Int32Prop = Int32.MinValue,
                    UInt32Prop = UInt32.MinValue,
                    Int64Prop = Int64.MinValue,
                    UInt64Prop = UInt64.MinValue,
                    CharProp = (char)1, //char.MinValue,
                    DoubleProp = double.MinValue,
                    SingleProp = Single.MinValue,
                    StringProp = "some other string",
                    DecimalProp = decimal.MinValue,
                    DateTimeProp = DateTime.MinValue,
                    TimeSpanProp = TimeSpan.MinValue,
                    DateTimeOffsetProp = DateTimeOffset.MinValue,
                    StringsProp = new string[0],
                    IntsProp = new int[] { 4, 2 },
                    EnumsProp = new TestEnum[] { TestEnum.Value0, TestEnum.Value1 },
                    DateTimesCollectionProp = new List<DateTime>() { DateTime.Now, DateTime.Now },
                    DateTimeOffsetsCollectionProp = new List<DateTimeOffset> { DateTimeOffset.MinValue, DateTimeOffset.MinValue },
                    TimeSpanListProp = new List<TimeSpan>() { new TimeSpan(123), new TimeSpan(456) },
                    UriProp = new Uri("http://localhost"),
                    GuidProp = new Guid("00000000-0000-0000-0000-000000000000"),
                    BinaryProp = new Binary(new byte[]{byte.MinValue}),
                    ByteArrayProp = new byte[]{byte.MinValue},
                    XElementProp = XElement.Parse("<someElement>min value</someElement>"),
                    EnumProp = TestEnum.Value0,

                    NullableBooleanProp = false,
                    NullableByteProp = byte.MinValue,
                    NullableSByteProp = sbyte.MinValue,
                    NullableInt16Prop = Int16.MinValue,
                    NullableUInt16Prop = UInt16.MinValue,
                    NullableInt32Prop = Int32.MinValue,
                    NullableUInt32Prop = UInt32.MinValue,
                    NullableInt64Prop = Int64.MinValue,
                    NullableUInt64Prop = UInt64.MinValue,
                    NullableCharProp = (char)1, //char.MinValue,
                    NullableDoubleProp = double.MinValue,
                    NullableSingleProp = Single.MinValue,
                    NullableDecimalProp = decimal.MinValue,
                    NullableDateTimeProp = DateTime.MinValue,
                    NullableTimeSpanProp = TimeSpan.MinValue,
                    NullableDateTimeOffsetProp = DateTimeOffset.MinValue,
                    NullableDateTimesCollectionProp = new List<DateTime?>() { DateTime.Now, null },
                    NullableDateTimeOffsetCollectionProp = new List<DateTimeOffset?> { DateTimeOffset.MinValue, null },
                    NullableEnumsArrayProp = new DateTime?[] { DateTime.Now, null },
                    NullableTimeSpanListProp = new List<TimeSpan?>() { new TimeSpan(123), null },
                    NullableGuidProp = new Guid("00000000-0000-0000-0000-000000000000"),
                    NullableEnumProp = TestEnum.Value0,

                    DictionaryDateTimeProp = CreateDictionary(DateTime.MinValue),
                    DictionaryDateTimeOffsetProp = CreateDictionary(DateTimeOffset.MinValue),
                    DictionaryGuidProp = CreateDictionary(new Guid("00000000-0000-0000-0000-000000000000")),
                    DictionaryStringProp = CreateDictionary("min string"), 
                    DictionaryTestEnumProp = CreateDictionary(TestEnum.Value1),
                    DictionaryXElementProp = CreateDictionary(XElement.Parse("<someElement>min string</someElement>")),
                }
            };
            #endregion
        }

        public MixedTypeData(bool useSuperset)
            : this()
        {
            if (useSuperset)
            {
                #region instantiation of a superset of MixedType objects
                _values = new MixedType[] { _values[0], _values[1], _values[2],
                    new MixedType()
                    {
                        ID = "MixedType_Negative",
                        BooleanProp = false,
                        ByteProp = 123,
                        SByteProp = -123,
                        Int16Prop = -123,
                        UInt16Prop = 123,
                        Int32Prop = -123,
                        UInt32Prop = 123,
                        Int64Prop = -123,
                        UInt64Prop = 123,
                        CharProp = (char)123,
                        DoubleProp = -123.123,
                        SingleProp = -(Single)123.123,
                        StringProp = "some other string value",
                        DecimalProp = -(Decimal)123.123,
                        DateTimeProp = new DateTime(2008, 09, 03),
                        DateTimeOffsetProp = new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)),
                        TimeSpanProp = new TimeSpan(123),
                        StringsProp = new string[] { "some string", "some other string", "some other string value" },
                        IntsProp = new int[] { -123, 123 },
                        EnumsProp = new TestEnum[] { TestEnum.Value0, TestEnum.Value1, TestEnum.Value2 },
                        DateTimesCollectionProp = new List<DateTime>() { new DateTime(2008, 09, 03), new DateTime(2009, 12, 10) },
                        DateTimeOffsetsCollectionProp = new List<DateTimeOffset> { new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)), new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)) },
                        TimeSpanListProp = new List<TimeSpan>() { new TimeSpan(123), new TimeSpan(456) },
                        UriProp = new Uri("http://localhost"),
                        GuidProp = new Guid("12345678-1234-1234-1234-123456789012"),
                        BinaryProp = new Binary(new byte[] { byte.MaxValue, byte.MinValue, 123 }),
                        ByteArrayProp = new byte[] { byte.MaxValue, byte.MinValue, 123 },
                        XElementProp = XElement.Parse("<someElement>element text</someElement>"),
                        EnumProp = TestEnum.Value2,

                        NullableBooleanProp = false,
                        NullableByteProp = null,
                        NullableSByteProp = -123,
                        NullableInt16Prop = -123,
                        NullableUInt16Prop = 123,
                        NullableInt32Prop = -123,
                        NullableUInt32Prop = 123,
                        NullableInt64Prop = -123,
                        NullableUInt64Prop = 123,
                        NullableCharProp = (char)123,
                        NullableDoubleProp = -123.123,
                        NullableSingleProp = -(Single)123.123,
                        NullableDecimalProp = -(Decimal)123.123,
                        NullableDateTimeProp = null,
                        NullableDateTimeOffsetProp = null,
                        NullableDateTimesCollectionProp = new List<DateTime?>() { new DateTime(2008, 09, 03), new DateTime(2009, 12, 10), null },
                        NullableDateTimeOffsetCollectionProp = new List<DateTimeOffset?> { new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)), new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)) },
                        NullableEnumsArrayProp = new DateTime?[] { new DateTime(2008, 09, 03), new DateTime(2009, 12, 10), null },
                        NullableTimeSpanListProp = new List<TimeSpan?>() { new TimeSpan(123), new TimeSpan(456), null },
                        NullableTimeSpanProp = null,
                        NullableGuidProp = null,
                        NullableEnumProp = null,

                        DictionaryStringProp = CreateDictionary("some string"),
                        DictionaryDateTimeProp = CreateDictionary(new DateTime(2008, 09, 03)),
                        DictionaryDateTimeOffsetProp = CreateDictionary(new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0))),
                        DictionaryGuidProp = CreateDictionary(new Guid("12345678-1234-1234-1234-123456789012")),
                        DictionaryTestEnumProp = CreateDictionary(TestEnum.Value2),
                        DictionaryXElementProp = CreateDictionary(XElement.Parse("<someElement>element text</someElement>"))
                    },
                    new MixedType()
                    {
                        ID = "MixedType_Null",
                        BooleanProp = true,
                        ByteProp = 123,
                        SByteProp = -123,
                        Int16Prop = -123,
                        UInt16Prop = 123,
                        Int32Prop = -123,
                        UInt32Prop = 123,
                        Int64Prop = -123,
                        UInt64Prop = 123,
                        CharProp = (char)123,
                        DoubleProp = -123.123,
                        SingleProp = -(Single)123.123,
                        StringProp = "some other string value",
                        DecimalProp = -(Decimal)123.123,
                        DateTimeProp = new DateTime(2008, 09, 03),
                        DateTimeOffsetProp = new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)),
                        TimeSpanProp = new TimeSpan(123),
                        StringsProp = new string[] { "some string", "some other string", "some other string value" },
                        IntsProp = new int[] { -123, 123 },
                        EnumsProp = new TestEnum[] { TestEnum.Value0, TestEnum.Value1, TestEnum.Value2 },
                        DateTimesCollectionProp = new List<DateTime>() { new DateTime(2008, 09, 03), new DateTime(2009, 12, 10) },
                        DateTimeOffsetsCollectionProp = new List<DateTimeOffset>() { new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)), new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)) },
                        TimeSpanListProp = new List<TimeSpan>() { new TimeSpan(123), new TimeSpan(456) },
                        UriProp = new Uri("http://localhost"),
                        GuidProp = new Guid("12345678-1234-1234-1234-123456789012"),
                        BinaryProp = new Binary(new byte[] { byte.MaxValue, byte.MinValue, 123 }),
                        ByteArrayProp = new byte[] { byte.MaxValue, byte.MinValue, 123 },
                        XElementProp = XElement.Parse("<someElement>element text</someElement>"),
                        EnumProp = TestEnum.Value2,

                        NullableBooleanProp = null,
                        NullableByteProp = null,
                        NullableSByteProp = null,
                        NullableInt16Prop = null,
                        NullableUInt16Prop = null,
                        NullableInt32Prop = null,
                        NullableUInt32Prop = null,
                        NullableInt64Prop = null,
                        NullableUInt64Prop = null,
                        NullableCharProp = null,
                        NullableDoubleProp = null,
                        NullableSingleProp = null,
                        NullableDecimalProp = null,
                        NullableDateTimeProp = null,
                        NullableDateTimeOffsetProp = null,
                        NullableDateTimesCollectionProp = new List<DateTime?>() { new DateTime(2008, 09, 03), new DateTime(2009, 12, 10), null },
                        NullableDateTimeOffsetCollectionProp = new List<DateTimeOffset?> { new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)), new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)) },
                        NullableEnumsArrayProp = new DateTime?[] { new DateTime(2008, 09, 03), new DateTime(2009, 12, 10), null },
                        NullableTimeSpanListProp = new List<TimeSpan?>() { new TimeSpan(123), new TimeSpan(456), null },
                        NullableTimeSpanProp = null,
                        NullableGuidProp = null,
                        NullableEnumProp = null,

                        DictionaryStringProp = CreateDictionary("some string"),
                        DictionaryDateTimeProp = CreateDictionary(new DateTime(2008, 09, 03)),
                        DictionaryDateTimeOffsetProp = CreateDictionary(new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0))),
                        DictionaryGuidProp = CreateDictionary(new Guid("12345678-1234-1234-1234-123456789012")),
                        DictionaryTestEnumProp = CreateDictionary(TestEnum.Value2),
                        DictionaryXElementProp = CreateDictionary(XElement.Parse("<someElement>element text</someElement>"))
                    }
                };
                #endregion
            }
        }

        public MixedType[] Values
        {
            get { return _values; }
        }

        private static Dictionary<TType, TType> CreateDictionary<TType>(TType seed)
        {
            return CreateDictionary(seed, seed);
        }

        private static Dictionary<TKey, TValue> CreateDictionary<TKey, TValue>(TKey seedKey, TValue seedValue)
        {
            var d = new Dictionary<TKey, TValue>();
            d.Add(seedKey, seedValue);
            return d;
        }
    }
    #endregion

    #region Test Entities and Providers used to verify Cross DomainContext functionality

    public partial class MockCustomer
    {
        [Key]
        public int CustomerId { get; set; }
        public string CityName { get; set; }
        public string StateName { get; set; }

        //[Association("Customer_City", "CityName,StateName", "Name,StateName", IsForeignKey = true)]
        //public Cities.City City { get; set; }

        //[Association("Customer_PreviousResidences", "StateName", "StateName")]
        //public List<Cities.City> PreviousResidences { get; set; }
    }

    [DataContract(Name = "MR", Namespace = "Mock.Models")]
    public partial class MockReport
    {
        [DataMember(Name = "CId")]
        public int CustomerId { get; set; }

        // project from customer
        [Association("R_C", "CustomerId", "CustomerId")]
        public MockCustomer Customer { get; set; }

        [Key]
        [DataMember(Name = "REFId", Order = 1)]
        public int ReportElementFieldId { get; set; }

        [DataMember(Name = "Title", IsRequired = true)]
        public string ReportTitle { get; set; }

        [DataMember(Name = "Data")]
        public MockReportBody ReportBody { get; set; }
    }

    [DataContract(Name = "MRB", Namespace = "Mock.Models")]
    public partial class MockReportBody
    {
        [DataMember(Name = "EntryDate", Order = 1)]
        public DateTime TimeEntered { get; set; }

        [DataMember(Name = "Body", Order = 2, EmitDefaultValue = false)]
        public string Report { get; set; }
    }

    #region Test RequiresSecureEndpoint

    public class TestEntity_RequiresSecureEndpoint
    {
        [Key]
        public int Key { get; set; }
    }

    #endregion

    #region test provider and entity for Bug 626901

    public class A_Bug626901
    {

        [Key]
        public int ID { get; set; }  // B_ID instead of ID it would work

        [Association("A_Bug626901_B_Bug626901", "B_ID", "ID", IsForeignKey = true)]
        public B_Bug626901 B { get; set; }

    }

    public class B_Bug626901
    {

        [Key]
        public int ID { get; set; }

        [Association("A_Bug626901_B_Bug626901", "ID", "B_ID")]  // This has B_ID which does not exist in A
        public A_Bug626901 A { get; set; }

    }
    #endregion

    #region test provider and entity for Bug 629280

    public class A_Bug629280
    {
        [Key]
        public int ID { get; set; }

        [Range(typeof(DateTime), "1/1/1980", "1/1/2001")]
        public DateTime RangeWithDateTime { get; set; }

        [Range(1.1d, 1.1d)]
        public double RangeWithDouble { get; set; }

        [Range(typeof(double), "1.1", "1.1")]
        public double RangeWithDoubleAsString { get; set; }

        [Range(1, 1)]
        public int RangeWithInteger { get; set; }

        [Range(typeof(int), "1", "1")]
        public int RangeWithIntegerAsString { get; set; }

        [Range(typeof(int), null, null)]
        public int RangeWithNullStrings { get; set; }

        [Range(typeof(int), null, "1")]
        public int RangeWithNullString1 { get; set; }

        [Range(typeof(int), "1", null)]
        public int RangeWithNullString2 { get; set; }

        [Range(1, 10, ErrorMessage = "Range must be between 1 and 10")]
        public int RangeWithErrorMessage { get; set; }

        [Range(1, 10, ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "String")]
        public int RangeWithResourceMessage { get; set; }
    }

    public static class SharedResource
    {
        public static string String { get { return "SharedResource.String"; } }
    }

    #endregion

    public class MockOrder
    {
        [Key]
        public int OrderID { get; set; }

        [Exclude]
        [Association("Order_OrderDetails", "OrderID", "OrderID", IsForeignKey = false)]
        public List<MockOrderDetails> MockOrderDetails { get; set; }
    }

    public class MockOrderDetails
    {
        [Key]
        public int Key { get; set; }
        public int OrderID { get; set; }

        [Association("Order_OrderDetails", "OrderID", "OrderID", IsForeignKey = true)]
        public MockOrder MockOrder { get; set; }
    }

    #endregion //Test provider and entity for Bug 796616

    #region Excluded Properties Validation Scenarios
    [CustomValidation(typeof(CustomExcludeValidator), "Validate")]
    public partial class ExcludeValidationEntity
    {
        [Key]
        public int K { get; set; }

        [Range(1, 10)]
        public double P1to10 { get; set; }

        [Exclude]
        [Range(1, 10)]
        public double P1to10Excluded { get; set; }

        [Exclude]
        [Range(1, 20)]
        public double P1to20Excluded { get; set; }
    }

    public static class CustomExcludeValidator
    {
        public static ValidationResult Validate(ExcludeValidationEntity entity, ValidationContext validationContext)
        {
            if (entity.P1to20Excluded > 10)
            {
                return new ValidationResult("error", new string[] { "P1to10Excluded", "P1to20Excluded" });
            }
            else
            {
                return ValidationResult.Success;
            }
        }
    }

    #endregion

    #region Entities for RoundtripOriginalScenarios

    public class EntityWithRoundtripOriginal_Derived : EntityWithoutRoundtripOriginal_Base
    {
        public string PropD { get; set; }
    }

    [KnownType(typeof(EntityWithRoundtripOriginal_Derived))]
    public class EntityWithoutRoundtripOriginal_Base
    {
        [Key]
        public int Key { get; set; }
        public string PropB { get; set; }
    }

    public class RoundtripOriginalTestEntity_A
    {
        public string PropA { get; set; }
    }

    [KnownType(typeof(RoundtripOriginalTestEntity_D))]
    public class RoundtripOriginalTestEntity_B
    {
        [Key]
        public int Key { get; set; }
    }
    public class RoundtripOriginalTestEntity_C
    {
        public int PropC { get; set; }
    }
    public class RoundtripOriginalTestEntity_D
    {
        public int PropD { get; set; }
    }

    public class RTO_EntityWithRoundtripOriginalOnAssociationPropType
    {
        [Key]
        public int ID { get; set; }
        [Association("Assoc1_B", "ID", "Key")]
        public RoundtripOriginalTestEntity_B PropWithTypeLevelRTO { get; set; }
    }

    public class RTO_EntityWithRoundtripOriginalOnAssociationProperty
    {
        [Key]
        public int ID { get; set; }
        [Association("Assoc2_B", "ID", "Key")]
        public EntityWithoutRoundtripOriginal_Base PropWithPropLevelRTO { get; set; }
    }

    public class RTO_EntityWithRoundtripOriginalOnAssociationPropertyAndOnEntity
    {
        [Key]
        public int ID { get; set; }
        [Association("Assoc2_B", "ID", "Key")]
        public EntityWithoutRoundtripOriginal_Base PropWithPropLevelRTO { get; set; }
    }

    public class RTO_EntityWithRoundtripOriginalOnMember
    {
        [Key]
        public int ID { get; set; }

        public string PropWithPropLevelRTO { get; set; }
    }
    #endregion
}

#region LTS Northwind Scenarios

namespace DataTests.Scenarios.LTS.Northwind
{
    [MetadataType(typeof(OrderMetadata))]
    public partial class Order_Bug479436
    {
    }

    public class OrderMetadata
    {
        public static object Customer;
    }
}

#endregion LTS Northwind Scenarios

#region VB Root Namespace Scenarios
namespace VBRootNamespaceTest
{
    using VBRootNamespaceTest.Other;
    using VBRootNamespaceTest3;

    [DataContract()]
    public class VBRootNamespaceDomainObject
    {
        [DataMember()]
        [Key()]
        public int Key { get; set; }
    }

    namespace Inner
    {
        [DataContract()]
        public class VBRootNamespaceDomainObjectInsideInner
        {
            [DataMember()]
            [Key()]
            public int Key { get; set; }
        }
    }
}

namespace VBRootNamespaceTest2
{
    [DataContract()]
    public class VBRootNamespaceDomainObject2
    {
        [DataMember()]
        [Key()]
        public int Key { get; set; }
    }
}

namespace VBRootNamespaceTest.Other
{
    [DataContract()]
    public class VBRootNamespaceDomainObject4
    {
        [DataMember()]
        [Key()]
        public int Key { get; set; }
    }
}

namespace VBRootNamespaceTest3
{
    [DataContract()]
    public class VBRootNamespaceDomainObject3
    {
        [DataMember()]
        [Key()]
        public int Key { get; set; }
    }

    public class VBRootNamespaceEntityWithComplexProperty
    {
        [Key]
        public int Key { get; set; }
        public ComplexType ComplexProp { get; set; }
    }

    public class ComplexType
    {
        public int Prop { get; set; }
    }
}

#endregion

#region Conflict Resolution Scenarios

namespace TestNamespace.TypeNameConflictResolution
{
    public class Attribute
    {
        [Key]
        public string Name { get; set; }
    }

    public class DataMemberAttribute
    {
        [Key]
        public string Name { get; set; }
    }

    public class DataMember
    {
        [Key]
        public string Name { get; set; }
    }

    public class Entity
    {
        [Key]
        public string Name { get; set; }
    }

    public class DomainContext
    {
        [Key]
        public int DataContextID { get; set; }
    }

    namespace ExternalConflicts
    {
        namespace Namespace1
        {
            public class MockEntity1
            {
                [Key]
                public int EntityID { get; set; }

                [Association("MockEntity1_1", "EntityID", "EntityID")]
                public Namespace1.MockEntity2 Namespace1Entity2 { get; set; }

                [Association("MockEntity1_2", "EntityID", "EntityID")]
                public Namespace2.MockEntity1[] Namespace2Entity1 { get; set; }
            }

            public class MockEntity2
            {
                [Key]
                public int EntityID { get; set; }

                [Association("MockEntity2_1", "EntityID", "EntityID")]
                public Namespace1.MockEntity1 Namespace1Entity1 { get; set; }

                [Association("MockEntity2_2", "EntityID", "EntityID")]
                public Namespace2.MockEntity2[] Namespace2Entity2 { get; set; }
            }
        }

        namespace Namespace2
        {
            public class MockEntity1
            {
                [Key]
                public int EntityID { get; set; }
            }

            public class MockEntity2
            {
                [Key]
                public int EntityID { get; set; }
            }

            public class MockEntity3
            {
                [Key]
                public int EntityID { get; set; }
            }
        }

        namespace Namespace3
        {
            public class MockEntityWithExternalReferences
            {
                [Key]
                public int EntityID { get; set; }

                public Namespace1.MockEntity1 Namespace1Entity1 { get; set; }

                public Namespace2.MockEntity2 Namespace2Entity2 { get; set; }

                public Namespace2.MockEntity3 Namespace2Entity3 { get; set; }
            }
        }
    }
}

#endregion Conflict Resolution Scenarios

#region Global Namespace Scenarios

public class GlobalNamespaceTest_Entity_Invalid
{
    [Key]
    public int Key { get; set; }
}

public enum GlobalNamespaceTest_Enum_NonShared
{
    DefaultValue,
    NonDefaultValue,
}

#endregion

#region System Namespace Scenarios

namespace System
{
    //[SystemNamespace]
    public class SystemEntity
    {
        [Key]
        public int Key { get; set; }

        //[SystemNamespace]
        //public SystemEnum SystemEnum { get; set; }

        //[Subsystem.SubsystemNamespace]
        //public Subsystem.SubsystemEnum SubsystemEnum { get; set; }

        public SystemGeneratedEnum SystemGeneratedEnum { get; set; }
    }

    public enum SystemGeneratedEnum
    {
        SystemGeneratedEnumValue
    }

    namespace Subsystem
    {
        public class SubsystemEntity
        {
            [Key]
            public int Key { get; set; }

            //[SystemNamespace]
            //public SystemEnum SystemEnum { get; set; }

            //[SubsystemNamespace]
            //public SubsystemEnum SubsystemEnum { get; set; }

            public SubsystemGeneratedEnum SubsystemGeneratedEnum { get; set; }
        }

        public enum SubsystemGeneratedEnum
        {
            SubsystemGeneratedEnumValue
        }
    }
}

namespace SystemExtensions
{
    public class SystemExtensionsEntity
    {
        [Key]
        public int Key { get; set; }

        //[SystemExtensionsNamespace]
        //public SystemExtensionsEnum SystemExtensionsEnum { get; set; }

        public SystemExtensionsGeneratedEnum SystemExtensionsGeneratedEnum { get; set; }
    }

    public enum SystemExtensionsGeneratedEnum
    {
        SystemExtensionsGeneratedEnumValue
    }
}

#endregion
