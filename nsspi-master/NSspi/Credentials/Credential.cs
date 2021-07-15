using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NSspi.Credentials
{
    /// <summary>
    /// Предоставляет доступ к уже существующим учетным данным принципа безопасности. 
    /// </summary>
    public class Credential : IDisposable
    {
        /// <summary>
        /// Имя пакета безопасности, который контролирует учетные данные. 
        /// </summary>
        private readonly string securityPackage;

        /// <summary>
        /// Были ли удалены учетные данные. 
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Безопасный дескриптор учетных данных. 
        /// </summary>
        private SafeCredentialHandle safeCredHandle;

        /// <summary>
        /// Время в формате UTC когда истекает срок действия учетных данных. 
        /// </summary>
        private DateTime expiry;

        /// <summary>
        /// Инициализирует новый экземпляр класса Credential. 
        /// </summary>
        /// <param name="package">Пакет безопасности, из которого нужно получить учетные данные. </param>
        public Credential( string package )
        {
            this.securityPackage = package;

            this.disposed = false;
            this.expiry = DateTime.MinValue;
            this.PackageInfo = PackageSupport.GetPackageCapabilities( this.SecurityPackage );
        }

        /// <summary>
        /// Получает метаданные для пакета безопасности, связанного с учетными данными. 
        /// </summary>
        public SecPkgInfo PackageInfo { get; private set; }

        /// <summary>
        /// Получает имя пакета безопасности, которому принадлежат учетные данные. 
        /// </summary>
        public string SecurityPackage
        {
            get
            {
                CheckLifecycle();

                return this.securityPackage;
            }
        }

        /// <summary>
        /// Возвращает имя принципа пользователя для учетных данных. В зависимости от базовой безопасности
        /// пакет, используемый учетными данными, он может не совпадать с именем входа нижнего уровня
        /// для пользователя. 
        /// </summary>
        public string PrincipleName
        {
            get
            {
                QueryNameAttribCarrier carrier;
                SecurityStatus status;
                string name = null;
                bool gotRef = false;

                CheckLifecycle();

                status = SecurityStatus.InternalError;
                carrier = new QueryNameAttribCarrier();

                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    this.safeCredHandle.DangerousAddRef( ref gotRef );
                }
                catch( Exception )
                {
                    if( gotRef == true )
                    {
                        this.safeCredHandle.DangerousRelease();
                        gotRef = false;
                    }
                    throw;
                }
                finally
                {
                    if( gotRef )
                    {
                        status = CredentialNativeMethods.QueryCredentialsAttribute_Name(
                            ref this.safeCredHandle.rawHandle,
                            CredentialQueryAttrib.Names,
                            ref carrier
                        );

                        this.safeCredHandle.DangerousRelease();

                        if( status == SecurityStatus.OK && carrier.Name != IntPtr.Zero )
                        {
                            try
                            {
                                name = Marshal.PtrToStringUni( carrier.Name );
                            }
                            finally
                            {
                                NativeMethods.FreeContextBuffer( carrier.Name );
                            }
                        }
                    }
                }

                if( status.IsError() )
                {
                    throw new SSPIException( "Failed to query credential name", status );
                }

                return name;
            }
        }

        /// <summary>
        /// Получает время в формате UTC, когда истекает срок действия учетных данных. 
        /// </summary>
        public DateTime Expiry
        {
            get
            {
                CheckLifecycle();

                return this.expiry;
            }

            protected set
            {
                CheckLifecycle();

                this.expiry = value;
            }
        }

        /// <summary>
        /// Gets a handle to the credential.
        /// </summary>
        public SafeCredentialHandle Handle
        {
            get
            {
                CheckLifecycle();

                return this.safeCredHandle;
            }

            protected set
            {
                CheckLifecycle();

                this.safeCredHandle = value;
            }
        }

        /// <summary>
        /// Освобождает все ресурсы, связанные с учетными данными. 
        /// </summary>
        public void Dispose()
        {
            Dispose( true );
            GC.SuppressFinalize( this );
        }

        /// <summary>
        /// Освобождает все ресурсы, связанные с учетными данными. 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose( bool disposing )
        {
            if( this.disposed == false )
            {
                if( disposing )
                {
                    this.safeCredHandle.Dispose();
                }

                this.disposed = true;
            }
        }

        private void CheckLifecycle()
        {
            if( this.disposed )
            {
                throw new ObjectDisposedException( "Credential" );
            }
        }
    }
}