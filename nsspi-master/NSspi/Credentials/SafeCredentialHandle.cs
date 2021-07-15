using System;
using System.Runtime.ConstrainedExecution;

namespace NSspi.Credentials
{
    /// <summary>
    /// Предоставляет управляемый дескриптор учетных данных SSPI. 
    /// </summary>
    public class SafeCredentialHandle : SafeSspiHandle
    {
        /// <summary>
        /// Инициализирует новый экземпляр  <see cref="SafeCredentialHandle"/> класса.
        /// </summary>
        public SafeCredentialHandle()
            : base()
        { }

        /// <summary>
        /// Освобождает ресурсы, удерживаемые дескриптором учетных данных. 
        /// </summary>
        /// <returns></returns>
        [ReliabilityContract( Consistency.WillNotCorruptState, Cer.Success )]
        protected override bool ReleaseHandle()
        {
            SecurityStatus status = CredentialNativeMethods.FreeCredentialsHandle(
                ref base.rawHandle
            );

            base.ReleaseHandle();

            return status == SecurityStatus.OK;
        }
    }
}