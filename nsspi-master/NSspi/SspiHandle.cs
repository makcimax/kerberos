using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace NSspi
{
    /// <summary>
    /// Представляет необработанную структуру для любого дескриптора, созданного для API SSPI, например учетных данных
    /// дескрипторы, дескрипторы контекста и дескрипторы пакетов безопасности. Любой дескриптор SSPI всегда имеет размер
    /// двух собственных указателей. 
    /// </summary>
    /// <remarks>
    /// Документацию для дескрипторов SSPI можно найти здесь: 
    /// http://msdn.microsoft.com/en-us/library/windows/desktop/aa380495(v=vs.85).aspx
    ///
    /// Этот класс небезопасен по ссылкам - при прямом использовании или прямой ссылке на него может произойти утечка,
    /// или в зависимости от гонки финализаторов, или любой из сотен вещей, которые SafeHandles были разработаны для исправления.
    /// Не используйте этот класс напрямую - используйте только объекты оболочки SafeHandle. Любая ссылка необходима
    /// к этому дескриптору для выполнения работы (например, InitializeSecurityContext) должен выполняться CER
    /// который использует подсчет ссылок на дескрипторы при вызове собственного API. 
    /// </remarks>
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct RawSspiHandle
    {
        private IntPtr lowPart;
        private IntPtr highPart;

        /// <summary>
        /// Возвращает независимо от того, установлен ли дескриптор в пустое значение по умолчанию. 
        /// </summary>
        /// <returns></returns>
        public bool IsZero()
        {
            return this.lowPart == IntPtr.Zero && this.highPart == IntPtr.Zero;
        }

        /// <summary>
        /// Устанавливает дескриптор на недопустимое значение. 
        /// </summary>
        /// <remarks>
        /// Этот метод выполняется в CER во время освобождения ручки. 
        /// </remarks>
        [ReliabilityContract( Consistency.WillNotCorruptState, Cer.Success )]
        public void SetInvalid()
        {
            this.lowPart = IntPtr.Zero;
            this.highPart = IntPtr.Zero;
        }
    }

    /// <summary>
    /// Безопасно инкапсулирует необработанный дескриптор, используемый в API SSPI. 
    /// </summary>
    public abstract class SafeSspiHandle : SafeHandle
    {
        internal RawSspiHandle rawHandle;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref = "SafeSspiHandle" />. 
        /// </summary>
        protected SafeSspiHandle()
            : base( IntPtr.Zero, true )
        {
            this.rawHandle = new RawSspiHandle();
        }

        /// <summary>
        /// Получает, является ли дескриптор недействительным. 
        /// </summary>
        public override bool IsInvalid
        {
            get { return IsClosed || this.rawHandle.IsZero(); }
        }

        /// <summary>
        /// Отмечает ручку как неиспользуемую.
        /// </summary>
        /// <returns></returns>
        [ReliabilityContract( Consistency.WillNotCorruptState, Cer.Success )]
        protected override bool ReleaseHandle()
        {
            this.rawHandle.SetInvalid();
            return true;
        }
    }
}