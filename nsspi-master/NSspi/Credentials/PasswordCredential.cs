using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace NSspi.Credentials
{
    /// <summary>
    /// Представляет учетные данные, полученные путем предоставления имени пользователя, пароля и домена. 
    /// </summary>
    public class PasswordCredential : Credential
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса PasswordCredential. 
        /// </summary>
        /// <remarks>
        /// Можно получить действительный дескриптор учетных данных, которые не обеспечивают действительный
        /// комбинация имени пользователя и пароля. Имя пользователя и пароль не проверяются до тех пор, пока
        /// цикл аутентификации не начинается. 
        /// </remarks>
        /// <param name="domain">Домен для аутентификации. </param>
        /// <param name="username">Имя пользователя для аутентификации. </param>
        /// <param name="password">Пароль пользователя. </param>
        /// <param name="secPackage">Пакет безопасности SSPI, для которого нужно создать учетные данные. </param>
        /// <param name="use">
        /// Укажите входящий при получении учетных данных для сервера; исходящий для клиента. 
        /// </param>
        public PasswordCredential( string domain, string username, string password, string secPackage, CredentialUse use ) 
            : base( secPackage )
        {
            NativeAuthData authData = new NativeAuthData( domain, username, password, NativeAuthDataFlag.Unicode );

            Init( authData, secPackage, use );
        }

        private void Init( NativeAuthData authData, string secPackage, CredentialUse use )
        {
            string packageName;
            TimeStamp rawExpiry = new TimeStamp();
            SecurityStatus status = SecurityStatus.InternalError;

            // Скопируйте вызов, поскольку this.SecurityPackage является свойством. 
            packageName = this.SecurityPackage;

            this.Handle = new SafeCredentialHandle();


            // Предложение finally - это фактическая ограниченная область. ВМ предварительно выделяет любое пространство стека,
            // выполняет любые выделения, необходимые для подготовки методов к выполнению, и откладывает любые
            // экземпляры «неуловимых» исключений (ThreadAbort, StackOverflow, OutOfMemory). 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { }
            finally
            {
                status = CredentialNativeMethods.AcquireCredentialsHandle_AuthData(
                   null,
                   packageName,
                   use,
                   IntPtr.Zero,
                   ref authData,
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
