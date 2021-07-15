using System;
using System.Runtime.InteropServices;

namespace NSspi.Buffers
{
    /// <summary>
    /// Представляет собственную структуру SecureBuffer, которая используется для связи
    /// буферы к собственным API. 
    /// </summary>
    [StructLayout( LayoutKind.Sequential )]
    internal struct SecureBufferInternal
    {
        /// <summary>
        /// При предоставлении встроенному API - общее количество байтов, доступных в буфере.
        /// При возврате из собственного API количество байтов, которые были заполнены или использованы
        /// собственный API. 
        /// </summary>
        public int Count;

        /// <summary>
        /// Тип или назначение буфера. 
        /// </summary>
        public BufferType Type;

        /// <summary>
        /// Указатель на закрепленный буфер byte []. 
        /// </summary>
        public IntPtr Buffer;
    }

    /// <summary>
    /// Хранит буферы для предоставления токенов и данных встроенным API SSPI. 
    /// </summary>
    /// <remarks>Буфер транслируется в SecureBufferInternal для фактического вызова.
    /// Чтобы упростить код установки вызова и централизовать код закрепления буфера,
    /// этот класс хранит и возвращает буферы как обычные байтовые массивы. Буфер
    /// поддержки закрепления в SecureBufferAdapter обрабатывает преобразование в SecureBufferInternal
    /// для перехода к управляемому API, а также для закрепления соответствующих фрагментов памяти. 
    ///
    /// Кроме того, собственный API может не использовать весь буфер, поэтому механизм
    /// необходим для сообщения об использовании буфера отдельно от длины
    /// буфера. </remarks>
    internal class SecureBuffer
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса SecureBuffer. 
        /// </summary>
        /// <param name="buffer">Оборачиваемый буфер. </param>
        /// <param name="type">Тип или назначение буфера для целей
        /// вызов собственного API. </param>
        public SecureBuffer( byte[] buffer, BufferType type )
        {
            this.Buffer = buffer;
            this.Type = type;
            this.Length = this.Buffer.Length;
        }

        /// <summary>
        /// Тип или цели API для вызова собственного API.
        /// </summary>
        public BufferType Type { get; set; }

        /// <summary>
        /// Буфер для собственного API.
        /// </summary>
        public byte[] Buffer { get; set; }

        /// <summary>
        /// Количество элементов, которые были фактически заполнены или использованы собственным API,
        /// который может быть меньше общей длины буфера. 
        /// </summary>
        public int Length { get; internal set; }
    }
}