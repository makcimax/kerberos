using System;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Threading;
using NSspi.Buffers;
using NSspi.Credentials;

namespace NSspi.Contexts
{
    /// <summary>
    /// Представляет контекст безопасности, используемый в роли сервера. 
    /// </summary>
    public class ServerContext : Context
    {
        private readonly ContextAttrib requestedAttribs;
        private readonly bool impersonationSetsThreadPrinciple;

        private ContextAttrib finalAttribs;
        private bool impersonating;

        /// <summary>
        /// Выполняет базовую инициализацию нового экземпляра класса ServerContext. В
        /// ServerContext не готов для обработки сообщений до тех пор, пока не будет создан контекст безопасности
        /// установлено с клиентом. 
        /// </summary>
        /// <param name="cred"></param>
        /// <param name="requestedAttribs"></param>
        /// <param name="impersonationSetsThreadPrinciple">
        /// Если true, свойство Thread.CurrentPrinciple будет изменено при успешном олицетворении. 
        /// </param>
        public ServerContext( Credential cred, ContextAttrib requestedAttribs, bool impersonationSetsThreadPrinciple = false ) : base( cred )
        {
            this.requestedAttribs = requestedAttribs;
            this.impersonationSetsThreadPrinciple = impersonationSetsThreadPrinciple;

            this.finalAttribs = ContextAttrib.Zero;

            this.impersonating = false;
            
            this.SupportsImpersonate = this.Credential.PackageInfo.Capabilities.HasFlag( SecPkgCapability.Impersonation );
        }

        /// <summary>
        /// Может ли сервер олицетворять аутентифицированного клиента. 
        /// </summary>
        /// <remarks>
        /// Это зависит от пакета безопасности, который использовался для создания учетных данных сервера и клиента. 
        /// </remarks>
        public bool SupportsImpersonate { get; private set; }

        /// <summary>
        /// Выполняет и продолжает цикл аутентификации. 
        /// </summary>
        /// <remarks>
        /// Этот метод выполняется итеративно, чтобы продолжить и завершить цикл аутентификации с помощью
        /// клиента. Каждый этап работает путем получения токена с одной стороны и передачи его другой стороне.
        /// который, в свою очередь, может генерировать новый токен. 
        ///
        /// Цикл обычно начинается и заканчивается клиентом. При первом вызове на клиенте
        /// токена сервера не существует, вместо него предоставляется null. Клиент возвращает свой статус, предоставляя
        /// его выходной токен для сервера. Сервер принимает токен клиентов в качестве входных данных и предоставляет
        /// токен в качестве вывода для отправки обратно клиенту. Этот цикл продолжается до тех пор, пока сервер и клиент
        /// оба обычно указывают SecurityStatus «ОК». 
        /// </remarks>
        /// <param name="clientToken">Последний токен, полученный от клиента. </param>
        /// <param name="nextToken">Следующий токен аутентификации сервера в цикле, который должен
        /// быть отправленным клиенту. </param>
        /// <returns>Сообщение о состоянии, указывающее на прогресс цикла аутентификации.
        /// Состояние «ОК» указывает, что цикл завершен с точки зрения серверов. Если nextToken
        /// не равно нулю, он должен быть отправлен клиенту.
        /// Статус «Продолжить» указывает, что выходной токен должен быть отправлен клиенту и
        /// следует ожидать ответа. </returns>
        public SecurityStatus AcceptToken( byte[] clientToken, out byte[] nextToken )
        {
            SecureBuffer clientBuffer;
            SecureBuffer outBuffer;

            SecurityStatus status;
            TimeStamp rawExpiry = new TimeStamp();

            SecureBufferAdapter clientAdapter;
            SecureBufferAdapter outAdapter;

            if( this.Disposed )
            {
                throw new ObjectDisposedException( "ServerContext" );
            }
            else if( this.Initialized )
            {
                throw new InvalidOperationException(
                    "Attempted to continue initialization of a ServerContext after initialization had completed."
                );
            }

            clientBuffer = new SecureBuffer( clientToken, BufferType.Token );

            outBuffer = new SecureBuffer(
                new byte[this.Credential.PackageInfo.MaxTokenLength],
                BufferType.Token
            );

            using( clientAdapter = new SecureBufferAdapter( clientBuffer ) )
            {
                using( outAdapter = new SecureBufferAdapter( outBuffer ) )
                {
                    if( this.ContextHandle.IsInvalid )
                    {
                        status = ContextNativeMethods.AcceptSecurityContext_1(
                            ref this.Credential.Handle.rawHandle,
                            IntPtr.Zero,
                            clientAdapter.Handle,
                            requestedAttribs,
                            SecureBufferDataRep.Network,
                            ref this.ContextHandle.rawHandle,
                            outAdapter.Handle,
                            ref this.finalAttribs,
                            ref rawExpiry
                        );
                    }
                    else
                    {
                        status = ContextNativeMethods.AcceptSecurityContext_2(
                            ref this.Credential.Handle.rawHandle,
                            ref this.ContextHandle.rawHandle,
                            clientAdapter.Handle,
                            requestedAttribs,
                            SecureBufferDataRep.Network,
                            ref this.ContextHandle.rawHandle,
                            outAdapter.Handle,
                            ref this.finalAttribs,
                            ref rawExpiry
                        );
                    }
                }
            }

            if( status == SecurityStatus.OK )
            {
                base.Initialize( rawExpiry.ToDateTime() );

                if( outBuffer.Length != 0 )
                {
                    nextToken = new byte[outBuffer.Length];
                    Array.Copy( outBuffer.Buffer, nextToken, nextToken.Length );
                }
                else
                {
                    nextToken = null;
                }
            }
            else if( status == SecurityStatus.ContinueNeeded )
            {
                nextToken = new byte[outBuffer.Length];
                Array.Copy( outBuffer.Buffer, nextToken, nextToken.Length );
            }
            else
            {
                throw new SSPIException( "Failed to call AcceptSecurityContext", status );
            }

            return status;
        }

