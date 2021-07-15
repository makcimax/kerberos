using System;
using System.Security.Principal;
using System.Threading;

namespace NSspi.Contexts
{
    /// <summary>
    /// Представляет олицетворение, выполняемое на сервере от имени клиента. 
    /// </summary>
    /// <remarks>
    /// Дескриптор контролирует время жизни олицетворения и отменит олицетворение.
    /// если он удален, или если он завершен, то есть в результате утечки и сбора мусора. 
    ///
    /// Если дескриптор случайно просочился во время выполнения операций от имени пользователя,
    /// олицетворение может быть отменено в любой момент, возможно, во время этих операций.
    /// Это может привести к выполнению операций в контексте безопасности сервера,
    /// потенциально может привести к уязвимостям безопасности. 
    /// </remarks>
    public class ImpersonationHandle : IDisposable
    {
        private readonly ServerContext server;

        private bool disposed;

        /// <summary>
        /// Инициализирует новый экземпляр ImpersonationHandle. Не выполняет олицетворение. 
        /// </summary>
        /// <param name="server">Контекст сервера, выполняющий олицетворение. </param>
        internal ImpersonationHandle( ServerContext server )
        {
            this.server = server;
            this.disposed = false;
        }

        /// <summary>
        /// Завершает ImpersonationHandle, отменяя олицетворение. 
        /// </summary>
        ~ImpersonationHandle()
        {
            Dispose( false );
        }

        /// <summary>
        /// Отменяет выдачу себя за другое лицо. 
        /// </summary>
        public void Dispose()
        {
            Dispose( true );
            GC.SuppressFinalize( this );
        }

        /// <summary>
        /// Отменяет выдачу себя за другое лицо. 
        /// </summary>
        /// <param name="disposing">True if being disposed, false if being finalized.</param>
        private void Dispose( bool disposing )
        {
            // Это реализует вариант типичного шаблона удаления. Всегда пытайтесь вернуться
            // олицетворение, даже после завершения. Не делайте ничего, если мы уже вернулись. 

            if ( this.disposed == false )
            {
                this.disposed = true;

                //На всякий случай, если ссылка вытаскивается, достает стабильную копию
                // ссылки, пока проверяется значение NULL. 
                var serverCopy = this.server;

                if( serverCopy != null && serverCopy.Disposed == false )
                {
                    serverCopy.RevertImpersonate();
                }
            }
        }
    }
}