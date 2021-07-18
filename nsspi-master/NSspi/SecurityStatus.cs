using System;

namespace NSspi
{
    /*
    // From winerror.h
    #define SEC_E_OK                         ((HRESULT)0x00000000L)
    #define SEC_E_INSUFFICIENT_MEMORY        _HRESULT_TYPEDEF_(0x80090300L)
    #define SEC_E_INVALID_HANDLE             _HRESULT_TYPEDEF_(0x80090301L)
    #define SEC_E_UNSUPPORTED_FUNCTION       _HRESULT_TYPEDEF_(0x80090302L)
    #define SEC_E_TARGET_UNKNOWN             _HRESULT_TYPEDEF_(0x80090303L)
    #define SEC_E_INTERNAL_ERROR             _HRESULT_TYPEDEF_(0x80090304L)
    #define SEC_E_SECPKG_NOT_FOUND           _HRESULT_TYPEDEF_(0x80090305L)
    #define SEC_E_NOT_OWNER                  _HRESULT_TYPEDEF_(0x80090306L)
    #define SEC_E_UNKNOWN_CREDENTIALS        _HRESULT_TYPEDEF_(0x8009030DL)
    #define SEC_E_NO_CREDENTIALS             _HRESULT_TYPEDEF_(0x8009030EL)
    */

    /// <summary>
    /// Определяет результаты вызова API SSPI. 
    /// </summary>
    public enum SecurityStatus : uint
    {
        // --- Success / Informational ---

        /// <summary>
        /// Запрос успешно выполнен 
        /// </summary>
        [EnumString( "No error" )]
        OK = 0x00000000,

        /// <summary>
        /// Токен, возвращаемый контекстом, должен быть предоставлен сотрудничающей стороне.
        /// продолжить построение контекста. 
        /// </summary>
        [EnumString( "Authentication cycle needs to continue" )]
        ContinueNeeded = 0x00090312,

        /// <summary>
        /// Происходит после того, как клиент вызывает InitializeSecurityContext, чтобы указать, что клиент
        /// должен вызывать CompleteAuthToken. 
        /// </summary>
        [EnumString( "Authentication cycle needs to perform a 'complete'." )]
        CompleteNeeded = 0x00090313,

        /// <summary>
        /// Происходит после того, как клиент вызывает InitializeSecurityContext, чтобы указать, что клиент
        /// должен вызвать CompleteAuthToken и передать результат серверу. 
        /// </summary>
        [EnumString( "Authentication cycle needs to perform a 'complete' and then continue." )]
        CompAndContinue = 0x00090314,

        /// <summary>
        /// Попытка использовать контекст была предпринята по истечении времени истечения срока действия контекста. 
        /// </summary>
        [EnumString( "The security context was used after its expiration time passed." )]
        ContextExpired = 0x00090317,

        /// <summary>
        /// Учетные данные, предоставленные контексту безопасности, не были полностью инициализированы. 
        /// </summary>
        [EnumString( "The credentials supplied to the security context were not fully initialized." )]
        CredentialsNeeded = 0x00090320,

        /// <summary>
        /// Данные контекста должны быть повторно согласованы с партнером. 
        /// </summary>
        [EnumString( "The context data must be re-negotiated with the peer." )]
        Renegotiate = 0x00090321,

        // -------------- Ошибки --------------

        /// <summary>
        /// Сбой операции SSPI из-за нехватки ресурсов памяти. 
        /// </summary>
        [EnumString( "Not enough memory." )]
        OutOfMemory = 0x80090300,

        /// <summary>
        /// Дескриптор, предоставленный API, недействителен. 
        /// </summary>
        [EnumString( "The handle provided to the API was invalid." )]
        InvalidHandle = 0x80090301,

        /// <summary>
        /// Предпринятая операция не поддерживается. 
        /// </summary>
        [EnumString( "The attempted operation is not supported." )]
        Unsupported = 0x80090302,