        /// <summary>
        /// Изменяет контекст безопасности текущего потока, чтобы олицетворять пользователя клиента. 
        /// </summary>
        /// <remarks>
        /// Требуется, чтобы пакет безопасности предоставлял учетные данные сервера, а также
        /// учетные данные клиента, поддержка олицетворения. 
        ///
        /// В настоящее время только один поток может инициировать олицетворение для каждого контекста безопасности. Выдача себя за другое лицо может
        /// следим за потоками, созданными начальным потоком олицетворения. 
        /// </remarks>
        /// <returns>Дескриптор, фиксирующий время жизни олицетворения. Утилизируйте ручку, чтобы продолжить выдавать
        /// себя за другое лицо. Если дескриптор просочился, олицетворение автоматически вернется в
        /// недетерминированное время, когда дескриптор завершается сборщиком мусора. </returns>
        public ImpersonationHandle ImpersonateClient()
        {
            ImpersonationHandle handle;
            SecurityStatus status = SecurityStatus.InternalError;
            bool gotRef = false;

            if( this.Disposed )
            {
                throw new ObjectDisposedException( "ServerContext" );
            }
            else if( this.Initialized == false )
            {
                throw new InvalidOperationException(
                    "The server context has not been completely initialized."
                );
            }
            else if( impersonating )
            {
                throw new InvalidOperationException( "Cannot impersonate again while already impersonating." );
            }
            else if( this.SupportsImpersonate == false )
            {
                throw new InvalidOperationException(
                    "The ServerContext is using a security package that does not support impersonation."
                );
            }

            handle = new ImpersonationHandle( this );
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
                    status = ContextNativeMethods.ImpersonateSecurityContext(
                        ref this.ContextHandle.rawHandle
                    );

                    this.ContextHandle.DangerousRelease();

                    this.impersonating = status == SecurityStatus.OK;
                }
            }

            if( status == SecurityStatus.NoImpersonation )
            {
                throw new SSPIException( "Impersonation could not be performed.", status );
            }
            else if( status == SecurityStatus.Unsupported )
            {
                throw new SSPIException( "Impersonation is not supported by the security context's Security Support Provider.", status );
            }
            else if( status != SecurityStatus.OK )
            {
                throw new SSPIException( "Failed to impersonate the client", status );
            }

            if( this.impersonating && this.impersonationSetsThreadPrinciple )
            {
                Thread.CurrentPrincipal = new WindowsPrincipal( (WindowsIdentity)GetRemoteIdentity() );
            }

            return handle;
        }

        /// <summary>
        /// Вызывается дескриптором олицетворения, когда он выпускается, путем удаления или завершения. 
        /// </summary>
        internal void RevertImpersonate()
        {
            bool gotRef = false;

            if( impersonating == false || this.Disposed )
            {
                return;
            }

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
                    ContextNativeMethods.RevertSecurityContext(
                        ref this.ContextHandle.rawHandle
                    );

                    this.ContextHandle.DangerousRelease();

                    this.impersonating = false;
                }
            }
        }

        /// <summary>
        /// Освобождает все ресурсы, связанные с серверным контекстом. 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose( bool disposing )
        {
            // Мы были настроены, выдавая себя за другое лицо. Это означает, что потребитель, который в настоящее время держит
            // дескриптор олицетворения позволяет удалить или завершить контекст, пока дескриптор олицетворения установлен
            // Мы должны отменить олицетворение, чтобы восстановить поведение потока, поскольку однажды контекст
            // ичезнет и ничего не останется.
            //
            // Когда и если дескриптор олицетворения будет отключен / завершен, он увидит, что контекст уже был
            // disposed, будет считать, что мы уже вернулись, и ничего не будет делать. 

            if ( this.impersonating )
            {
                RevertImpersonate();
            }

            base.Dispose( disposing );
        }
    }
}