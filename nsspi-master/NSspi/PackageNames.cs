using System;

namespace NSspi
{
    /// <summary>
    /// Предоставляет канонические имена для пакетов безопасности. 
    /// </summary>
    public static class PackageNames
    {
        /// <summary>
        /// Указывает пакет безопасности Negotiate. 
        /// </summary>
        public const string Negotiate = "Negotiate";

        /// <summary>
        /// Указывает пакет безопасности Kerberos. 
        /// </summary>
        public const string Kerberos = "Kerberos";

        /// <summary>
        /// Указывает на пакет безопасности NTLM. 
        /// </summary>
        public const string Ntlm = "NTLM";
    }
}