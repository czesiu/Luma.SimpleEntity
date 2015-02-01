using Luma.SimpleEntity.Tools;

namespace Luma.SimpleEntity
{
    /// <summary>
    /// CodeDom-based client proxy code generator for VisualBasic.
    /// </summary>
    /// <remarks>
    /// This internal class exists solely to provide a unique export
    /// for the VisualBasic language.
    /// </remarks>
    [ClientCodeGenerator(CodeDomClientCodeGenerator.GeneratorName, "VB")]
    public class VisualBasicCodeDomClientCodeGenerator : CodeDomClientCodeGenerator
    {
    }
}
