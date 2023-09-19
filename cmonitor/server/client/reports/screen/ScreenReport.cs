using cmonitor.server.service;
using common.libs;
using cmonitor.server.service.messengers.screen;
using System.Runtime.InteropServices;
using System.Buffers;
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
            byte[] bytes = ScreenCapture();
            if (bytes.Length > 0)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)ScreenMessengerIds.Report,
                    Payload = bytes,
                });
            }
            Return(bytes);
        }
        private byte[] ScreenCapture()
        {
#if DEBUG || RELEASE
            if (OperatingSystem.IsWindows())
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                if (hdc != IntPtr.Zero)
                {
                    using Bitmap source = new Bitmap(GetDeviceCaps(hdc, DESKTOPHORZRES), GetDeviceCaps(hdc, DESKTOPVERTRES));
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
                                DrawIconEx(hdc1, pci.ptScreenPos.x - 0, pci.ptScreenPos.y - 0, pci.hCursor, 0, 0, 0, IntPtr.Zero, DI_NORMAL);
                                g.ReleaseHdc();
                            }
                        }

                        g.Dispose();
                    }
                    ReleaseDC(IntPtr.Zero, hdc);


                    int newWidth = (int)(source.Width * config.ScreenScale);
                    int newHeight = (int)(source.Height * config.ScreenScale);
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

                    byte[] bytes = ArrayPool<byte>.Shared.Rent((int)ms.Length);
                    ms.Read(bytes);
                    return bytes;
                }
            }
#endif
            return Array.Empty<byte>();

        }
        private void Return(byte[] bytes)
        {
            ArrayPool<byte>.Shared.Return(bytes);
        }


        const int DESKTOPVERTRES = 117;
        const int DESKTOPHORZRES = 118;

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

}
