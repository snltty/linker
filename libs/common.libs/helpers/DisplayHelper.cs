using System.Runtime.InteropServices;
using System;
using static cmonitor.libs.winapis.WTSAPI32;
using common.libs.winapis;

namespace common.libs.helpers
{
    public class DisplayHelper
    {
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MONITORPOWER = 0xF170;
        private const int MONITOR_ON = -1;
        private const int MONITOR_OFF = 2;
        /// <summary>
        /// 关闭显示器
        /// </summary>
        public static void Off()
        {
            User32.SendMessage(0xffff, WM_SYSCOMMAND, SC_MONITORPOWER, MONITOR_OFF);
        }
        /// <summary>
        /// 关闭显示器
        /// </summary>
        public static void On()
        {
            User32.SendMessage(0xffff, WM_SYSCOMMAND, SC_MONITORPOWER, MONITOR_ON);
        }


        [StructLayout(LayoutKind.Sequential)]
        struct WTSINFO
        {
            public int SessionId;
            public int WinStationNameLen;
            public string WinStationName;
            public int State;
        }

        /// <summary>
        /// 是否已锁定
        /// </summary>
        /// <param name="sessionid"></param>
        /// <returns></returns>
        public static bool Locked(uint sessionid)
        {
            IntPtr buffer = IntPtr.Zero;
            uint bytesReturned = 0;
            try
            {
                bool result = WTSQuerySessionInformation(IntPtr.Zero, sessionid, WTS_INFO_CLASS.WTSSessionInfo, out buffer, out bytesReturned);

                if (result)
                {
                    WTSINFO sessionInfo = (WTSINFO)Marshal.PtrToStructure(buffer, typeof(WTSINFO));

                    return sessionInfo.State == 0;
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                {
                    WTSFreeMemory(buffer);
                }
            }
            return false;
        }

        /// <summary>
        /// 获取缩放比例
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="sourceWidth"></param>
        /// <param name="sourceHeight"></param>
        /// <returns></returns>
        public static bool GetSystemScale(out float x, out float y, out int sourceWidth, out int sourceHeight)
        {
            x = 1;
            y = 1;
            sourceWidth = 0;
            sourceHeight = 0;
            IntPtr hdc = User32.GetDC(IntPtr.Zero);
            if (hdc != IntPtr.Zero)
            {
                sourceWidth = User32.GetDeviceCaps(hdc, User32.DESKTOPHORZRES);
                sourceHeight = User32.GetDeviceCaps(hdc, User32.DESKTOPVERTRES);
                int screenWidth = User32.GetSystemMetrics(User32.SM_CXSCREEN);
                int screenHeight = User32.GetSystemMetrics(User32.SM_CYSCREEN);

                x = sourceWidth * 1.0f / screenWidth;
                y = sourceHeight * 1.0f / screenHeight;

                User32.ReleaseDC(IntPtr.Zero, hdc);

                return true;
            }
            return false;
        }
    }
}
