using cmonitor.server.service;
using common.libs;
using cmonitor.server.service.messengers.screen;
using System.Runtime.InteropServices;
using MemoryPack;
using cmonitor.server.client.reports.screen.sharpDX;
using System.Diagnostics;

namespace cmonitor.server.client.reports.screen
{
    public sealed class ScreenReport : IReport
    {

        public string Name => "Screen";
        private readonly ClientSignInState clientSignInState;
        private readonly MessengerSender messengerSender;
        private readonly Config config;

        private ScreenReportInfo report = new ScreenReportInfo();
        private readonly DesktopDuplicator desktopDuplicator;

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
            desktopDuplicator = new DesktopDuplicator(0);
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
            //var sw = new Stopwatch();
            //sw.Start();
            byte[] bytes = ScreenCapture2(out int length);
            //sw.Stop();
            //Console.WriteLine($"{bytes.Length}->{sw.ElapsedMilliseconds}");
            if (length > 0)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)ScreenMessengerIds.Report,
                    Payload = bytes.AsMemory(0, length),
                });
                GdiCapture.Return(bytes);
            }
        }

        private byte[] ScreenCapture(out int length)
        {
            length = 0;
#if DEBUG || RELEASE
            return GdiCapture.Capture(config.ScreenScale, out length);
#else
            return Array.Empty<byte>();
#endif
        }
        private byte[] ScreenCapture2(out int length)
        {
            length = 0;
#if DEBUG || RELEASE
            DesktopFrame frame = desktopDuplicator.GetLatestFrame(config.ScreenScale, out length);
            return frame.DesktopImage;
#else
            return Array.Empty<byte>();
#endif
        }


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
