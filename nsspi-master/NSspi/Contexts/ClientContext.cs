using System;
using NSspi.Buffers;
using NSspi.Credentials;

namespace NSspi.Contexts
{
    /// <summary>
    /// Предоставляет средства для создания общего контекста безопасности
    /// с сервером, а также для шифрования, дешифрования, подписи и проверки сообщений, 
    /// отправляемых и исходящих от сервера. 
    /// </summary>
    /// <remarks>
    // Клиент и сервер устанавливают общий контекст безопасности, обмениваясь токенами аутентификации. Если один раз
    /// общий контекст установлен, клиент и сервер могут передавать друг другу сообщения в зашифрованном виде,
    /// подписанные с использованием установленных параметров общего контекста. 
    /// </remarks>
    public class ClientContext : Context
    {
        private ContextAttrib requestedAttribs;
        private ContextAttrib finalAttribs;
        private string serverPrinc;

        /// <summary>
        /// Инициализирует новый экземпляр класса ClientContext. Контекст не полностью инициализирован и 
        /// пригоден для использования до завершения цикла аутентификации. 
        /// </summary>
        /// <param name="cred">Учетные данные безопасности для аутентификации</param>
        /// <param name="serverPrinc">Основное имя сервера для подключения или null для любого</param>
        /// <param name="requestedAttribs">Запрошенные атрибуты, описывающие желаемые свойства
        /// контекста, как только он будет установлен. Если не удается установить контекст, удовлетворяющий указанным
        /// свойства, инициализация контекста прерывается </param>
        public ClientContext( Credential cred, string serverPrinc, ContextAttrib requestedAttribs )
            : base( cred )
        {
            this.serverPrinc = serverPrinc;
            this.requestedAttribs = requestedAttribs;
        }

        /// <summary>
        /// Выполняет и продолжает цикл аутентификации. 
        /// </summary>
        /// <remarks>
        /// Этот метод выполняется итеративно для запуска, продолжения и завершения цикла аутентификации с помощью
        /// сервера. Каждый этап работает путем получения токена с одной стороны и передачи его другой стороне.
        /// который, в свою очередь, может генерировать новый токен. 
        ///
        /// Цикл обычно начинается и заканчивается клиентом. При первом вызове на клиенте
        /// токена сервера не существует, вместо него предоставляется null. Клиент возвращает свой статус, предоставляя
        /// его выходной токен для сервера. Сервер принимает токен клиентов в качестве входных данных и предоставляет
        /// токен в качестве вывода для отправки обратно клиенту. Этот цикл продолжается до тех пор, пока сервер и клиент
        /// оба обычно указывают SecurityStatus «ОК». 
        /// </remarks>
        /// <param name="serverToken">Последний токен, полученный с сервера, или null, если начинается
        /// цикл аутентификации. </param>
        /// <param name="outToken">Следующий токен аутентификации клиентов в цикле аутентификации</param>
        /// <returns>Сообщение о состоянии, указывающее на прогресс цикла аутентификации.
        /// Статус «ОК» указывает, что цикл завершен с точки зрения клиента. Если outToken
        /// не равно нулю, он должен быть отправлен на сервер.
        /// Статус «Продолжить» указывает, что выходной токен должен быть отправлен на сервер и
        /// следует ожидать ответа. </returns>
        public SecurityStatus Init( byte[] serverToken, out byte[] outToken )
        {
            TimeStamp rawExpiry = new TimeStamp();

            SecurityStatus status;

            SecureBuffer outTokenBuffer;
            SecureBufferAdapter outAdapter;

            SecureBuffer serverBuffer;
            SecureBufferAdapter serverAdapter;

            if( this.Disposed )
            {
                throw new ObjectDisposedException( "ClientContext" );
            }
            else if( ( serverToken != null ) && ( this.ContextHandle.IsInvalid ) )
            {
                throw new InvalidOperationException( "Out-of-order usage detected - have a server token, but no previous client token had been created." );
            }
            else if( ( serverToken == null ) && ( this.ContextHandle.IsInvalid == false ) )
            {
                throw new InvalidOperationException( "Must provide the server's response when continuing the init process." );
            }

            // Пакет безопасности сообщает нам, насколько большим будет его
            // самый большой токен. Мы выделим буфер
            // такого размера, и это скажет нам, сколько он был использован.
            outTokenBuffer = new SecureBuffer(
                new byte[this.Credential.PackageInfo.MaxTokenLength],
                BufferType.Token
            );

            serverBuffer = null;
            if( serverToken != null )
            {
                serverBuffer = new SecureBuffer( serverToken, BufferType.Token );
            }

            //Некоторые примечания по дескрипторам и вызову InitializeSecurityContext
            // - В первый раз параметр phContext («старый» дескриптор) является нулевым указателем на то, что
            // будет RawSspiHandle, чтобы указать, что это первый вызов.
            // phNewContext - это указатель (ссылка) на структуру RawSspiHandle, где записывать
            // новые значения дескриптора.
            // - В следующий раз, когда вы вызовете ISC, он получит указатель на дескриптор, который он дал вам в последний раз в phContext,
            // и принимает указатель на то, куда следует записывать значения нового дескриптора в phNewContext.
            // - После первого раза вы можете указать один и тот же дескриптор для обоих параметров. Из MSDN:
            // "При втором вызове phNewContext может быть таким же, как дескриптор, указанный в phContext
            // параметр. "
            // Он перезапишет переданный вами дескриптор новым значением дескриптора.
            // - Все структуры дескрипторов на самом деле являются * двумя * переменными-указателями, например, 64-битные на 32-битных
            // Windows, 128 бит в 64-битной Windows.
            // - Итак, в конце на 64-битной машине мы передаем 64-битное значение (указатель на структуру), которое
            // указывает на 128 бит памяти (саму структуру) для записи номеров дескрипторов. 
            using ( outAdapter = new SecureBufferAdapter( outTokenBuffer ) )
            {
                if( this.ContextHandle.IsInvalid )
                {
                    status = ContextNativeMethods.InitializeSecurityContext_1(
                        ref this.Credential.Handle.rawHandle,
                        IntPtr.Zero,
                        this.serverPrinc,
                        this.requestedAttribs,
                        0,
                        SecureBufferDataRep.Network,
                        IntPtr.Zero,
                        0,
                        ref this.ContextHandle.rawHandle,
                        outAdapter.Handle,
                        ref this.finalAttribs,
                        ref rawExpiry
                    );
                }
                else
                {
                    using( serverAdapter = new SecureBufferAdapter( serverBuffer ) )
                    {
                        status = ContextNativeMethods.InitializeSecurityContext_2(
                            ref this.Credential.Handle.rawHandle,
                            ref this.ContextHandle.rawHandle,
                            this.serverPrinc,
                            this.requestedAttribs,
                            0,
                            SecureBufferDataRep.Network,
                            serverAdapter.Handle,
                            0,
                            ref this.ContextHandle.rawHandle,
                            outAdapter.Handle,
                            ref this.finalAttribs,
                            ref rawExpiry
                        );
                    }
                }
            }

            if( status.IsError() == false )
            {
                if( status == SecurityStatus.OK )
                {
                    base.Initialize( rawExpiry.ToDateTime() );
                }

                outToken = null;

                if( outTokenBuffer.Length != 0 )
                {
                    outToken = new byte[outTokenBuffer.Length];
                    Array.Copy( outTokenBuffer.Buffer, outToken, outToken.Length );
                }
            }
            else
            {
                throw new SSPIException( "Failed to invoke InitializeSecurityContext for a client", status );
            }

            return status;
        }
    }
}