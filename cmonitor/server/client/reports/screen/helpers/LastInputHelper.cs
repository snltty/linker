using cmonitor.server.client.reports.screen.winapiss;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static cmonitor.server.client.reports.screen.winapiss.User32;

namespace cmonitor.server.client.reports.screen.helpers
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
