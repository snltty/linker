using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace common.libs.helpers
{
    public static class DisplaysEnumerationHelper
    {
        delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const int CCHDEVICENAME = 32;
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct MonitorInfoEx
        {
            public int Size;
            public RECT Monitor;
            public RECT WorkArea;
            public uint Flags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string DeviceName;
        }

        [DllImport("user32.dll")]
        static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

        public static DisplayInfo[] GetDisplays()
        {
            List<DisplayInfo> result = new List<DisplayInfo>();

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
                {
                    MonitorInfoEx mi = new MonitorInfoEx();
                    mi.Size = Marshal.SizeOf(mi);
                    bool success = GetMonitorInfo(hMonitor, ref mi);
                    if (success)
                    {
                        DisplayInfo info = new DisplayInfo
                        {
                            ScreenSize = new Size(mi.Monitor.Right - mi.Monitor.Left, mi.Monitor.Bottom - mi.Monitor.Top),
                            MonitorArea = new Rectangle(mi.Monitor.Left, mi.Monitor.Top, mi.Monitor.Right - mi.Monitor.Left, mi.Monitor.Bottom - mi.Monitor.Top),
                            WorkArea = new Rectangle(mi.WorkArea.Left, mi.WorkArea.Top, mi.WorkArea.Right - mi.WorkArea.Left, mi.WorkArea.Bottom - mi.WorkArea.Top),
                            IsPrimary = mi.Flags > 0,
                            Hmon = hMonitor,
                            DeviceName = mi.DeviceName
                        };
                        result.Add(info);
                    }
                    return true;
                }, IntPtr.Zero);
            return result.ToArray();
        }
    }

    public partial class DisplayInfo
    {
        public bool IsPrimary { get; set; }
        public Size ScreenSize { get; set; }
        public Rectangle MonitorArea { get; set; }
        public Rectangle WorkArea { get; set; }
        public string DeviceName { get; set; } = string.Empty;

        [JsonIgnore]
        public IntPtr Hmon { get; set; }
    }
}