        /// <summary>
        /// Указанный принцип не известен в системе аутентификации. 
        /// </summary>
        [EnumString( "The specified principle is not known in the authentication system." )]
        TargetUnknown = 0x80090303,

        /// <summary>
        /// Возникла внутренняя ошибка
        /// </summary>
        [EnumString( "An internal error occurred." )]
        InternalError = 0x80090304,

        /// <summary>
        /// Пакет поставщика безопасности с указанным именем не найден.
        /// </summary>
        [EnumString( "The requested security package was not found." )]
        PackageNotFound = 0x80090305,

        /// <summary>
        /// Невозможно использовать предоставленные учетные данные, вызывающий не является владельцем учетных данных.
        /// </summary>
        [EnumString( "The caller is not the owner of the desired credentials." )]
        NotOwner = 0x80090306,

        /// <summary>
        /// Запрошенный пакет безопасности не удалось инициализировать, поэтому его нельзя использовать.
        /// </summary>
        [EnumString( "The requested security package failed to initalize, and thus cannot be used." )]
        CannotInstall = 0x80090307,

        /// <summary>
        /// Предоставлен токен, содержащий неверные или поврежденные данные. 
        /// </summary>
        [EnumString( "The provided authentication token is invalid or corrupted." )]
        InvalidToken = 0x80090308,

        /// <summary>
        /// Пакет безопасности не может маршалировать буфер входа в систему, поэтому попытка входа в систему не удалась. 
        /// </summary>
        [EnumString( "The security package is not able to marshall the logon buffer, so the logon attempt has failed." )]
        CannotPack = 0x80090309,

        /// <summary>
        /// Качество защиты для каждого сообщения не поддерживается пакетом безопасности. 
        /// </summary>
        [EnumString( "The per-message Quality of Protection is not supported by the security package." )]
        QopNotSupported = 0x8009030A,

        /// <summary>
        /// Выдача себя за другое лицо не поддерживается.
        /// </summary>
        [EnumString( "Impersonation is not supported with the current security package." )]
        NoImpersonation = 0x8009030B,

        /// <summary>
        /// В входе было отказано, возможно, из-за неверных учетных данных. 
        /// </summary>
        [EnumString( "The logon was denied, perhaps because the provided credentials were incorrect." )]
        LogonDenied = 0x8009030C,

        /// <summary>
        /// Указанные учетные данные не распознаются выбранным пакетом безопасности. 
        /// </summary>
        [EnumString( "The credentials provided are not recognized by the selected security package." )]
        UnknownCredentials = 0x8009030D,

        /// <summary>
        /// В выбранном пакете безопасности нет учетных данных. 
        /// </summary>
        [EnumString( "No credentials are available in the selected security package." )]
        NoCredentials = 0x8009030E,

        /// <summary>
        /// Сообщение, предоставленное функциям Decrypt или VerifySignature, было изменено после
        /// создания. 
        /// </summary>
        [EnumString( "A message that was provided to the Decrypt or VerifySignature functions was altered " +
            "after it was created." )]
        MessageAltered = 0x8009030F,

        /// <summary>
        /// Сообщение было получено не в ожидаемом порядке. 
        /// </summary>
        [EnumString( "A message was received out of the expected order." )]
        OutOfSequence = 0x80090310,

        /// <summary>
        /// Текущий пакет безопасности не может связаться с центром аутентификации. 
        /// </summary>
        [EnumString( "The current security package cannot contact an authenticating authority." )]
        NoAuthenticatingAuthority = 0x80090311,

        /// <summary>
        /// Буфер, предоставленный для вызова API SSPI, содержал сообщение, которое не было завершено. 
        /// </summary>
        /// <remarks>
        /// Это происходит регулярно с контекстами SSPI, которые обмениваются данными с использованием контекста потоковой передачи,
        /// где данные, возвращаемые из канала потоковой связи, такого как сокет TCP,
        /// не содержит полного сообщения.
        /// Точно так же потоковый канал может возвращать слишком много данных, и в этом случае функция API
        /// будет означать успех, но сохранит лишние несвязанные данные в буфере
        /// набираем 'extra'. 
        /// </remarks>
        [EnumString( "The buffer provided to an SSPI API call contained a message that was not complete." )]
        IncompleteMessage = 0x80090318,

