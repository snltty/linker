using cmonitor.server.service;
using common.libs;
using cmonitor.server.service.messengers.screen;
using System.Runtime.InteropServices;
using System.Buffers;
using MemoryPack;
#if DEBUG || RELEASE
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;
#endif

namespace cmonitor.server.client.reports.screen
{
    public sealed class ScreenReport : IReport
    {

        public string Name => "Screen";
        private readonly ClientSignInState clientSignInState;
        private readonly MessengerSender messengerSender;
        private readonly Config config;

        private ScreenReportInfo report = new ScreenReportInfo();

        public ScreenReport(ClientSignInState clientSignInState, MessengerSender messengerSender, Config config)
        {
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
            this.config = config;
            if (config.IsCLient)
            {
                ScreenCaptureTask();
                InitLastInputInfo();
            }
        }
        public object GetReports()
        {
            report.LastInput = GetLastInputInfo();
            return report;
        }


        #region 截图
        private uint screenCaptureFlag = 0;
        public void Update()
        {
            Interlocked.CompareExchange(ref screenCaptureFlag, 1, 0);
        }
        private void ScreenCaptureTask()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (clientSignInState.Connected == true && Interlocked.CompareExchange(ref screenCaptureFlag, 0, 1) == 1)
                    {
                        try
                        {
                            await SendScreenCapture();
                        }
                        catch (Exception ex)
                        {
                            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                Logger.Instance.Error(ex);
                        }
                    }
                    await Task.Delay(config.ScreenDelay);
                }
            }, TaskCreationOptions.LongRunning);
        }
        private async Task SendScreenCapture()
        {
            byte[] bytes = ScreenCapture(out int length);
            if (length > 0)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)ScreenMessengerIds.Report,
                    Payload = bytes.AsMemory(0, length),
                });
                Return(bytes);
            }
        }


        private byte[] ScreenCapture(out int length)
        {
            length = 0;
#if DEBUG || RELEASE
            if (OperatingSystem.IsWindows())
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                if (hdc != IntPtr.Zero)
                {
                    int sourceWidth = GetDeviceCaps(hdc, DESKTOPHORZRES);
                    int sourceHeight = GetDeviceCaps(hdc, DESKTOPVERTRES);
                    int screenWidth = GetSystemMetrics(SM_CXSCREEN);
                    int screenHeight = GetSystemMetrics(SM_CYSCREEN);

                    int scaleX = (int)(sourceWidth * 1.0 / screenWidth);
                    int scaleY = (int)(sourceHeight * 1.0 / screenHeight);

                    int newWidth = (int)(sourceWidth * 1.0 / scaleX * config.ScreenScale);
                    int newHeight = (int)(sourceHeight * 1.0 / scaleY * config.ScreenScale);
                    int curWidth = (int)(sourceWidth * config.ScreenScale * config.ScreenScale);

                    using Bitmap source = new Bitmap(sourceWidth, sourceHeight);
                    using (Graphics g = Graphics.FromImage(source))
                    {
                        g.CopyFromScreen(0, 0, 0, 0, source.Size, CopyPixelOperation.SourceCopy);

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

                        g.Dispose();
                    }
                    ReleaseDC(IntPtr.Zero, hdc);



                    Bitmap bmp = new Bitmap(newWidth, newHeight);
                    bmp.SetResolution(source.HorizontalResolution, source.VerticalResolution);
                    using Graphics graphic = Graphics.FromImage(bmp);
                    graphic.SmoothingMode = SmoothingMode.HighQuality;
                    graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphic.DrawImage(source, new Rectangle(0, 0, newWidth, newHeight));

                    using Image image = bmp;

                    using MemoryStream ms = new MemoryStream();
                    image.Save(ms, ImageFormat.Jpeg);
                    ms.Seek(0, SeekOrigin.Begin);

                    length = (int)ms.Length;

                    byte[] bytes = ArrayPool<byte>.Shared.Rent((int)ms.Length);
                    ms.Read(bytes);
                    return bytes;
                }
            }
