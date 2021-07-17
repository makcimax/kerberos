using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NSspi
{
    /// <summary>
    /// Запрашивает информацию о пакетах безопасности. 
    /// </summary>
    public static class PackageSupport
    {
        /// <summary>
        /// Возвращает свойства названного пакета. 
        /// </summary>
        /// <param name="packageName">Название пакета. </param>
        /// <returns></returns>
        public static SecPkgInfo GetPackageCapabilities( string packageName )
        {
            SecPkgInfo info;
            SecurityStatus status = SecurityStatus.InternalError;

            IntPtr rawInfoPtr;

            rawInfoPtr = new IntPtr();
            info = new SecPkgInfo();

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            { }
            finally
            {
                status = NativeMethods.QuerySecurityPackageInfo( packageName, ref rawInfoPtr );

                if( rawInfoPtr != IntPtr.Zero )
                {
                    try
                    {
                        if( status == SecurityStatus.OK )
                        {
                            // Выполняется выделение памяти, так как освобождается место для строк, содержащихся в классе SecPkgInfo. 
                            Marshal.PtrToStructure( rawInfoPtr, info );
                        }
                    }
                    finally
                    {
                        NativeMethods.FreeContextBuffer( rawInfoPtr );
                    }
                }
            }

            if( status != SecurityStatus.OK )
            {
                throw new SSPIException( "Failed to query security package provider details", status );
            }

            return info;
        }

        /// <summary>
        /// Возвращает список всех известных поставщиков пакетов безопасности и их свойств. 
        /// </summary>
        /// <returns></returns>
        public static SecPkgInfo[] EnumeratePackages()
        {
            SecurityStatus status = SecurityStatus.InternalError;
            SecPkgInfo[] packages = null;
            IntPtr pkgArrayPtr;
            IntPtr pkgPtr;
            int numPackages = 0;
            int pkgSize = Marshal.SizeOf( typeof( SecPkgInfo ) );

            pkgArrayPtr = new IntPtr();

            RuntimeHelpers.PrepareConstrainedRegions();
            try { }
            finally
            {
                status = NativeMethods.EnumerateSecurityPackages( ref numPackages, ref pkgArrayPtr );

                if( pkgArrayPtr != IntPtr.Zero )
                {
                    try
                    {
                        if( status == SecurityStatus.OK )
                        {
                            // 1) Выделяем массив
                            // 2) Мы выделяем отдельные элементы в массиве (это объекты класса).
                            // 3) Мы выделяем строки в отдельные элементы в массиве, когда мы
                            //когда мы вызываем Marshal.PtrToStructure() 
                            packages = new SecPkgInfo[numPackages];

                            for( int i = 0; i < numPackages; i++ )
                            {
                                packages[i] = new SecPkgInfo();
                            }

                            for( int i = 0; i < numPackages; i++ )
                            {
                                pkgPtr = IntPtr.Add( pkgArrayPtr, i * pkgSize );

                                Marshal.PtrToStructure( pkgPtr, packages[i] );
                            }
                        }
                    }
                    finally
                    {
                        NativeMethods.FreeContextBuffer( pkgArrayPtr );
                    }
                }
            }

            if( status != SecurityStatus.OK )
            {
                throw new SSPIException( "Failed to enumerate security package providers", status );
            }

            return packages;
        }
    }
}