using System;
using System.Runtime.CompilerServices;

namespace NSspi.Credentials
{
    /// <summary>
    /// Получает дескриптор учетных данных пользователя, связанного с текущим процессом.
    /// </summary>
    public class CurrentCredential : Credential
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса CurrentCredential. 
        /// </summary>
        /// <param name="securityPackage">Пакет безопасности для получения дескриптора учетных данных</param>
        /// <param name="use">Способ использования учетных данных - входящий
        /// представляет серверы, исходящие представляют клиентов. </param>
        public CurrentCredential( string securityPackage, CredentialUse use ) :
            base( securityPackage )
        {
            Init( use );
        }

        private void Init( CredentialUse use )
        {
            string packageName;
            TimeStamp rawExpiry = new TimeStamp();
            SecurityStatus status = SecurityStatus.InternalError;

            // Скопируем вызов, поскольку this.SecurityPackage является свойством. 
            packageName = this.SecurityPackage;

            this.Handle = new SafeCredentialHandle();

            // Предложение finally - это фактическая ограниченная область. ВМ предварительно выделяет любое пространство стека,
            // выполняет любые выделения, необходимые для подготовки методов к выполнению, и откладывает любые
            // экземпляры «неуловимых» исключений (ThreadAbort, StackOverflow, OutOfMemory). 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { }
            finally
            {
                status = CredentialNativeMethods.AcquireCredentialsHandle(
                   null,
                   packageName,
                   use,
                   IntPtr.Zero,
                   IntPtr.Zero,
                   IntPtr.Zero,
                   IntPtr.Zero,
                   ref this.Handle.rawHandle,
                   ref rawExpiry
               );
            }

            if( status != SecurityStatus.OK )
            {
                throw new SSPIException( "Failed to call AcquireCredentialHandle", status );
            }

            this.Expiry = rawExpiry.ToDateTime();
        }
    }
}