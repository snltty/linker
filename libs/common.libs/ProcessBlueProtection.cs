using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace common.libs
{
    public static class ProcessBlueProtection
    {
        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern void RtlSetProcessIsCritical(UInt32 v1, UInt32 v2, UInt32 v3);
        private static volatile bool s_isProtected = false;
        private static ReaderWriterLockSlim s_isProtectedLock = new ReaderWriterLockSlim();
        public static bool IsProtected
        {
            get
            {
                try
                {
                    s_isProtectedLock.EnterReadLock();

                    return s_isProtected;
                }
                finally
                {
                    s_isProtectedLock.ExitReadLock();
                }
            }
        }
        public static void Protect()
        {
            try
            {
                s_isProtectedLock.EnterWriteLock();

                if (!s_isProtected)
                {
                    System.Diagnostics.Process.EnterDebugMode();
                    RtlSetProcessIsCritical(1, 0, 0);
                    s_isProtected = true;
                }
            }
            finally
            {
                s_isProtectedLock.ExitWriteLock();
            }
        }
        public static void Unprotect()
        {
            try
            {
                s_isProtectedLock.EnterWriteLock();

                if (s_isProtected)
                {
                    RtlSetProcessIsCritical(0, 0, 0);
                    s_isProtected = false;
                }
            }
            finally
            {
                s_isProtectedLock.ExitWriteLock();
            }
        }
    }
}
