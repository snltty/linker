using System;
using System.Runtime.InteropServices;
using System.Text;

namespace linker.libs.winapis
{
    public static class NetApi32
    {
        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint NetUserChangePassword(
            [MarshalAs(UnmanagedType.LPWStr)] string domainname,
            [MarshalAs(UnmanagedType.LPWStr)] string username,
            [MarshalAs(UnmanagedType.LPWStr)] string oldpassword,
            [MarshalAs(UnmanagedType.LPWStr)] string newpassword
        );

        public static bool ChangePassword(
            [MarshalAs(UnmanagedType.LPWStr)] string domainname,
            [MarshalAs(UnmanagedType.LPWStr)] string username,
            [MarshalAs(UnmanagedType.LPWStr)] string oldpassword,
            [MarshalAs(UnmanagedType.LPWStr)] string newpassword
        )
        {
            return NetUserChangePassword(domainname, username, oldpassword, newpassword) == 0;
        }

        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int NetUserEnum(
        StringBuilder servername,
        int level,
        int filter,
        out IntPtr bufptr,
        int prefmaxlen,
        out int entriesread,
        out int totalentries,
        ref int resume_handle
        );

        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode)]
        public static extern int NetUserGetInfo(
        string servername,
        string username,
        int level,
        out IntPtr bufptr
    );

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct USER_INFO_3
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Name;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Password;
            public int PasswordAge;
            public int Privilege;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string HomeDirectory;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Comment;
            public int Flags;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string ScriptPath;
            public int AuthFlags;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string FullName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string UserComment;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Parameters;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Workstations;
            public int LastLogon;
            public int LastLogoff;
            public int AcctExpires;
            public int MaxStorage;
            public int UnitsPerWeek;
            public IntPtr LogonHours;
            public int BadPwdCount;
            public int NumLogons;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string LogonServer;
            public int CountryCode;
            public int CodePage;
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct USER_INFO_0
        {
            public string usri0_name;
        }

        public const int NERR_Success = 0;
        public const int ERROR_ACCESS_DENIED = 5;
        public const int ERROR_INVALID_PARAMETER = 87;
        public const int ERROR_NOT_ENOUGH_MEMORY = 8;
        public const int ERROR_NO_SUCH_USER = 1317;
        public const int ERROR_INVALID_LEVEL = 124;
        public const int ERROR_MORE_DATA = 234;

        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int NetApiBufferFree(IntPtr Buffer);
    }
}
