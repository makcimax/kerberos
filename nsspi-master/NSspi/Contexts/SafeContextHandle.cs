using System;
using System.Runtime.ConstrainedExecution;

namespace NSspi.Contexts
{
    /// <summary>
    /// Захватывает дескриптор неуправляемого контекста безопасности. 
    /// </summary>
    public class SafeContextHandle : SafeSspiHandle
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref = "SafeContextHandle" />. 
        /// </summary>
        public SafeContextHandle()
            : base()
        { }

        /// <summary>
        /// Освобождает безопасный дескриптор контекста. 
        /// </summary>
        /// <returns></returns>
        [ReliabilityContract( Consistency.WillNotCorruptState, Cer.Success )]
        protected override bool ReleaseHandle()
        {
            SecurityStatus status = ContextNativeMethods.DeleteSecurityContext(
                ref base.rawHandle
            );

            base.ReleaseHandle();

            return status == SecurityStatus.OK;
        }
    }
}