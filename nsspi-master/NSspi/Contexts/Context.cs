using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using NSspi.Buffers;
using NSspi.Credentials;

namespace NSspi.Contexts
{
    /// <summary>
    /// Представляет контекст безопасности и предоставляет общие функции, необходимые для 
    /// всех систем безопасности.
    /// </summary>
    /// <remarks>
    /// Этот класс является абстрактным и имеет защищенный конструктор и метод Initialize. Точная
    /// реализация инициализации предоставляется подклассами, которые могут выполнять инициализацию
    /// разными способами. 
    /// </remarks>
    public abstract class Context : IDisposable
    {
        /// <summary>
        /// Выполняет базовую инициализацию нового экземпляра класса Context.
        /// Инициализация не завершена, пока не будет установлено свойство ContextHandle
        /// и вызван метод Initialize.
        /// </summary>
        /// <param name="cred"></param>
        protected Context( Credential cred )
        {
            this.Credential = cred;

            this.ContextHandle = new SafeContextHandle();

            this.Disposed = false;
            this.Initialized = false;
        }

        /// <summary>
        /// Независимо от того, сформирован ли контекст полностью. 
        /// </summary>
        public bool Initialized { get; private set; }

        /// <summary>
        /// Учетные данные, используемые контекстом для аутентификации других участников. 
        /// </summary>
        protected Credential Credential { get; private set; }

        /// <summary>
        /// Ссылка на дескриптор контекста безопасности.
        /// </summary>
        public SafeContextHandle ContextHandle { get; private set; }

        /// <summary>
        /// Имя аутентифицирующего органа для контекста.
        /// </summary>
        public string AuthorityName
        {
            get
            {
                CheckLifecycle();
                return QueryContextString( ContextQueryAttrib.Authority );
            }
        }

        /// <summary>
        /// Имя пользователя для входа в систему, которое представляет контекст.
        /// </summary>
        public string ContextUserName
        {
            get
            {
                CheckLifecycle();
                return QueryContextString( ContextQueryAttrib.Names );
            }
        }

        /// <summary>
        /// Время в формате UTC, когда истекает срок действия контекста. 
        /// </summary>
        public DateTime Expiry { get; private set; }

        /// <summary>
        /// Был ли удален контекст. 
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        /// Отмечает контекст как завершивший процесс инициализации, т. е. обмен токенами аутентификации. 
        /// </summary>
        /// <param name="expiry">Дата и время истечения срока действия контекста.</param>
        protected void Initialize( DateTime expiry )
        {
            this.Expiry = expiry;
            this.Initialized = true;
        }

        /// <summary>
        /// Освобождает все ресурсы, связанные с контекстом.
        /// </summary>
        public void Dispose()
        {
            Dispose( true );
            GC.SuppressFinalize( this );
        }

        /// <summary>
        /// Освобождает ресурсы, связанные с контекстом. 
        /// </summary>
        /// <param name="disposing">Если true, освободит управляемые ресурсы, 
        /// иначе освободит только неуправляемые ресурсы. </param>
        protected virtual void Dispose( bool disposing )
        {
            if( this.Disposed ) { return; }

            if( disposing )
            {
                this.ContextHandle.Dispose();
            }

            this.Disposed = true;
        }

        /// <summary>
        /// Возвращает идентификатор удаленного объекта. 
        /// </summary>
        /// <returns></returns>
        public IIdentity GetRemoteIdentity()
        {
            IIdentity result = null;

            using( var tokenHandle = GetContextToken() )
            {
                bool gotRef = false;

                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    tokenHandle.DangerousAddRef( ref gotRef );
                }
                catch( Exception )
                {
                    if( gotRef )
                    {
                        tokenHandle.DangerousRelease();
                        gotRef = false;
                    }

                    throw;
                }
                finally
                {
                    try
                    {
                        result = new WindowsIdentity(
                            tokenHandle.DangerousGetHandle(),
                            this.Credential.SecurityPackage
                        );
                    }
                    finally
                    {
                        // Убедимся, что освободили дескриптор, иначе
                        // ошибка WindowsIdentity. 
                        tokenHandle.DangerousRelease();
                    }
                }
            }

            return result;
        }

