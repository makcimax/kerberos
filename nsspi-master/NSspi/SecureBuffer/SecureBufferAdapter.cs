using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace NSspi.Buffers
{
    /// <summary>
    /// Подготавливает SecureBuffers для предоставления их собственным вызовам API.
    /// </summary>
    /// <remarks>
    /// Собственные API используют списки буферов, каждый из которых указывает его тип или назначение. 
    ///
    /// Сами буферы представляют собой простые байтовые массивы, а собственные API-интерфейсы используют массивы буферов. 
    ///
    /// Поскольку соглашение о вызовах winapi, возможно, как расширение соглашения о вызовах C, не
    /// предоставляет стандартное соглашение, обозначающее длину любого массива, пользовательские структуры
    /// должны быть созданы для переноса длины и использования буфера. 
    ///
    /// API не только должен знать длину каждого буфера и массив буферов,
    /// ему нужно сообщить, сколько из каждого буфера было заполнено;  можем предоставить ему токен-буфер
    /// длиной 12288 байт, но он может использовать только 125 байт, которые нужно узнать. 
    ///
    ///В результате API требует, чтобы массивы байтов переносились в структуры, которые изначально известны как
    /// SecureBuffers (известные в этом проекте как SecureBufferInternal), а затем массивы SecureBuffers
    /// переносится в структуре SecureBufferDescriptor. 
    ///
    /// Таким образом, этот класс должен проделать значительный объем работы по маршалингу, чтобы вернуть буферы и
    /// к собственным API.
    /// * Мы должны закрепить все буферы
    /// * Мы должны закрепить массив буферов
    /// * Мы должны получить дескрипторы IntPtr для каждого из буферов и для массива буферов.
    /// * Поскольку мы предоставляем классы EasyToUse SecureBuffer из остальной части проекта, но мы
    /// предоставляем структуры SecureBufferInternal из собственного API, мы должны скопировать обратно значения
    /// из структур SecureBufferInternal в наш класс SecureBuffer. 
    ///
    /// Чтобы упростить использование этого класса, он принимает в качестве конструктора один или несколько буферов; а также
    /// реализует IDisposable, чтобы знать, когда маршалировать значения обратно из неуправляемых структур и в
    /// освобождаем закрепленные ручки. 
    ///
    /// Кроме того, в случае утечки адаптера без утилизации, адаптер реализует Critical
    /// Finalizer, чтобы гарантировать, что GCHandles освобождены, иначе мы навсегда закрепим дескрипторы. 
    ///
    /// Типичный поток состоит из одного или нескольких буферов; 
    /// для того чтобы создавать и заполнять необходимые неуправляемые структуры;
    /// пин-память;
    /// получить дескрипторы IntPtr;
    /// позвольте вызывающей стороне получить доступ к IntPtr верхнего уровня, представляющему
    /// SecureBufferDescriptor для предоставления встроенным API; 
    /// подождите, пока вызывающий вызовет родной
    /// API; дождитесь, пока вызывающий объект вызовет наш Dispose;
    /// маршалинг любых данных из собственных структур
    /// (счетчик записи в буфер); отпустите все GCHandles, чтобы открепить память. 
    ///
    ///Общая структура дескриптора выглядит следующим образом:
    /// | - Дескриптор дескриптора
    /// | - Массив буферов
    /// | - Буфер 1
    /// | - Буфер 2
    /// ...
    /// | - Буфер N. 
    ///
    /// Каждый объект в этой структуре должен быть закреплен и передан как IntPtr в собственные API.
    /// Все это для передачи того, что сводится к списку байтовых массивов .. 
    /// </remarks>
    internal sealed class SecureBufferAdapter : CriticalFinalizerObject, IDisposable
    {
        /// <summary>
        /// Был ли адаптер утилизирован. 
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Список управляемых безопасных буферов, предоставленный нам вызывающей стороной. 
        /// </summary>
        private IList<SecureBuffer> buffers;

        /// <summary>
        /// Дескриптор верхнего уровня, представляющий весь дескриптор. 
        /// </summary>
        private GCHandle descriptorHandle;

        /// <summary>
        /// Дескриптор, представляющий массив буферов. 
        /// </summary>
        private GCHandle bufferCarrierHandle;

        /// <summary>
        /// Дескрипторы, представляющие каждый фактический буфер. 
        /// </summary>
        private GCHandle[] bufferHandles;

        /// <summary>
        /// Дескриптор собственного буфера 
        /// </summary>
        private SecureBufferDescInternal descriptor;

        /// <summary>
        /// Массив собственных буферов. 
        /// </summary>
        private SecureBufferInternal[] bufferCarrier;

        /// <summary>
        /// Инициализирует SecureBufferAdapter для переноса одного буфера в собственный API.
        /// <param name="buffer"></param>
        public SecureBufferAdapter( SecureBuffer buffer )
            : this( new[] { buffer } )
        {
        }

        /// <summary>
        /// Инициализирует SecureBufferAdapter для передачи списка буферов в собственный api. 
        /// </summary>
        /// <param name="buffers"></param>
        public SecureBufferAdapter( IList<SecureBuffer> buffers ) : base()
        {
            this.buffers = buffers;

            this.disposed = false;

            this.bufferHandles = new GCHandle[this.buffers.Count];
            this.bufferCarrier = new SecureBufferInternal[this.buffers.Count];

            for( int i = 0; i < this.buffers.Count; i++ )
            {
                this.bufferHandles[i] = GCHandle.Alloc( this.buffers[i].Buffer, GCHandleType.Pinned );

                this.bufferCarrier[i] = new SecureBufferInternal();
                this.bufferCarrier[i].Type = this.buffers[i].Type;
                this.bufferCarrier[i].Count = this.buffers[i].Buffer.Length;
                this.bufferCarrier[i].Buffer = bufferHandles[i].AddrOfPinnedObject();
            }

            this.bufferCarrierHandle = GCHandle.Alloc( bufferCarrier, GCHandleType.Pinned );

            this.descriptor = new SecureBufferDescInternal();
            this.descriptor.Version = SecureBufferDescInternal.ApiVersion;
            this.descriptor.NumBuffers = this.buffers.Count;
            this.descriptor.Buffers = bufferCarrierHandle.AddrOfPinnedObject();

            this.descriptorHandle = GCHandle.Alloc( descriptor, GCHandleType.Pinned );
        }

        [ReliabilityContract( Consistency.WillNotCorruptState, Cer.Success )]
        ~SecureBufferAdapter()
        {
            // Здесь мы изменяем типичный паттерн Dispose. Этот финализатор работает в области ограниченного выполнения,
            // и поэтому мы не должны вызывать виртуальные методы. Нет необходимости расширять этот класс, поэтому мы его предотвращаем.
            // и помечаем защищенный метод Dispose как не виртуальный. 
            Dispose( false );
        }

        /// <summary>
        /// Получает указатель верхнего уровня на дескриптор безопасного буфера для передачи в собственный API. 
        /// </summary>
        public IntPtr Handle
        {
            get
            {
                if( this.disposed )
                {
                    throw new ObjectDisposedException( "Cannot use SecureBufferListHandle after it has been disposed" );
                }

                return this.descriptorHandle.AddrOfPinnedObject();
            }
        }

        /// <summary>
        /// Завершает любой маршалинг передачи буфера и освобождает все ресурсы, связанные с адаптером. 
        /// </summary>
        public void Dispose()
        {
            this.Dispose( true );
            GC.SuppressFinalize( this );
        }

        /// <summary>
        /// Завершает любой маршалинг передачи буфера и освобождает все ресурсы, связанные с адаптером.
        /// Это может быть вызвано финализатором или обычным методом Dispose. В случае финализатора
        /// произошла утечка информации, и нет смысла пытаться маршалировать данные из нативных структур,
        /// мы и не должны, так как они могут исчезнуть. 
        /// </summary>
        /// <param name="disposing">Вызывается ли Dispose. </param>
        [ReliabilityContract( Consistency.WillNotCorruptState, Cer.Success )]
        private void Dispose( bool disposing )
        {
            if( this.disposed == true ) { return; }

            if( disposing )
            {
                // Когда этот класс фактически используется по своему первоначальному назначению - для передачи буферов
                // туда и обратно к вызовам SSPI - нам нужно скопировать потенциально измененные элементы структуры
                // назад в буфер нашего вызывающего абонента. 
                for ( int i = 0; i < this.buffers.Count; i++ )
                {
                    this.buffers[i].Length = this.bufferCarrier[i].Count;
                }
            }

            for( int i = 0; i < this.bufferHandles.Length; i++ )
            {
                if( this.bufferHandles[i].IsAllocated )
                {
                    this.bufferHandles[i].Free();
                }
            }

            if( this.bufferCarrierHandle.IsAllocated )
            {
                this.bufferCarrierHandle.Free();
            }

            if( this.descriptorHandle.IsAllocated )
            {
                this.descriptorHandle.Free();
            }

            this.disposed = true;
        }
    }
}