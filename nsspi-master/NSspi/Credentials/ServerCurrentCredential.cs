using System;

namespace NSspi.Credentials
{
    /// <summary>
    /// Представляет дескриптор учетных данных пользователя, запускающего текущий процесс, который будет использоваться для
    /// аутентификации как сервер. 
    /// </summary>
    public class ServerCurrentCredential : CurrentCredential
    {
        /// <summary>
        ///Инициализирует новый экземпляр класса ServerCredential, получая учетные данные от
        /// контекст безопасности текущего потока. 
        /// </summary>
        /// <param name="package">Имя пакета безопасности, от которого требуется получить учетные данные. </param>
        public ServerCurrentCredential( string package )
            : base( package, CredentialUse.Inbound )
        {
        }
    }
}