#endif
            return Array.Empty<byte>();
        }
        /*
        Bitmap previousFrame;
        private Bitmap DiffArea(Bitmap currentFrame, out int x, out int y)
        {
            Bitmap bmp;
            x = 0; y = 0;
            if (previousFrame == null)
            {
                int newWidth = (int)(currentFrame.Width * config.ScreenScale);
                int newHeight = (int)(currentFrame.Height * config.ScreenScale);
                bmp = new Bitmap(newWidth, newHeight);
            }
            else
            {
                Rectangle rectangle = DiffArea(currentFrame, previousFrame);
                bmp = new Bitmap(rectangle.Width, rectangle.Height);
                x = rectangle.X;
                y = rectangle.X;
                previousFrame.Dispose();
                previousFrame = null;
            }

            bmp.SetResolution(currentFrame.HorizontalResolution, currentFrame.VerticalResolution);
            using Graphics graphic = Graphics.FromImage(bmp);
            graphic.SmoothingMode = SmoothingMode.HighQuality;
            graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
            Rectangle rectangle1 = new Rectangle(x, y, bmp.Width, bmp.Height);
            graphic.DrawImage(currentFrame, rectangle1);

            //previousFrame = currentFrame;

            return bmp;
        }
        private Rectangle DiffArea(Bitmap currentFrame, Bitmap previousFrame)
        {
            if (currentFrame.Height != previousFrame.Height || currentFrame.Width != previousFrame.Width)
            {
                throw new Exception("Bitmaps are not of equal dimensions.");
            }
            if (!Bitmap.IsAlphaPixelFormat(currentFrame.PixelFormat) || !Bitmap.IsAlphaPixelFormat(previousFrame.PixelFormat) ||
                !Bitmap.IsCanonicalPixelFormat(currentFrame.PixelFormat) || !Bitmap.IsCanonicalPixelFormat(previousFrame.PixelFormat))
            {
                throw new Exception("Bitmaps must be 32 bits per pixel and contain alpha channel.");
            }
            var width = currentFrame.Width;
            var height = currentFrame.Height;
            int left = int.MaxValue;
            int top = int.MaxValue;
            int right = int.MinValue;
            int bottom = int.MinValue;

            BitmapData bd1 = null;
            BitmapData bd2 = null;

            try
            {
                bd1 = previousFrame.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, currentFrame.PixelFormat);
                bd2 = currentFrame.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, previousFrame.PixelFormat);

                var bytesPerPixel = Bitmap.GetPixelFormatSize(currentFrame.PixelFormat) / 8;
                var totalSize = bd1.Height * bd1.Width * bytesPerPixel;

                unsafe
                {
                    byte* scan1 = (byte*)bd1.Scan0.ToPointer();
                    byte* scan2 = (byte*)bd2.Scan0.ToPointer();

                    for (int counter = 0; counter < totalSize - bytesPerPixel; counter += bytesPerPixel)
                    {
                        byte* data1 = scan1 + counter;
                        byte* data2 = scan2 + counter;

                        if (data1[0] != data2[0] ||
                            data1[1] != data2[1] ||
                            data1[2] != data2[2] ||
                            data1[3] != data2[3])
                        {
                            var pixel = counter / 4;
                            var row = (int)Math.Floor((double)pixel / bd1.Width);
                            var column = pixel % bd1.Width;
                            if (row < top)
                            {
                                top = row;
                            }
                            if (row > bottom)
                            {
                                bottom = row;
                            }
                            if (column < left)
                            {
                                left = column;
                            }
                            if (column > right)
                            {
                                right = column;
                            }
                        }
                    }
                }

                if (left < right && top < bottom)
                {
                    left = Math.Max(left - 10, 0);
                    top = Math.Max(top - 10, 0);
                    right = Math.Min(right + 10, width);
                    bottom = Math.Min(bottom + 10, height);

                    return new Rectangle(left, top, right - left, bottom - top);
                }
                else
                {
                    return Rectangle.Empty;
                }
            }
            catch
            {
                return Rectangle.Empty;
            }
            finally
            {
                currentFrame.UnlockBits(bd1);
                previousFrame.UnlockBits(bd2);
            }
        }
        */
        private void Return(byte[] bytes)
        {
            ArrayPool<byte>.Shared.Return(bytes);
        }


        private const int DESKTOPVERTRES = 117;
        private const int DESKTOPHORZRES = 118;
        private const int LOGPIXELSX = 88;
        private const int LOGPIXELSY = 90;
        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr ptr);

        [DllImport("user32.dll", EntryPoint = "ReleaseDC")]
        static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);

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

        #endregion

        #region 最后活动时间
        private LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
        private void InitLastInputInfo()
        {
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO));
        }
        private uint GetLastInputInfo()
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
    }

    public sealed class ScreenReportInfo
    {
        public uint LastInput { get; set; }
    }
    [MemoryPackable]
    public sealed partial class ScreenFrameInfo
    {
        public int X { get; set; }
        public int Y { get; set; }
        public byte[] Data { get; set; }

    }
}
