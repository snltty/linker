using System.Buffers;
using System.Drawing;
using System.Runtime.InteropServices;

namespace cmonitor.server.client.reports.screen
{
    public sealed class GdiCapture
    {
        static ScreenClipInfo screenClipInfo = new ScreenClipInfo { X = 0, Y = 0, Scale = 1 };
        public static void Clip(ScreenClipInfo _screenClipInfo)
        {
            screenClipInfo = _screenClipInfo;
        }
        public static bool IsClip()
        {
            return screenClipInfo.Scale != 0;
        }
        public static void CalcClip(int sourceWidth, int sourceHeight,  out int left, out int top, out int width, out int height)
        {
            CalcClip(sourceWidth, sourceHeight, screenClipInfo.X, screenClipInfo.Y, screenClipInfo.Scale, out left, out top, out width, out height);

        }
        public static void CalcClip(int sourceWidth, int sourceHeight,int x,int y,float scale, out int left, out int top, out int width, out int height)
        {
            //缩放后宽高
            int newSourceWidth = (int)(sourceWidth * screenClipInfo.Scale);
            int newSourceHeight = (int)(sourceHeight * screenClipInfo.Scale);

            //减去的宽高
            int clipWidth = (int)((newSourceWidth - sourceWidth) * 1.0 / newSourceWidth * sourceWidth);
            int clipHeight = (int)((newSourceHeight - sourceHeight) * 1.0 / newSourceHeight * sourceHeight);
            //留下的宽高
            width = sourceWidth - clipWidth;
            height = sourceHeight - clipHeight;

            float scaleX = screenClipInfo.X * 1.0f / sourceWidth;
            float scaleY = screenClipInfo.Y * 1.0f / sourceHeight;

            left = (int)(clipWidth * scaleX);
            top = (int)(clipHeight * scaleY);

        }

        public static void Return(byte[] bytes)
        {
            ArrayPool<byte>.Shared.Return(bytes);
        }

        public static bool GetScale(out float x, out float y, out int sourceWidth, out int sourceHeight)
        {
            x = 1;
            y = 1;
            sourceWidth = 0;
            sourceHeight = 0;
            IntPtr hdc = GetDC(IntPtr.Zero);
            if (hdc != IntPtr.Zero)
            {
                sourceWidth = GetDeviceCaps(hdc, DESKTOPHORZRES);
                sourceHeight = GetDeviceCaps(hdc, DESKTOPVERTRES);
                int screenWidth = GetSystemMetrics(SM_CXSCREEN);
                int screenHeight = GetSystemMetrics(SM_CYSCREEN);

                x = (sourceWidth * 1.0f / screenWidth);
                y = (sourceHeight * 1.0f / screenHeight);

                ReleaseDC(IntPtr.Zero, hdc);

                return true;
            }
            return false;
        }
        public static bool GetNewSize(int sourceWidth, int sourceHeight, float scaleX, float scaleY, float configScale, out int width, out int height)
        {
            width = (int)(sourceWidth * 1.0 / scaleX * configScale);
            height = (int)(sourceHeight * 1.0 / scaleY * configScale);
            return true;
        }
        public static void DrawCursorIcon(Graphics g, int sourceWidth, int scaleX, int scaleY, float configScale)
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
                        DrawIconEx(hdc1, pci.ptScreenPos.x * scaleX, pci.ptScreenPos.y * scaleY, pci.hCursor, curWidth, curWidth, 0, IntPtr.Zero, DI_NORMAL);
                        g.ReleaseHdc();
                    }
                }
            }
        }


        private const int DESKTOPVERTRES = 117;
        private const int DESKTOPHORZRES = 118;
        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr ptr);

        [DllImport("user32.dll", EntryPoint = "ReleaseDC")]
        public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);

        [DllImport("gdi32.dll", EntryPoint = "GetDeviceCaps", SetLastError = true)]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetSystemMetrics")]
        private static extern int GetSystemMetrics(int mVal);


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
        private const Int32 CURSOR_SHOWING = 0x0001;
        private const Int32 DI_NORMAL = 0x0003;

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon, int cxWidth, int cyHeight, int istepIfAniCur, IntPtr hbrFlickerFreeDraw, int diFlags);
    }
}
