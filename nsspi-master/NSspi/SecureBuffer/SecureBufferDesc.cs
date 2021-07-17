using System;
using System.Runtime.InteropServices;

namespace NSspi.Buffers
{
    /// <summary>
    /// Представляет собственный макет дескриптора безопасного буфера, который предоставляется напрямую
    /// к собственным вызовам API. 
    /// </summary>
    [StructLayout( LayoutKind.Sequential )]
    internal struct SecureBufferDescInternal
    {
        /// <summary>
        /// Версия буферной структуры. 
        /// </summary>
        public int Version;

        /// <summary>
        /// Количество буферов, представленных этим дескриптором. 
        /// </summary>
        public int NumBuffers;

        /// <summary>
        /// Указатель на массив буферов, где каждый буфер представляет собой byte []. 
        /// </summary>
        public IntPtr Buffers;

        /// <summary>
        /// Указывает версию структуры буфера, поддерживаемую этой структурой. Всегда 0. 
        /// </summary>
        public const int ApiVersion = 0;
    }
}