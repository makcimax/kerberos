using System;

namespace NSspi.Credentials
{
    /// <summary>
    /// Определяет типы запросов к учетным данным.
    /// </summary>
    public enum CredentialQueryAttrib : uint
    {
        /// <summary>
        /// Запрашивает основное имя учетных данных.
        /// </summary>
        Names = 1,
    }
}