        private SafeTokenHandle GetContextToken()
        {
            bool gotRef = false;
            SecurityStatus status = SecurityStatus.InternalError;
            SafeTokenHandle token;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                this.ContextHandle.DangerousAddRef( ref gotRef );
            }
            catch( Exception )
            {
                if( gotRef )
                {
                    this.ContextHandle.DangerousRelease();
                    gotRef = false;
                }

                throw;
            }
            finally
            {
                if( gotRef )
                {
                    try
                    {
                        status = ContextNativeMethods.QuerySecurityContextToken(
                            ref this.ContextHandle.rawHandle,
                            out token
                        );
                    }
                    finally
                    {
                        this.ContextHandle.DangerousRelease();
                    }
                }
                else
                {
                    token = null;
                }
            }

            if( status != SecurityStatus.OK )
            {
                throw new SSPIException( "Failed to query context token.", status );
            }

            return token;
        }

        /// <summary>
        /// Шифрует массив байтов, используя ключ сеанса контекста. 
        /// </summary>
        /// <remarks>
        /// Структура возвращаемых данных следующая:
        /// - 2 байта, беззнаковое целое число с прямым порядком байтов, указывающее длину размера конечного буфера
        /// - 4 байта, целое число без знака с прямым порядком байтов, указывающее длину размера буфера сообщения.
        /// - 2 байта, беззнаковое целое число с прямым порядком байтов, указывающее длину размера буфера заполнения для шифрования.
        /// - Буфер трейлера
        /// - Буфер сообщений
        /// - Буфер заполнения. 
        /// </remarks>
        /// <param name="input">Необработанное сообщение для шифрования. </param>
        /// <returns>Упакованное и зашифрованное сообщение. </returns>
        public byte[] Encrypt( byte[] input )
        {
            //Сообщение зашифровано в буфере, который мы предоставляем Win32 EncryptMessage.  
            SecPkgContext_Sizes sizes;

            SecureBuffer trailerBuffer;
            SecureBuffer dataBuffer;
            SecureBuffer paddingBuffer;
            SecureBufferAdapter adapter;

            SecurityStatus status = SecurityStatus.InvalidHandle;
            byte[] result;

            CheckLifecycle();

            sizes = QueryBufferSizes();

            trailerBuffer = new SecureBuffer( new byte[sizes.SecurityTrailer], BufferType.Token );
            dataBuffer = new SecureBuffer( new byte[input.Length], BufferType.Data );
            paddingBuffer = new SecureBuffer( new byte[sizes.BlockSize], BufferType.Padding );

            Array.Copy( input, dataBuffer.Buffer, input.Length );

            using( adapter = new SecureBufferAdapter( new[] { trailerBuffer, dataBuffer, paddingBuffer } ) )
            {
                status = ContextNativeMethods.SafeEncryptMessage(
                    this.ContextHandle,
                    0,
                    adapter,
                    0
                );
            }

            if( status != SecurityStatus.OK )
            {
                throw new SSPIException( "Failed to encrypt message", status );
            }

            int position = 0;

            // Достаточно места для размещения:
            // - 2 байта для размера буфера трейлера
            // - 4 байта для размера сообщения
            // - 2 байта для размера заполнения.
            // - зашифрованное сообщение
            result = new byte[2 + 4 + 2 + trailerBuffer.Length + dataBuffer.Length + paddingBuffer.Length];

            ByteWriter.WriteInt16_BE( (short)trailerBuffer.Length, result, position );
            position += 2;

            ByteWriter.WriteInt32_BE( dataBuffer.Length, result, position );
            position += 4;

            ByteWriter.WriteInt16_BE( (short)paddingBuffer.Length, result, position );
            position += 2;

            Array.Copy( trailerBuffer.Buffer, 0, result, position, trailerBuffer.Length );
            position += trailerBuffer.Length;

            Array.Copy( dataBuffer.Buffer, 0, result, position, dataBuffer.Length );
            position += dataBuffer.Length;

            Array.Copy( paddingBuffer.Buffer, 0, result, position, paddingBuffer.Length );
            position += paddingBuffer.Length;

            return result;
        }

        /// <summary>
        /// Расшифровывает ранее зашифрованное сообщение. 
        /// </summary>
        /// <remarks>
        /// Ожидаемый формат буфера следующий:
        /// - 2 байта, беззнаковое целое число с прямым порядком байтов, указывающее длину размера конечного буфера
        /// - 4 байта, целое число без знака с прямым порядком байтов, указывающее длину размера буфера сообщения.
        /// - 2 байта, беззнаковое целое число с прямым порядком байтов, указывающее длину размера буфера заполнения для шифрования.
        /// - Буфер трейлера
        /// - Буфер сообщений
        /// - Буфер заполнения. 
        /// </remarks>
        /// <param name="input">Упакованные и зашифрованные данные. </param>
        /// <returns>Исходное текстовое сообщение. </returns>
        public byte[] Decrypt( byte[] input )
        {
            SecPkgContext_Sizes sizes;

            SecureBuffer trailerBuffer;
            SecureBuffer dataBuffer;
            SecureBuffer paddingBuffer;
            SecureBufferAdapter adapter;

            SecurityStatus status;
            byte[] result = null;
            int remaining;
            int position;

            int trailerLength;
            int dataLength;
            int paddingLength;

            CheckLifecycle();

            sizes = QueryBufferSizes();

            // Эта проверка обязательна
            if ( input.Length < 2 + 4 + 2 + sizes.SecurityTrailer )
            {
                throw new ArgumentException( "Buffer is too small to possibly contain an encrypted message" );
            }

            position = 0;

            trailerLength = ByteWriter.ReadInt16_BE( input, position );
            position += 2;

            dataLength = ByteWriter.ReadInt32_BE( input, position );
            position += 4;

            paddingLength = ByteWriter.ReadInt16_BE( input, position );
            position += 2;

            if( trailerLength + dataLength + paddingLength + 2 + 4 + 2 > input.Length )
            {
                throw new ArgumentException( "The buffer contains invalid data - the embedded length data does not add up." );
            }

            trailerBuffer = new SecureBuffer( new byte[trailerLength], BufferType.Token );
            dataBuffer = new SecureBuffer( new byte[dataLength], BufferType.Data );
            paddingBuffer = new SecureBuffer( new byte[paddingLength], BufferType.Padding );

            remaining = input.Length - position;

            if( trailerBuffer.Length <= remaining )
            {
                Array.Copy( input, position, trailerBuffer.Buffer, 0, trailerBuffer.Length );
                position += trailerBuffer.Length;
                remaining -= trailerBuffer.Length;
            }
            else
            {
                throw new ArgumentException( "Input is missing data - it is not long enough to contain a fully encrypted message" );
            }

            if( dataBuffer.Length <= remaining )
            {
                Array.Copy( input, position, dataBuffer.Buffer, 0, dataBuffer.Length );
                position += dataBuffer.Length;
                remaining -= dataBuffer.Length;
            }
            else
            {
                throw new ArgumentException( "Input is missing data - it is not long enough to contain a fully encrypted message" );
            }

            if( paddingBuffer.Length <= remaining )
            {
                Array.Copy( input, position, paddingBuffer.Buffer, 0, paddingBuffer.Length );
            }
            // иначе не было бы отступа.

            using ( adapter = new SecureBufferAdapter( new[] { trailerBuffer, dataBuffer, paddingBuffer } ) )
            {
                status = ContextNativeMethods.SafeDecryptMessage(
                    this.ContextHandle,
                    0,
                    adapter,
                    0
                );
            }

            if( status != SecurityStatus.OK )
            {
                throw new SSPIException( "Failed to encrypt message", status );
            }

            result = new byte[dataBuffer.Length];
            Array.Copy( dataBuffer.Buffer, 0, result, 0, dataBuffer.Length );

            return result;
        }

        /// <summary>
        /// Подписывает сообщение, используя ключ сеанса контекста. 
        /// </summary>
        /// <remarks>
        /// Структура возвращаемого буфера следующая:
        /// - 4 байта, целое число без знака с прямым порядком байтов, указывающее длину текстового сообщения
        /// - 2 байта, целое число без знака с прямым порядком байтов, указывающее длину сигнатуры
        /// - Текстовое сообщение
        /// </remarks>
        /// <param name="message"></param>
        /// <returns></returns>
        public byte[] MakeSignature( byte[] message )
        {
            SecurityStatus status = SecurityStatus.InternalError;

            SecPkgContext_Sizes sizes;
            SecureBuffer dataBuffer;
            SecureBuffer signatureBuffer;
            SecureBufferAdapter adapter;

            CheckLifecycle();

            sizes = QueryBufferSizes();

            dataBuffer = new SecureBuffer( new byte[message.Length], BufferType.Data );
            signatureBuffer = new SecureBuffer( new byte[sizes.MaxSignature], BufferType.Token );

            Array.Copy( message, dataBuffer.Buffer, message.Length );

            using( adapter = new SecureBufferAdapter( new[] { dataBuffer, signatureBuffer } ) )
            {
                status = ContextNativeMethods.SafeMakeSignature(
                    this.ContextHandle,
                    0,
                    adapter,
                    0
                );
            }

            if( status != SecurityStatus.OK )
            {
                throw new SSPIException( "Failed to create message signature.", status );
            }

            byte[] outMessage;
            int position = 0;

            // Достаточно места для
            // - исходная длина сообщения (4 байта)
            // - длина подписи (2 байта)
            //  - Исходное сообщение
            //  - подпись 

            outMessage = new byte[4 + 2 + dataBuffer.Length + signatureBuffer.Length];

            ByteWriter.WriteInt32_BE( dataBuffer.Length, outMessage, position );
            position += 4;

            ByteWriter.WriteInt16_BE( (Int16)signatureBuffer.Length, outMessage, position );
            position += 2;

            Array.Copy( dataBuffer.Buffer, 0, outMessage, position, dataBuffer.Length );
            position += dataBuffer.Length;

            Array.Copy( signatureBuffer.Buffer, 0, outMessage, position, signatureBuffer.Length );
            position += signatureBuffer.Length;

            return outMessage;
        }

        /// <summary>
        /// Возвращает ключ сеанса из контекста или null в случае ошибки. 
        /// </summary>
        /// <remarks>
        /// Ключи сеанса иногда необходимы для других целей или функций HMAC. Эта функция
        /// запустит QueryAttribute для получения структуры ключа сеанса, а также прочитает и вернет ключ из
        /// эта структура. 
        /// </remarks>
        /// <returns>byte[] с данными сеансового ключа или null в случае сбоя </returns>
        public byte[] QuerySessionKey()
        {
            SecurityStatus status;

            byte[] SessionKey = null;

            status = ContextNativeMethods.SafeQueryContextAttribute(
                this.ContextHandle,
                ContextQueryAttrib.SessionKey,
                ref SessionKey
            );

            if( status != SecurityStatus.OK )
            {
                throw new SSPIException( "Failed to query session key.", status );
            }

            return SessionKey;
        }

        /// <summary>
        /// Проверяет подпись подписанного сообщения 
        /// </summary>
        /// <remarks>
        /// Ожидаемая структура буфера подписанного сообщения следующая:
        /// - 4 байта, целое число без знака в формате big endian, указывающее длину текстового сообщения
        /// - 2 байта, целое число без знака в формате big endian, указывающее длину сигнатуры
        /// - Текстовое сообщение
        /// - Подпись сообщения. 
        /// </remarks>
        /// <param name="signedMessage">Упакованное подписанное сообщение. </param>
        /// <param name="origMessage">Извлеченное исходное сообщение. </param>
        /// <returns>Истина, если у сообщения есть действительная подпись, в противном случае - ложь.</returns>
        public bool VerifySignature( byte[] signedMessage, out byte[] origMessage )
        {
            SecurityStatus status = SecurityStatus.InternalError;

            SecPkgContext_Sizes sizes;
            SecureBuffer dataBuffer;
            SecureBuffer signatureBuffer;
            SecureBufferAdapter adapter;

            CheckLifecycle();

            sizes = QueryBufferSizes();

            if( signedMessage.Length < 2 + 4 + sizes.MaxSignature )
            {
                throw new ArgumentException( "Input message is too small to possibly fit a valid message" );
            }

            int position = 0;
            int messageLen;
            int sigLen;

            messageLen = ByteWriter.ReadInt32_BE( signedMessage, 0 );
            position += 4;

            sigLen = ByteWriter.ReadInt16_BE( signedMessage, position );
            position += 2;

            if( messageLen + sigLen + 2 + 4 > signedMessage.Length )
            {
                throw new ArgumentException( "The buffer contains invalid data - the embedded length data does not add up." );
            }

            dataBuffer = new SecureBuffer( new byte[messageLen], BufferType.Data );
            Array.Copy( signedMessage, position, dataBuffer.Buffer, 0, messageLen );
            position += messageLen;

            signatureBuffer = new SecureBuffer( new byte[sigLen], BufferType.Token );
            Array.Copy( signedMessage, position, signatureBuffer.Buffer, 0, sigLen );
            position += sigLen;

            using( adapter = new SecureBufferAdapter( new[] { dataBuffer, signatureBuffer } ) )
            {
                status = ContextNativeMethods.SafeVerifySignature(
                    this.ContextHandle,
                    0,
                    adapter,
                    0
                );
            }

            if( status == SecurityStatus.OK )
            {
                origMessage = dataBuffer.Buffer;
                return true;
            }
            else if( status == SecurityStatus.MessageAltered ||
                      status == SecurityStatus.OutOfSequence )
            {
                origMessage = null;
                return false;
            }
            else
            {
                throw new SSPIException( "Failed to determine the veracity of a signed message.", status );
            }
        }

        /// <summary>
        /// Запрашивает ожидания пакетов безопасности в отношении 
        /// размеров буфера сообщений / токенов / подписей / заполнения. 
        /// </summary>
        /// <returns></returns>
        private SecPkgContext_Sizes QueryBufferSizes()
        {
            SecPkgContext_Sizes sizes = new SecPkgContext_Sizes();
            SecurityStatus status = SecurityStatus.InternalError;
            bool gotRef = false;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                this.ContextHandle.DangerousAddRef( ref gotRef );
            }
            catch( Exception )
            {
                if( gotRef )
                {
                    this.ContextHandle.DangerousRelease();
                    gotRef = false;
                }

                throw;
            }
            finally
            {
                if( gotRef )
                {
                    status = ContextNativeMethods.QueryContextAttributes_Sizes(
                        ref this.ContextHandle.rawHandle,
                        ContextQueryAttrib.Sizes,
                        ref sizes
                    );
                    this.ContextHandle.DangerousRelease();
                }
            }

            if( status != SecurityStatus.OK )
            {
                throw new SSPIException( "Failed to query context buffer size attributes", status );
            }

            return sizes;
        }

        /// <summary>
        /// Запрашивает строковый атрибут контекста по названному атрибуту.
        /// </summary>
        /// <param name="attrib">Атрибут со строковым значением для запроса.</param>
        /// <returns></returns>
        private string QueryContextString( ContextQueryAttrib attrib )
        {
            SecPkgContext_String stringAttrib;
            SecurityStatus status = SecurityStatus.InternalError;
            string result = null;
            bool gotRef = false;

            if( attrib != ContextQueryAttrib.Names && attrib != ContextQueryAttrib.Authority )
            {
                throw new InvalidOperationException( "QueryContextString can only be used to query context Name and Authority attributes" );
            }

            stringAttrib = new SecPkgContext_String();

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                this.ContextHandle.DangerousAddRef( ref gotRef );
            }
            catch( Exception )
            {
                if( gotRef )
                {
                    this.ContextHandle.DangerousRelease();
                    gotRef = false;
                }
                throw;
            }
            finally
            {
                if( gotRef )
                {
                    status = ContextNativeMethods.QueryContextAttributes_String(
                        ref this.ContextHandle.rawHandle,
                        attrib,
                        ref stringAttrib
                    );

                    this.ContextHandle.DangerousRelease();

                    if( status == SecurityStatus.OK )
                    {
                        result = Marshal.PtrToStringUni( stringAttrib.StringResult );
                        ContextNativeMethods.FreeContextBuffer( stringAttrib.StringResult );
                    }
                }
            }

            if( status == SecurityStatus.Unsupported )
            {
                return null;
            }
            else if( status != SecurityStatus.OK )
            {
                throw new SSPIException( "Failed to query the context's associated user name", status );
            }

            return result;
        }

        /// <summary>
        /// Проверяет, подходит ли состояние жизненного цикла объекта (инициализация / размещение) для использования
        /// объект. 
        /// </summary>
        private void CheckLifecycle()
        {
            if( this.Initialized == false )
            {
                throw new InvalidOperationException( "The context is not yet fully formed." );
            }
            else if( this.Disposed )
            {
                throw new ObjectDisposedException( "Context" );
            }
        }
    }
}