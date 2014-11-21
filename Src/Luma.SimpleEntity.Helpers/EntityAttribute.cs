using System;

namespace Luma.SimpleEntity.Helpers
{
    /// <summary>
    /// Indicates that a specific class is an entity and client code should be generated.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class EntityAttribute : Attribute
    {
    }
}