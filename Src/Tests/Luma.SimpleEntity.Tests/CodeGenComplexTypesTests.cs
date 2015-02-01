using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Luma.SimpleEntity.Helpers;

namespace Luma.SimpleEntity.Tests
{
    public class ComplexCodeGen_Entity
    {
        [Key]
        public int Key { get; set; }
    }

    #region Test Automatic Attributes

    public class CodeGenComplexTypes_Attributes_Entity : ComplexCodeGen_Entity
    {
        public CodeGenComplexType_Attributes_ComplexType_WithComplexType ComplexType { get; set; }
    }
    
    public class CodeGenComplexTypes_Attributes_Entity_WithComplexType : ComplexCodeGen_Entity
    {
        public CodeGenComplexTypes_Attributes_ComplexType ComplexType { get; set; }
        public IEnumerable<CodeGenComplexTypes_Attributes_ComplexType> ComplexTypes { get; set; }
    }

    public class CodeGenComplexTypes_Attributes_ComplexType
    {
    }

    public class CodeGenComplexType_Attributes_ComplexType_WithComplexType
    {
        public CodeGenComplexTypes_Attributes_ComplexType ComplexType { get; set; }
        public IEnumerable<CodeGenComplexTypes_Attributes_ComplexType> ComplexTypes { get; set; }
    }
    #endregion

    #region Test Exclude
   
    public class ComplexCodeGen_Exclude_Entity : ComplexCodeGen_Entity
    {
        [Exclude]
        public ComplexCodeGen_CT_Excluded_Via_Property PropertyExclude { get; set; }

        public ComplexCodeGen_Exclude_CT ExcludeCT { get; set; }
    }

    public class ComplexCodeGen_Exclude_CT
    {
        [Exclude]
        public ComplexCodeGen_CT_Excluded_Via_Property PropertyExclude { get; set; }
    }

    public class ComplexCodeGen_CT_Excluded_Via_Property { }
    #endregion

    #region Sharing complex types
    
    public class ComplexCodeGen_SharedComplexType_SharedComplexTypes_CT
    {
    }
    #endregion
}
