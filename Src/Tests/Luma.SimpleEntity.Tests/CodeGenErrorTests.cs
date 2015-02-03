using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Luma.SimpleEntity.Helpers;
using Luma.SimpleEntity.TestHelpers;
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
        
        [Ignore]
        [TestMethod]
        public void CodeGen_EntityWithStructProperty()
        {
            string error = string.Format(SimpleEntity.Server.Resource.Invalid_Entity_Property, typeof(Mock_CG_Entity_WithStructProperty).FullName, "StructProperty");
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_CG_Entity_WithStructProperty), error);
        }

        [TestMethod]
        [Description("Entity type with no default constructor")]
        public void CodeGen_Entity_No_Default_Constructor()
        {
            UnitTestHelper.EnsureEnglish();
            var error = "Type 'Mock_CG_Entity_No_Default_Constructor' is not a valid entity type.  Entity types must have a default constructor.\r\nParameter name: entityType";
            TestHelper.GenerateCodeAssertWarnings("C#", new[] { typeof(Mock_CG_Entity_No_Default_Constructor), typeof(Mock_CG_SimpleEntity) }, error);
        }


        [TestMethod]
        [Description("Generic entity type")]
        public void CodeGen_Generic_Entity()
        {
            UnitTestHelper.EnsureEnglish();
            var error = "Type 'Mock_CG_Generic_Entity`1' is not a valid entity type.  Entity types cannot be generic.\r\nParameter name: entityType";
            TestHelper.GenerateCodeAssertWarnings("C#", new[] { typeof(Mock_CG_Generic_Entity<object>), typeof(Mock_CG_SimpleEntity) }, error);
        }

        public class Mock_CG_Entity_No_Default_Constructor
        {
            [Key]
            public int KeyField { get; set; }

            public string StringField { get; set; }

            private Mock_CG_Entity_No_Default_Constructor()
            {
                //
            }
        }

        public class Mock_CG_Attr_Entity_Excluded_Key
        {
            [Key]
            [Exclude]
            public int KeyField { get; set; }

            public string StringField { get; set; }
        }
        public class Mock_CG_Generic_Entity<T>
        {
        }

        public class Mock_CG_SimpleEntity
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
