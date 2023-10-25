using System.Drawing;
using System.Runtime.InteropServices;

namespace cmonitor.server.client.reports.screen
{
    public sealed class WinApi
    {

        public static void DrawCursorIcon(Graphics g, int sourceWidth, float scaleX, float scaleY, float configScale)
        {
            if (OperatingSystem.IsWindows())
            {
                int curWidth = (int)(sourceWidth * configScale * configScale);
                CURSORINFO pci;
                pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
                if (GetCursorInfo(out pci))
                {
                    if (pci.flags == CURSOR_SHOWING)
                    {
                        var hdc1 = g.GetHdc();
                        DrawIconEx(hdc1, (int)(pci.ptScreenPos.x * scaleX), (int)(pci.ptScreenPos.y * scaleY), pci.hCursor, curWidth, curWidth, 0, IntPtr.Zero, DI_NORMAL);
                        g.ReleaseHdc();
                    }
                }
            }
        }

        public static void DrawCursorIcon(Graphics g, float scaleX, float scaleY)
        {
            if (OperatingSystem.IsWindows())
            {
                CURSORINFO pci;
                pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
                if (GetCursorInfo(out pci))
                {
                    if (pci.flags == CURSOR_SHOWING)
                    {
                        using System.Drawing.Icon icon = System.Drawing.Icon.FromHandle(pci.hCursor);

                        g.DrawIcon(icon, new System.Drawing.Rectangle(new Point((int)(pci.ptScreenPos.x * scaleX), (int)(pci.ptScreenPos.y * scaleY)), icon.Size));
                    }
                }
            }
        }
        public static bool GetCursorPosition(out int x, out int y)
        {
            x = 0;
            y = 0;

            CURSORINFO pci;
            pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
            bool res = GetCursorInfo(out pci);
            if (res)
            {
                x = pci.ptScreenPos.x;
                y = pci.ptScreenPos.y;
            }
            return res;
        }


        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public POINTAPI ptScreenPos;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct POINTAPI
        {
            public int x;
            public int y;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct IconInfo
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }
        private const Int32 CURSOR_SHOWING = 0x0001;
        private const Int32 DI_NORMAL = 0x0003;

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(out CURSORINFO pci);
        [DllImport("user32.dll")]
        public static extern bool GetIconInfo(IntPtr hIcon, out IconInfo piconinfo);


        [DllImport("user32.dll", SetLastError = true)]
        static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon, int cxWidth, int cyHeight, int istepIfAniCur, IntPtr hbrFlickerFreeDraw, int diFlags);


        public const int DESKTOPVERTRES = 117;
        public const int DESKTOPHORZRES = 118;
        public const int SM_CXSCREEN = 0;
        public const int SM_CYSCREEN = 1;
        public const int SM_REMOTESESSION = 0x1000;

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr ptr);

        [DllImport("user32.dll", EntryPoint = "ReleaseDC")]
        public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);

        [DllImport("gdi32.dll", EntryPoint = "GetDeviceCaps", SetLastError = true)]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetSystemMetrics")]
        public static extern int GetSystemMetrics(int mVal);





        #region 最后活动时间
        private static LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
        public static void InitLastInputInfo()
        {
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO));
        }
        public static uint GetLastInputInfo()
        {
            bool res = GetLastInputInfo(ref lastInputInfo);
            if (res)
            {
                return (uint)Environment.TickCount - lastInputInfo.dwTime;
            }
            return 0;
        }
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }
        #endregion


        [DllImport("wtsapi32.dll")]
        static extern bool WTSQuerySessionInformation(IntPtr hServer, int sessionId, WTS_INFO_CLASS wtsInfoClass, out IntPtr ppBuffer, out uint pBytesReturned);
        [DllImport("wtsapi32.dll")]
        static extern void WTSFreeMemory(IntPtr pMemory);
        [StructLayout(LayoutKind.Sequential)]
        struct WTSINFO
        {
            public int SessionId;
            public int WinStationNameLen;
            public string WinStationName;
            public int State;
        }
        enum WTS_INFO_CLASS
        {
            WTSInitialProgram,
            WTSApplicationName,
            WTSWorkingDirectory,
            WTSOEMId,
            WTSSessionId,
            WTSUserName,
            WTSWinStationName,
            WTSDomainName,
            WTSConnectState,
            WTSClientBuildNumber,
            WTSClientName,
            WTSClientDirectory,
            WTSClientProductId,
            WTSClientHardwareId,
            WTSClientAddress,
            WTSClientDisplay,
            WTSClientProtocolType,
            WTSIdleTime,
            WTSLogonTime,
            WTSIncomingBytes,
            WTSOutgoingBytes,
            WTSIncomingFrames,
            WTSOutgoingFrames,
            WTSClientInfo,
            WTSSessionInfo,
            WTSConfigInfo,
            WTSShadowInfo,
            WTSValidConnectionTypes,
            WTSRemoteAddressV4,
            WTSRemoteAddressV6,
            WTSIsRemoteSession
        }
        public static bool Locked()
        {
            IntPtr buffer = IntPtr.Zero;
            uint bytesReturned = 0;
            try
            {
                bool result = WTSQuerySessionInformation(IntPtr.Zero, -1, WTS_INFO_CLASS.WTSSessionInfo, out buffer, out bytesReturned);

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
    }
}
