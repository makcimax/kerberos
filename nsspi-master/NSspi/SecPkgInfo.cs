﻿using System;
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
    /// Describes the capabilities of a security package.
    /// </summary>
    [Flags]
    public enum SecPkgCapability : uint
    {
        /// <summary>
        /// Whether the package supports generating messages with integrity information. Required for MakeSignature and VerifySignature.
        /// </summary>
        Integrity = 0x1,

        /// <summary>
        /// Whether the package supports generating encrypted messages. Required for EncryptMessage and DecryptMessage.
        /// </summary>
        Privacy = 0x2,

        /// <summary>
        /// Whether the package uses any other buffer information than token buffers.
        /// </summary>
        TokenOnly = 0x4,

        /// <summary>
        /// Whether the package supports datagram-style authentication.
        /// </summary>
        Datagram = 0x8,

        /// <summary>
        /// Whether the package supports creating contexts with connection semantics
        /// </summary>
        Connection = 0x10,

        /// <summary>
        /// Multiple legs are neccessary for authentication.
        /// </summary>
        MultiLeg = 0x20,

        /// <summary>
        /// Server authentication is not supported.
        /// </summary>
        ClientOnly = 0x40,

        /// <summary>
        /// Supports extended error handling facilities.
        /// </summary>
        ExtendedError = 0x80,

        /// <summary>
        /// Supports client impersonation on the server.
        /// </summary>
        Impersonation = 0x100,

        /// <summary>
        /// Understands Windows princple and target names.
        /// </summary>
        AcceptWin32Name = 0x200,

        /// <summary>
        /// Supports stream semantics
        /// </summary>
        Stream = 0x400,

        /// <summary>
        /// Package may be used by the Negiotiate meta-package.
        /// </summary>
        Negotiable = 0x800,

        /// <summary>
        /// Compatible with GSS.
        /// </summary>
        GssCompatible = 0x1000,

        /// <summary>
        /// Supports LsaLogonUser
        /// </summary>
        Logon = 0x2000,

        /// <summary>
        /// Token buffers are in Ascii format.
        /// </summary>
        AsciiBuffers = 0x4000,

        /// <summary>
        /// Supports separating large tokens into multiple buffers.
        /// </summary>
        Fragment = 0x8000,

        /// <summary>
        /// Supports mutual authentication between a client and server.
        /// </summary>
        MutualAuth = 0x10000,

        /// <summary>
        /// Supports credential delegation from the server to a third context.
        /// </summary>
        Delegation = 0x20000,

        /// <summary>
        /// Supports calling EncryptMessage with the read-only-checksum flag, which protects data only
        /// with a checksum and does not encrypt it.
        /// </summary>
        ReadOnlyChecksum = 0x40000,

        /// <summary>
        /// Whether the package supports handling restricted tokens, which are tokens derived from existing tokens
        /// that have had restrictions placed on them.
        /// </summary>
        RestrictedTokens = 0x80000,

        /// <summary>
        /// Extends the negotiate package; only one such package may be registered at any time.
        /// </summary>
        ExtendsNego = 0x00100000,

        /// <summary>
        /// This package is negotiated by the package of type ExtendsNego.
        /// </summary>
        Negotiable2 = 0x00200000,
    }
}