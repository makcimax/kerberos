using System;
using System.Runtime.InteropServices;

namespace NSspi
{
    /// <summary>
    /// Представляет структуру отметки времени Windows API, в которой время хранится в единицах 100 наносекунды.
    /// тиков, считая с 1 января 1601 года в 00:00 UTC. Время хранится как 64-битное значение. 
    /// </summary>
    [StructLayout( LayoutKind.Sequential )]
    public struct TimeStamp
    {
        /// <summary>
        /// Возвращает календарную дату и время, соответствующие нулевой отметке времени. 
        /// </summary>
        public static readonly DateTime Epoch = new DateTime( 1601, 1, 1, 0, 0, 0, DateTimeKind.Utc );

        /// <summary>
        /// Сохраняет значение времени. Бесконечное время часто представляется как близкое, но не совсем точное значение.
        /// при максимальном знаковом 64-битном значении дополнения до 2. 
        /// </summary>
        private long time;

        /// <summary>
        /// Преобразует TimeStamp в эквивалентный объект DateTime. Если TimeStamp представляет
        /// значение больше DateTime.MaxValue, тогда возвращается DateTime.MaxValue. 
        /// </summary>
        /// <returns></returns>
        public DateTime ToDateTime()
        {
            ulong test = (ulong)this.time + (ulong)( Epoch.Ticks );

            // Иногда возвращается массивное значение, например 0x7fffff154e84ffff, что является значением
            // где-то в 30848 году. Это приведет к переполнению DateTime, поскольку его пик приходится на 31 декабря 9999 года.
            // Оказывается, это значение соответствует максимальному значению TimeStamp, уменьшенному на мой местный часовой пояс 
            // http://stackoverflow.com/questions/24478056/
            if ( test > (ulong)DateTime.MaxValue.Ticks )
            {
                return DateTime.MaxValue;
            }
            else
            {
                return DateTime.FromFileTimeUtc( this.time );
            }
        }
    }
}