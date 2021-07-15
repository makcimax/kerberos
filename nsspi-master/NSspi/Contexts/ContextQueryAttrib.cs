using System;

namespace NSspi.Contexts
{
    /// <summary>
    /// Определяет типы запросов, которые могут выполняться с QueryContextAttribute.
    /// Каждый запрос имеет свой буфер результатов. 
    /// </summary>
    internal enum ContextQueryAttrib : int
    {
        /// <summary>
        /// Запрашивает параметры размера буфера при выполнении функций сообщений, таких как
        /// как шифрование, дешифрование, подпись и проверка подписи. 
        /// </summary>
        /// <remarks>
        /// Результаты запроса этого типа хранятся в структуре Win32 SecPkgContext_Sizes. 
        /// </remarks>
        Sizes = 0,

        /// <summary>
        /// Запрашивает в контексте имя пользователя, связанного с контекстом безопасности. 
        /// </summary>
        /// <remarks>
        /// Результаты запроса этого типа хранятся в структуре Win32 SecPkgContext_Name. 
        /// </remarks>
        Names = 1,

        /// <summary>
        /// Запрашивает имя аутентифицирующего органа для контекста безопасности. 
        /// </summary>
        /// <remarks>
        /// Результаты запроса этого типа хранятся в структуре Win32 SecPkgContext_Authority. 
        /// </remarks>
        Authority = 6,

        /// <summary>
        /// Запрашивает контекст для нового ключа SessionKey 
        /// </summary>
        /// <remarks>
        /// Результаты запроса этого типа хранятся в структуре Win32 SecPkgContext_SessionKey. 
        /// </remarks>
        SessionKey = 9,

        AccessToken = 13, //не реализовано
    }
}