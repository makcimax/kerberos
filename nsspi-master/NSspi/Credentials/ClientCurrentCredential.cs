using System;

namespace NSspi.Credentials
{
    /// <summary>
    /// Представляет дескриптор учетных данных пользователя, запускающего текущий процесс, который будет использоваться для
    /// аутентифицируемся как клиент. 
    /// </summary>
    public class ClientCurrentCredential : CurrentCredential
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса ClientCurrentCredential. 
        /// </summary>
        /// <param name="package">Пакет безопасности, от которого требуется получить дескриптор учетных данных. </param>
        public ClientCurrentCredential( string package )
            : base( package, CredentialUse.Outbound )
        {
        }
    }
}