using cmonitor.server.service;
using common.libs;
using cmonitor.server.service.messengers.screen;
using System.Runtime.InteropServices;
using System.Buffers;
#if DEBUG || RELEASE
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO.MemoryMappedFiles;
#endif
using System.Text;

namespace cmonitor.server.client.reports.screen
{
    public sealed class ScreenReport : IReport
    {
        public string Name => "Screen";
        private readonly ClientSignInState clientSignInState;
        private readonly MessengerSender messengerSender;
        private readonly Config config;

        Dictionary<string, object> dic = new Dictionary<string, object> { { "LastInput", 0 }, { "UserName", string.Empty }, { "KeyBoard", string.Empty } };

        public ScreenReport(ClientSignInState clientSignInState, MessengerSender messengerSender, Config config)
        {
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
            this.config = config;

            if (config.IsCLient)
            {
                ScreenCaptureTask();
                InitUserNameMemory();
                InitLastInputInfo();
                InitKeyBoard();
            }
        }
        public Dictionary<string, object> GetReports()
        {
            dic["LastInput"] = GetLastInputInfo();
            dic["UserName"] = GetUserNameMemory();
            dic["KeyBoard"] = GetKeyBoard();
            return dic;
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
                    await Task.Delay(Config.ScreenTime);
                }
            }, TaskCreationOptions.LongRunning);
        }
        private async Task SendScreenCapture()
        {
            byte[] bytes = ScreenCapture();
            if (bytes.Length > 0)
            {

                // byte[] bytes = MemoryPackSerializer.Serialize(img);
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


                int newWidth = 384;
                int newHeight = (int)(source.Height * (newWidth * 1.0 / source.Width));
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
                //string base64 = Convert.ToBase64String(bytes, 0, (int)ms.Length);
                //ArrayPool<byte>.Shared.Return(bytes);
                //return base64;

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

        #region 用户名

#if DEBUG || RELEASE
        MemoryMappedFile mmf2;
        MemoryMappedViewAccessor accessor2;
        byte[] userNameBytes;
#endif
        private void InitUserNameMemory()
        {
#if DEBUG || RELEASE
            userNameBytes = new byte[config.UserNameMemoryLength];
            if (OperatingSystem.IsWindows())
            {
                mmf2 = MemoryMappedFile.CreateOrOpen(config.UserNameMemoryKey, userNameBytes.Length);
                accessor2 = mmf2.CreateViewAccessor();
            }
#endif
        }

        private string GetUserNameMemory()
        {
#if DEBUG || RELEASE
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    if (accessor2 != null)
                    {
                        accessor2.Read(0, out byte length);
                        if (length > 0)
                        {
                            accessor2.ReadArray(1, userNameBytes, 0, length);
                            return Encoding.UTF8.GetString(userNameBytes.AsSpan(0, length));
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
#endif
            return string.Empty;
        }
        #endregion

        #region 键盘

#if DEBUG || RELEASE
        MemoryMappedFile mmf3;
        MemoryMappedViewAccessor accessor3;
        byte[] keyBoardBytes;
#endif
        private void InitKeyBoard()
        {
#if DEBUG || RELEASE
            keyBoardBytes = new byte[config.KeyboardMemoryLength];
            if (OperatingSystem.IsWindows())
            {
                mmf3 = MemoryMappedFile.CreateOrOpen(config.KeyboardMemoryKey, keyBoardBytes.Length);
                accessor3 = mmf3.CreateViewAccessor();
            }
#endif
        }
        private string GetKeyBoard()
        {
#if DEBUG || RELEASE
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    if (accessor3 != null)
                    {
                        accessor3.Read(0, out byte length);
                        if (length > 0)
                        {
                            accessor3.ReadArray(1, keyBoardBytes, 0, length);
                            return Encoding.UTF8.GetString(keyBoardBytes.AsSpan(0, length));
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
#endif
            return string.Empty;
        }
        #endregion
    }

}
