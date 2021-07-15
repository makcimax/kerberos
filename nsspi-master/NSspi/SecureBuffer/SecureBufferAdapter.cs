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
    /// Additionally, in case the adapter is leaked without disposing, the adapter implements a Critical
    /// Finalizer, to ensure that the GCHandles are released, else we will permanently pin handles.
    ///
    /// The typical flow is to take one or many buffers; create and fill the neccessary unmanaged structures;
    /// pin memory; acquire the IntPtr handles; let the caller access the top-level IntPtr representing
    /// the SecureBufferDescriptor, to provide to the native APIs; wait for the caller to invoke the native
    /// API; wait for the caller to invoke our Dispose; marshal back any data from the native structures
    /// (buffer write counts); release all GCHandles to unpin memory.
    ///
    /// The total descriptor structure is as follows:
    /// |-- Descriptor handle
    ///     |-- Array of buffers
    ///         |-- Buffer 1
    ///         |-- Buffer 2
    ///         ...
    ///         |-- Buffer N.
    ///
    /// Each object in that structure must be pinned and passed as an IntPtr to the native APIs.
    /// All this to pass what boils down to a List of byte arrays..
    /// </remarks>
    internal sealed class SecureBufferAdapter : CriticalFinalizerObject, IDisposable
    {
        /// <summary>
        /// Whether the adapter has already been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// The list of mananged SecureBuffers the caller provided to us.
        /// </summary>
        private IList<SecureBuffer> buffers;

        /// <summary>
        /// The top level handle representing the entire descriptor.
        /// </summary>
        private GCHandle descriptorHandle;

        /// <summary>
        /// The handle representing the array of buffers.
        /// </summary>
        private GCHandle bufferCarrierHandle;

        /// <summary>
        /// The handles representing each actual buffer.
        /// </summary>
        private GCHandle[] bufferHandles;

        /// <summary>
        /// The native buffer descriptor
        /// </summary>
        private SecureBufferDescInternal descriptor;

        /// <summary>
        /// An array of the native buffers.
        /// </summary>
        private SecureBufferInternal[] bufferCarrier;

        /// <summary>
        /// Initializes a SecureBufferAdapter to carry a single buffer to the native api.
        /// </summary>
        /// <param name="buffer"></param>
        public SecureBufferAdapter( SecureBuffer buffer )
            : this( new[] { buffer } )
        {
        }

        /// <summary>
        /// Initializes the SecureBufferAdapter to carry a list of buffers to the native api.
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
            // We bend the typical Dispose pattern here. This finalizer runs in a Constrained Execution Region,
            // and so we shouldn't call virtual methods. There's no need to extend this class, so we prevent it
            // and mark the protected Dispose method as non-virtual.
            Dispose( false );
        }

        /// <summary>
        /// Gets the top-level pointer to the secure buffer descriptor to pass to the native API.
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
        /// Completes any buffer passing marshaling and releases all resources associated with the adapter.
        /// </summary>
        public void Dispose()
        {
            this.Dispose( true );
            GC.SuppressFinalize( this );
        }

        /// <summary>
        /// Completes any buffer passing marshaling and releases all resources associated with the adapter.
        /// This may be called by the finalizer, or by the regular Dispose method. In the case of the finalizer,
        /// we've been leaked and there's no point in attempting to marshal back data from the native structures,
        /// nor should we anyway since they may be gone.
        /// </summary>
        /// <param name="disposing">Whether Dispose is being called.</param>
        [ReliabilityContract( Consistency.WillNotCorruptState, Cer.Success )]
        private void Dispose( bool disposing )
        {
            if( this.disposed == true ) { return; }

            if( disposing )
            {
                // When this class is actually being used for its original purpose - to convey buffers
                // back and forth to SSPI calls - we need to copy the potentially modified structure members
                // back to our caller's buffer.
                for( int i = 0; i < this.buffers.Count; i++ )
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