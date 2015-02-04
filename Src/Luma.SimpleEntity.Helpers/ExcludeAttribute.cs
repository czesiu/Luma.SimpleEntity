using System;

namespace Luma.SimpleEntity.Helpers
{
    /// <summary>
    /// Indicates that an entity member should not exist in the code generated 
    /// client view of the entity.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class ExcludeAttribute : Attribute
    {
    }
}
