using System;

namespace NSspi
{
    /// <summary>
    /// Читает и записывает типы значений в байтовые массивы с явным порядком байтов. 
    /// </summary>
    public static class ByteWriter
    {
        //Big endian: старший байт по наименьшему адресу в памяти. 

        /// <summary>
        /// Записывает в буфер 2-байтовое целое число со знаком в формате big-endian. 
        /// </summary>
        /// <param name="value">The value to write to the buffer.</param>
        /// <param name="buffer">The buffer to write to.</param>
        /// <param name="position">The index of the first byte to write to.</param>
        public static void WriteInt16_BE( Int16 value, byte[] buffer, int position )
        {
            buffer[position + 0] = (byte)( value >> 8 );
            buffer[position + 1] = (byte)( value );
        }

        /// <summary>
        /// Записывает в буфер 4-байтовое целое число со знаком в формате big-endian. 
        /// </summary>
        /// <param name="value">Значение для записи в буфер. </param>
        /// <param name="buffer">Буфер для записи. </param>
        /// <param name="position">Индекс первого байта для записи. </param>
        public static void WriteInt32_BE( Int32 value, byte[] buffer, int position )
        {
            buffer[position + 0] = (byte)( value >> 24 );
            buffer[position + 1] = (byte)( value >> 16 );
            buffer[position + 2] = (byte)( value >> 8 );
            buffer[position + 3] = (byte)( value );
        }

        /// <summary>
        /// Считывает 2-байтовое целое число со знаком, которое хранится в буфере в формате с прямым порядком байтов.
        /// Возвращаемое значение имеет собственный порядок байтов. 
        /// </summary>
        /// <param name="buffer">Буфер для чтения. </param>
        /// <param name="position">Индекс первого читаемого байта. </param>
        /// <returns></returns>
        public static Int16 ReadInt16_BE( byte[] buffer, int position )
        {
            Int16 value;

            value = (Int16)( buffer[position + 0] << 8 );
            value += (Int16)( buffer[position + 1] );

            return value;
        }

        /// <summary>
        /// Считывает 4-байтовое целое число со знаком, которое хранится в буфере в формате с прямым порядком байтов.
        /// Возвращаемое значение имеет собственный порядок байтов. 
        /// </summary>
        /// <param name="buffer">Буфер для чтения. </param>
        /// <param name="position">Индекс первого читаемого байта. </param>
        /// <returns></returns>
        public static Int32 ReadInt32_BE( byte[] buffer, int position )
        {
            Int32 value;

            value = (Int32)( buffer[position + 0] << 24 );
            value |= (Int32)( buffer[position + 1] << 16 );
            value |= (Int32)( buffer[position + 2] << 8 );
            value |= (Int32)( buffer[position + 3] );

            return value;
        }
    }
}