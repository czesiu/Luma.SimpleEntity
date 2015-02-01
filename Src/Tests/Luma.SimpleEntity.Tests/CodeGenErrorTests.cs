using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Luma.SimpleEntity.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Luma.SimpleEntity.Tests
{
    /// <summary>
    /// Summary description for domain service code gen with errors
    /// </summary>
    [TestClass]
    public class CodeGenErrorTests
    {
        [TestMethod]
        public void CodeGen_Key_Type_Not_Supported()
        {
            string error = string.Format(Resource.EntityCodeGen_EntityKey_PropertyNotSerializable, typeof(Mock_CG_Key_Type_Not_Serializable), "KeyField");
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_CG_Key_Type_Not_Serializable), error);

            error = string.Format(Resource.EntityCodeGen_EntityKey_KeyTypeNotSupported, typeof(Mock_CG_Key_Type_Complex), "KeyField", typeof(List<string>));
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_CG_Key_Type_Complex), error);
        }
        
        [TestMethod]
        [WorkItem(566732)]
        public void CodeGen_EntityWithStructProperty()
        {
            string error = string.Format(Luma.SimpleEntity.Server.Resource.Invalid_Entity_Property, typeof(Mock_CG_Entity_WithStructProperty).FullName, "StructProperty");
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_CG_Entity_WithStructProperty), error);
        }

        [TestMethod]
        [Description("Entity type with an excluded key")]
        public void CodeGen_Attribute_Entity_Excluded_Key()
        {
            string error = string.Format(Luma.SimpleEntity.Server.Resource.Entity_Has_No_Key_Properties, typeof(Mock_CG_Attr_Entity_Excluded_Key).Name, typeof(Mock_CG_Attr_Entity_Excluded_Key).Name);
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_CG_Attr_Entity_Excluded_Key), error);
        }

        [TestMethod]
        [Description("Entity type with no default constructor")]
        public void CodeGen_Entity_No_Default_Constructor()
        {
            string error = "Type 'Mock_CG_Entity_No_Default_Constructor' is not a valid entity type.  Entity types must have a default constructor.";
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_CG_Entity_No_Default_Constructor), error);
        }

        [TestMethod]
        [Description("Generic entity type")]
        public void CodeGen_Generic_Entity()
        {
            string error = "Type 'Mock_CG_Generic_Entity`1' is not a valid entity type.  Entity types cannot be generic.";
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_CG_Generic_Entity<object>), error);
        }

        [TestMethod]
        [Description("Verifies errors are generated when a non-simple type is used as a Dictionary generic argument.")]
        public void CodeGen_DictionaryMember_InvalidGenericArg()
        {
            string error = "Entity 'Luma.SimpleEntity.Tests.CodeGenErrorTests+Mock_CG_Entity_InvalidDictionaryMember' has a property 'UnsupportedMemberType' with an unsupported type.";
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_CG_Entity_InvalidDictionaryMember), error);
        }

        [TestMethod]
        [Description("Verifies errors are generated when an Entity type is used as a Dictionary generic argument.")]
        public void CodeGen_DictionaryMember_InvalidGenericArg_EntityUsedAsDictionaryTypeArg()
        {
            string error = "Entity 'Luma.SimpleEntity.Tests.CodeGenErrorTests+Mock_CG_Entity_InvalidDictionaryMember_EntityUsedAsDictionaryTypeArg' has a property 'EntityUsedAsDictionaryTypeArg' with an unsupported type.";
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_CG_Entity_InvalidDictionaryMember_EntityUsedAsDictionaryTypeArg), error);
        }

        public class Mock_CG_Entity_InvalidDictionaryMember
        {
            [Key]
            public int ID { get; set; }
            public Dictionary<string, List<string>> UnsupportedMemberType { get; set; }
        }


        public class Mock_CG_Entity_InvalidDictionaryMember_EntityUsedAsDictionaryTypeArg
        {
            [Key]
            public int ID { get; set; }
            public Dictionary<string, MockEntity1> EntityUsedAsDictionaryTypeArg { get; set; }
        }

        public partial class Mock_CG_Attr_Entity_Invalid_Include
        {
            [Key]
            public int KeyField { get; set; }

            public string StringField { get; set; }
        }

        public partial class Mock_CG_Entity_No_Default_Constructor
        {
            [Key]
            public int KeyField { get; set; }

            public string StringField { get; set; }

            private Mock_CG_Entity_No_Default_Constructor()
            {
                //
            }
        }

        public class Mock_CG_Generic_Entity<T>
        {
        }

        public partial class Mock_CG_Attr_Entity_Excluded_Key
        {
            [Key]
            [Exclude]
            public int KeyField { get; set; }

            public string StringField { get; set; }
        }

        public partial class Mock_CG_SimpleEntity
        {
            [Key]
            public int KeyField { get; set; }

            public string StringField { get; set; }
        }

        public class Mock_CG_Entity_WithStructProperty : Mock_CG_SimpleEntity
        {
            public Mock_CG_Entity_StructProperty StructProperty { get; set; }
        }

        public struct Mock_CG_Entity_StructProperty { }

        public class Mock_CG_SimpleEntity_WithTdp : Mock_CG_SimpleEntity
        {
        }

        #region Mock Entities

        [DataContract]
        public class Bug523677_Entity1
        {
            [Key]
            [DataMember]
            public int ID
            {
                get;
                set;
            }

            public Bug523677_Entity2 E
            {
                get;
                set;
            }
        }

        [DataContract]
        public class Bug523677_Entity2
        {
            [Key]
            [DataMember]
            public int ID
            {
                get;
                set;
            }
        }

        [DataContract]
        public class Mock_CG_MinimalEntity
        {
            [DataMember]
            [Key]
            public int ID { get; set; }
        }

        [DataContract]
        public class Mock_CG_Key_Type_Not_Serializable
        {
            [Key]
            public int KeyField { get; set; }
        }

        [DataContract]
        public class Mock_CG_Key_Type_Complex
        {
            [DataMember]
            [Key]
            public List<string> KeyField { get; set; }
        }

        [DataContract]
        public class Mock_CG_Key_Type_Uri
        {
            [DataMember]
            [Key]
            public Uri KeyField { get; set; }
        }

        public struct Mock_CG_Struct
        {
            public int MockValue;
        }

        #endregion
    }
}
