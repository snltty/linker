using common.libs.winapis;
using System;
using System.Runtime.InteropServices;
using static common.libs.winapis.User32;

namespace common.libs.helpers
{
    public static class LastInputHelper
    {
        private static LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
        public static uint GetLastInputInfo()
        {
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO));
            bool res = User32.GetLastInputInfo(ref lastInputInfo);
            if (res)
            {
                return (uint)Environment.TickCount - lastInputInfo.dwTime;
            }
            return 0;
        }
    }
}
