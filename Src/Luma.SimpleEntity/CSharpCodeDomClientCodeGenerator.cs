using Luma.SimpleEntity.Tools;

namespace Luma.SimpleEntity
{
    /// <summary>
    /// CodeDom-based client proxy code generator for C#.
    /// </summary>
    /// <remarks>
    /// This internal class exists solely to provide a unique export
    /// for the C# language.
    /// </remarks>
    [ClientCodeGenerator(CodeDomClientCodeGenerator.GeneratorName, "C#")]
    public class CSharpCodeDomClientCodeGenerator : CodeDomClientCodeGenerator
    {
    }
}
