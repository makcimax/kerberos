using System;
using System.Runtime.InteropServices;

namespace NSspi.Credentials
{
    /// <summary>
    /// Сохраняет результат запроса имени субъекта учетных данных. 
    /// </summary>
    [StructLayout( LayoutKind.Sequential )]
    internal struct QueryNameAttribCarrier
    {
        /// <summary>
        /// Указатель на ascii-код с завершающим нулем, содержащий имя принципа
        /// связанного с учетными данными 
        /// </summary>
        public IntPtr Name;
    }
}