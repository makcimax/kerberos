using System;
using System.Runtime.InteropServices;

namespace NSspi
{
    /// <summary>
    ///Хранит информацию о конкретном пакете безопасности. 
    /// </summary>
    [StructLayout( LayoutKind.Sequential )]
    public class SecPkgInfo
    {
        /// <summary>
        /// Возможности и опции пакетов. 
        /// </summary>
        public SecPkgCapability Capabilities;

        /// <summary>
        /// Номер версии пакета. 
        /// </summary>
        public short Version;

        /// <summary>
        /// Идентификатор пакета при использовании в контекстах RPC. 
        /// </summary>
        public short RpcId;

        /// <summary>
        /// Максимальный размер токенов, сгенерированных пакетом, в байтах. 
        /// </summary>
        public int MaxTokenLength;

        /// <summary>
        /// Удобочитаемое имя пакета.
        /// </summary>
        [MarshalAs( UnmanagedType.LPWStr )]
        public string Name;

        /// <summary>
        /// Краткое описание пакета. 
        /// </summary>
        [MarshalAs( UnmanagedType.LPWStr )]
        public string Comment;
    }

    /// <summary>
    /// Описывает возможности пакета безопасности
    /// </summary>
    [Flags]
    public enum SecPkgCapability : uint
    {
        /// <summary>
        /// Поддерживает ли пакет создание сообщений с информацией о целостности. Требуется для MakeSignature и VerifySignature.
        /// </summary>
        Integrity = 0x1,

        /// <summary>
        /// Поддерживает ли пакет создание зашифрованных сообщений. Требуется для EncryptMessage и DecryptMessage.
        /// </summary>
        Privacy = 0x2,

        /// <summary>
        /// Использует ли пакет какую-либо другую информацию о буфере, кроме буферов токенов.
        /// </summary>
        TokenOnly = 0x4,

        /// <summary>
        /// Поддерживает ли пакет аутентификацию в стиле дейтаграммы.
        /// </summary>
        Datagram = 0x8,

        /// <summary>
        /// Поддерживает ли пакет создание контекстов с семантикой соединения
        /// </summary>
        Connection = 0x10,

        /// <summary>
        /// Для аутентификации необходимо несколько ветвей.
        /// </summary>
        MultiLeg = 0x20,

        /// <summary>
        /// Аутентификация сервера не поддерживается..
        /// </summary>
        ClientOnly = 0x40,

        /// <summary>
        /// Поддерживает расширенные средства обработки ошибок.
        /// </summary>
        ExtendedError = 0x80,

        /// <summary>
        /// Поддерживает олицетворение клиента на сервере.
        /// </summary>
        Impersonation = 0x100,

        /// <summary>
        /// Понимает основные и целевые имена Windows.
        /// </summary>
        AcceptWin32Name = 0x200,

        /// <summary>
        /// Поддерживает семантику потока
        /// </summary>
        Stream = 0x400,

        /// <summary>
        /// Пакет может использоваться мета-пакетом Negiotiate. 
        /// </summary>
        Negotiable = 0x800,

        /// <summary>
        /// Совместим с GSS. 
        /// </summary>
        GssCompatible = 0x1000,

        /// <summary>
        /// Поддерживает LsaLogonUser 
        /// </summary>
        Logon = 0x2000,

        /// <summary>
        /// Буферы токенов имеют формат Ascii. 
        /// </summary>
        AsciiBuffers = 0x4000,

        /// <summary>
        /// Поддерживает разделение больших токенов на несколько буферов.
        /// </summary>
        Fragment = 0x8000,

        /// <summary>
        /// Поддерживает взаимную аутентификацию между клиентом и сервером. 
        /// </summary>
        MutualAuth = 0x10000,

        /// <summary>
        /// Поддерживает делегирование учетных данных с сервера в третий контекст. 
        /// </summary>
        Delegation = 0x20000,

        /// <summary>
        /// Поддерживает вызов EncryptMessage с флагом контрольной суммы только для чтения, который защищает только данные
        /// с контрольной суммой и не шифрует ее. 
        /// </summary>
        ReadOnlyChecksum = 0x40000,

        /// <summary>
        /// Поддерживает ли пакет обработку ограниченных токенов, которые являются токенами, производными от существующих токенов.
        /// на которые наложены ограничения. 
        /// </summary>
        RestrictedTokens = 0x80000,

        /// <summary>
        /// Расширяет переговорный пакет; единовременно может быть зарегистрирован только один такой пакет. 
        /// </summary>
        ExtendsNego = 0x00100000,

        /// <summary>
        /// Этот пакет согласовывается с пакетом типа ExtendsNego. 
        /// </summary>
        Negotiable2 = 0x00200000,
    }
}