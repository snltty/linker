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
        private uint lastInput;
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
            report.LT = GetLastInputInfo();
            if (report.LT < lastInput || report.LT - lastInput > 1000)
            {
                lastInput = report.LT;
                return report;
            }
            return null;
        }


        #region 截图
        private ScreenReportType screenReportType = ScreenReportType.None;
        private long ticks = 0;
        public void Full()
        {
            ticks = DateTime.UtcNow.Ticks;
            screenReportType = ScreenReportType.Full;
        }
        public void Clip(ScreenClipInfo screenClipInfo)
        {
            GdiCapture.Clip(screenClipInfo);
        }
        public void Region()
        {
            ticks = DateTime.UtcNow.Ticks;
            screenReportType = ScreenReportType.Region;
        }

        private void ScreenCaptureTask()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    int delayms = 0;
                    bool res = clientSignInState.Connected == true
                    && screenReportType > ScreenReportType.None
                    && (DateTime.UtcNow.Ticks - ticks) / TimeSpan.TicksPerMillisecond < 1000;
                    if (res)
                    {
                        try
                        {
                            long start = DateTime.UtcNow.Ticks;
                            await SendScreenCapture();
                            delayms = (int)((DateTime.UtcNow.Ticks - start) / TimeSpan.TicksPerMillisecond);
                        }
                        catch (Exception ex)
                        {
                            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                Logger.Instance.Error(ex);
                        }
                    }
                    if (delayms < config.ScreenDelay)
                    {
                        Thread.Sleep(config.ScreenDelay - delayms);
                    }
                    else
                    {
                        Thread.Sleep(config.ScreenDelay);
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }
        private async Task SendScreenCapture()
        {
            //var sw = new Stopwatch();
            // sw.Start();
            DesktopFrame frame = desktopDuplicator.GetLatestFrame(screenReportType, config.ScreenScale);
            // sw.Stop();
            // Console.WriteLine($"{frame.FullImage.Length}->{sw.ElapsedMilliseconds}");
            if (frame != null)
            {
                if (frame.FullImage.Length > 0)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = clientSignInState.Connection,
                        MessengerId = (ushort)ScreenMessengerIds.FullReport,
                        Payload = frame.FullImage,
                    });
                }
                else if (frame.RegionImage.Length > 0)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = clientSignInState.Connection,
                        MessengerId = (ushort)ScreenMessengerIds.RegionReport,
                        Payload = frame.RegionImage,
                    });
                }
                /*
                if (frame.Updateds.Length > 0)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = clientSignInState.Connection,
                        MessengerId = (ushort)ScreenMessengerIds.Rectangles,
                        Payload = MemoryPackSerializer.Serialize(frame.Updateds),
                    });
                }
                */
            }

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
        public uint LT { get; set; }
    }

    public enum ScreenReportType : byte
    {
        None = 0,
        Full = 1,
        Region = 2
    };

    [MemoryPackable]
    public sealed partial class ScreenClipInfo
    {
        public int X { get; set; }
        public int Y { get; set; }
        public float Scale { get; set; }

    }
}
