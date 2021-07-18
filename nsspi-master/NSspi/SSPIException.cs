using System;
using System.Runtime.Serialization;

namespace NSspi
{
    /// <summary>
    /// Исключение, которое выдается при возникновении проблемы при использовании системы SSPI. 
    /// </summary>
    [Serializable]
    public class SSPIException : Exception
    {
        private SecurityStatus errorCode;
        private string message;

        /// <summary>
        /// Инициализирует новый экземпляр класса SSPIException с заданным сообщением и статусом. 
        /// </summary>
        /// <param name="message">Сообщение, объясняющее, какая часть системы вышла из строя. </param>
        /// <param name="errorCode">Код ошибки, наблюдаемый во время сбоя.</param>
        public SSPIException( string message, SecurityStatus errorCode )
        {
            this.message = message;
            this.errorCode = errorCode;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса SSPIException из данных сериализации.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected SSPIException( SerializationInfo info, StreamingContext context )
            : base( info, context )
        {
            this.message = info.GetString( "message" );
            this.errorCode = (SecurityStatus)info.GetUInt32( "errorCode" );
        }

        /// <summary>
        /// Сериализует исключение. 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public override void GetObjectData( SerializationInfo info, StreamingContext context )
        {
            base.GetObjectData( info, context );

            info.AddValue( "message", this.message );
            info.AddValue( "errorCode", this.errorCode );
        }

        /// <summary>
        /// Код ошибки, обнаруженный во время вызова SSPI. 
        /// </summary>
        public SecurityStatus ErrorCode
        {
            get
            {
                return this.errorCode;
            }
        }

        /// <summary>
        /// Сообщение, удобочитаемое для чтения, с указанием характера исключения. 
        /// </summary>
        public override string Message
        {
            get
            {
                return string.Format(
                    "{0}. Error Code = '0x{1:X}' - \"{2}\".",
                    this.message,
                    this.errorCode,
                    EnumMgr.ToText( this.errorCode )
                );
            }
        }
    }
}