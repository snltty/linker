using cmonitor.server.client.reports.screen.aforge;
using System.Buffers;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Image = System.Drawing.Image;

namespace cmonitor.server.client.reports.screen
{
    public sealed class GdiCapture
    {
        static ResizeBilinear resizeFilter;
        public static byte[] Capture(float configScale, out int length)
        {
            length = 0;
            if (GetScale(out int scaleX, out int scaleY, out int sourceWidth, out int sourceHeight) == false)
            {
                return Array.Empty<byte>();
            }
            if (OperatingSystem.IsWindows())
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                using Bitmap source = new Bitmap(sourceWidth, sourceHeight);
                using (Graphics g = Graphics.FromImage(source))
                {
                    g.CopyFromScreen(0, 0, 0, 0, source.Size, CopyPixelOperation.SourceCopy);

                    //DrawCursorIcon(g, sourceWidth, scaleX, scaleY, configScale);
                    g.Dispose();
                }
                ReleaseDC(IntPtr.Zero, hdc);

                GetNewSize(sourceWidth, sourceHeight, scaleX, scaleY, configScale, out int width, out int height);

                if (resizeFilter == null)
                {
                    resizeFilter = new ResizeBilinear(width, height);
                }
                Bitmap bmp = resizeFilter.Apply(source);

                using Image image = bmp;

                using MemoryStream ms = new MemoryStream();
                image.Save(ms, ImageFormat.Jpeg);
                ms.Seek(0, SeekOrigin.Begin);

                length = (int)ms.Length;

                byte[] bytes = ArrayPool<byte>.Shared.Rent((int)ms.Length);
                ms.Read(bytes);

                return bytes;
            }

            return Array.Empty<byte>();
        }

        public static void Return(byte[] bytes)
        {
            ArrayPool<byte>.Shared.Return(bytes);
        }

        public static bool GetScale(out int x, out int y, out int sourceWidth, out int sourceHeight)
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

                x = (int)(sourceWidth * 1.0 / screenWidth);
                y = (int)(sourceHeight * 1.0 / screenHeight);

                ReleaseDC(IntPtr.Zero, hdc);

                return true;
            }
            return false;
        }
        public static bool GetNewSize(int sourceWidth, int sourceHeight, int scaleX, int scaleY, float configScale, out int width, out int height)
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
