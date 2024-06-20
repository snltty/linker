using common.libs.winapis;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static common.libs.winapis.User32;

namespace common.libs.helpers
{
    [SupportedOSPlatform("windows")]
    public class CursorHelper
    {
        public static void DrawCursorIcon(Graphics g, int sourceWidth, float scaleX, float scaleY, float configScale)
        {
            if (OperatingSystem.IsWindows())
            {
                int curWidth = (int)(sourceWidth * configScale * configScale);
                CursorInfo pci;
                pci.cbSize = Marshal.SizeOf(typeof(CursorInfo));
                if (GetCursorInfo(out pci))
                {
                    if (pci.flags == CURSOR_SHOWING)
                    {
                        nint hdc1 = g.GetHdc();
                        User32.DrawIconEx(hdc1, (int)(pci.ptScreenPos.x * scaleX), (int)(pci.ptScreenPos.y * scaleY), pci.hCursor, curWidth, curWidth, 0, IntPtr.Zero, User32.DI_NORMAL);
                        g.ReleaseHdc();
                    }
                }
            }
        }
        public static void DrawCursorIcon(Graphics g, float scaleX, float scaleY)
        {
            if (OperatingSystem.IsWindows())
            {
                CursorInfo pci;
                pci.cbSize = Marshal.SizeOf(typeof(CursorInfo));
                if (GetCursorInfo(out pci))
                {
                    if (pci.flags == CURSOR_SHOWING)
                    {
                        using Icon icon = Icon.FromHandle(pci.hCursor);

                        g.DrawIcon(icon, new System.Drawing.Rectangle(new Point((int)(pci.ptScreenPos.x * scaleX), (int)(pci.ptScreenPos.y * scaleY)), icon.Size));
                    }
                }
            }
        }
        public static bool GetCursorPosition(out int x, out int y)
        {
            x = 0;
            y = 0;

            CursorInfo pci;
            pci.cbSize = Marshal.SizeOf(typeof(CursorInfo));
            bool res = GetCursorInfo(out pci);
            if (res)
            {
                x = pci.ptScreenPos.x;
                y = pci.ptScreenPos.y;
            }
            return res;
        }
    }
}
