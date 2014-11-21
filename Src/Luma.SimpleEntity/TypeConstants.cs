namespace Luma.SimpleEntity
{
    /// <summary>
    /// Since this code generator doesn't have assembly references to the server/client
    /// framework assemblies, we're using type name strings rather than type references
    /// during codegen.
    /// </summary>
    internal static class TypeConstants
    {
        /// <summary>
        /// The 'Luma.SimpleEntity.Client.EntityRef' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntityRefTypeFullName = "Luma.Client.EntityRef";

        /// <summary>
        /// The 'Luma.SimpleEntity.Client.EntityCollection' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntityCollectionTypeFullName = "System.Collections.ObjectModel.ObservableCollection";

        /// <summary>
        /// The 'Luma.SimpleEntity.Client.EntityType' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntityTypeFullName = "Luma.Client.Entity";
    }
}
