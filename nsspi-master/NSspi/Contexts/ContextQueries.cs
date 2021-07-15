using System;
using System.Runtime.InteropServices;

namespace NSspi.Contexts
{
    /// <summary>
    /// Сохраняет результат контекстного запроса для размеров контекстного буфера. 
    /// </summary>
    [StructLayout( LayoutKind.Sequential )]
    internal struct SecPkgContext_Sizes
    {
        public int MaxToken;
        public int MaxSignature;
        public int BlockSize;
        public int SecurityTrailer;
    }

    /// <summary>
    /// Сохраняет результат контекстного запроса для атрибута контекста со строковым значением. 
    /// </summary>
    [StructLayout( LayoutKind.Sequential )]
    internal struct SecPkgContext_String
    {
        public IntPtr StringResult;
    }
}