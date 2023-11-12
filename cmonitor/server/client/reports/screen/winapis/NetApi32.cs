using System.Runtime.InteropServices;

namespace cmonitor.server.client.reports.screen.winapis
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
    }
}
