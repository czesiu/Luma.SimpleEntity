// These interfaces serve as an extension to the BCL's SymbolStore interfaces.
namespace Luma.SimpleEntity.Tools.Pdb.SymStore 
{
    using System;
    using System.Runtime.InteropServices;

    // Interface does not need to be marked with the serializable attribute
    // Interface is returned by ISymbolScope2.GetConstants() so must be public
    [ComVisible(false)]
    internal interface ISymbolConstant
    {
        String GetName();

        Object GetValue();

        byte[] GetSignature();
    }
}