        /// <summary>
        /// Предоставленные учетные данные не были полными и не могли быть проверены. Не удалось инициализировать контекст. 
        /// </summary>
        [EnumString( "The credentials supplied were not complete, and could not be verified. The context could not be initialized." )]
        IncompleteCredentials = 0x80090320,

        /// <summary>
        /// Буферы, предоставленные функции безопасности, были слишком малы. 
        /// </summary>
        [EnumString( "The buffers supplied to a security function were too small." )]
        BufferNotEnough = 0x80090321,

        /// <summary>
        /// Целевое главное имя неверно. 
        /// </summary>
        [EnumString( "The target principal name is incorrect." )]
        WrongPrincipal = 0x80090322,

        /// <summary>
        /// Часы на клиентских и серверных машинах перекошены. 
        /// </summary>
        [EnumString( "The clocks on the client and server machines are skewed." )]
        TimeSkew = 0x80090324,

        /// <summary>
        /// Цепочка сертификатов была выпущена центром, которому не доверяют. 
        /// </summary>
        [EnumString( "The certificate chain was issued by an authority that is not trusted." )]
        UntrustedRoot = 0x80090325,

        /// <summary>
        /// Полученное сообщение было неожиданным или неправильно отформатировано. 
        /// </summary>
        [EnumString( "The message received was unexpected or badly formatted." )]
        IllegalMessage = 0x80090326,

        /// <summary>
        /// При обработке сертификата произошла неизвестная ошибка. 
        /// </summary>
        [EnumString( "An unknown error occurred while processing the certificate." )]
        CertUnknown = 0x80090327,

        /// <summary>
        /// Срок действия полученного сертификата истек.
        /// </summary>
        [EnumString( "The received certificate has expired." )]
        CertExpired = 0x80090328,

        /// <summary>
        /// Клиент и сервер не могут общаться, потому что у них нет общего алгоритма. 
        /// </summary>
        [EnumString( "The client and server cannot communicate, because they do not possess a common algorithm." )]
        AlgorithmMismatch = 0x80090331,

        /// <summary>
        /// Контекст безопасности не может быть установлен из-за сбоя в запрошенном качестве
        /// услуги (например, взаимная аутентификация или делегирование). 
        /// </summary>
        [EnumString( "The security context could not be established due to a failure in the requested " + 
            "quality of service (e.g. mutual authentication or delegation)." )]
        SecurityQosFailed = 0x80090332,

        /// <summary>
        /// Требуется вход со смарт-картой, и она не использовалась. 
        /// </summary>
        [EnumString( "Smartcard logon is required and was not used." )]
        SmartcardLogonRequired = 0x8009033E,

        /// <summary>
        /// В пакет Kerberos был представлен неподдерживаемый механизм предварительной проверки подлинности. 
        /// </summary>
        [EnumString( "An unsupported preauthentication mechanism was presented to the Kerberos package." )]
        UnsupportedPreauth = 0x80090343,

        /// <summary>
        /// Привязки каналов SSPI, предоставленные клиентом, были неправильными. 
        /// </summary>
        [EnumString( "Client's supplied SSPI channel bindings were incorrect." )]
        BadBinding = 0x80090346
    }

    /// <summary>
    /// Предоставляет методы расширения для перечисления SecurityStatus. 
    /// </summary>
    public static class SecurityStatusExtensions
    {
        /// <summary>
        /// Возвращает независимо от того, представляет ли статус ошибку. 
        /// </summary>
        /// <param name="status"></param>
        /// <returns>Истинно, если статус представляет собой состояние ошибки.</returns>
        public static bool IsError( this SecurityStatus status )
        {
            return (uint)status > 0x80000000u;
        }
    }
}