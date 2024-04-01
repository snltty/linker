using common.libs;
using MemoryPack;
using common.libs.helpers;
using cmonitor.plugins.screen.messenger;
using cmonitor.client.report;
using cmonitor.client;
using cmonitor.server;
using cmonitor.config;

namespace cmonitor.plugins.screen.report
{
    public sealed class ScreenReport : IClientReport
    {

        public string Name => "Screen";
        private readonly ClientSignInState clientSignInState;
        private readonly MessengerSender messengerSender;
        private readonly Config config;
        private readonly IScreen screen;

        private ScreenReportInfo report = new ScreenReportInfo();
        private uint lastInput;
        private DisplayInfo[] displays;

        public ScreenReport(ClientSignInState clientSignInState, MessengerSender messengerSender, Config config, IScreen screen)
        {
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
            this.config = config;
            this.screen = screen;

            CaptureTask();
            displays = screen.GetDisplays(out int w, out int h);
            report.W = w;
            report.H = h;
        }

        public object GetReports(ReportType reportType)
        {
            if (reportType == ReportType.Full)
            {
                report.Displays = displays;
            }
            else
            {
                report.Displays = Array.Empty<DisplayInfo>();
            }
            report.LT = screen.GetLastInputTime();
            if (reportType == ReportType.Full || report.LT < lastInput || report.LT - lastInput > 1000)
            {
                lastInput = report.LT;
                return report;
            }
            return null;
        }

        private ScreenReportType screenReportType = ScreenReportType.Full;
        private ScreenReportFullType screenReportFullType = ScreenReportFullType.Full | ScreenReportFullType.Trim;
        private long ticks = 0;
        public void SetCaptureFull(ScreenReportFullType screenReportFullType)
        {
            ticks = DateTime.UtcNow.Ticks;
            screenReportType = ScreenReportType.Full;
            this.screenReportFullType |= screenReportFullType;
        }
        public void SetCaptureClip(ScreenClipInfo screenClipInfo)
        {
            ticks = DateTime.UtcNow.Ticks;
            screenReportType = ScreenReportType.Full;
            screen.Clip(screenClipInfo);
        }
        public void SetCaptureRegion()
        {
            ticks = DateTime.UtcNow.Ticks;
            screenReportType = ScreenReportType.Region;
        }


        private Memory<byte> fullImageMemory = Helper.EmptyArray;
        private Task task;
        private void CaptureTask()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    bool connected = clientSignInState.Connected == true;
                    bool time = (DateTime.UtcNow.Ticks - ticks) / TimeSpan.TicksPerMillisecond < 1000;
                    if (connected && time && (task == null || task.IsCompleted))
                    {
                        try
                        {
                            task = CaptureFrame();
                        }
                        catch (Exception ex)
                        {
                            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                Logger.Instance.Error(ex);
                        }
                    }

                    await Task.Delay(200);
                }
            }, TaskCreationOptions.LongRunning);
        }
        private async Task CaptureFrame()
        {
            DesktopFrame frame = CaptureGetFrame();
            await CaptureSendFrame(frame);
        }
        private DesktopFrame CaptureGetFrame()
        {
            DesktopFrame frame = null;

            long ticks = DateTime.UtcNow.Ticks;
            if (screen.IsClip())
            {
                frame = screen.GetClipFrame();
            }
            else if (screenReportType == ScreenReportType.Full)
            {
                frame = screen.GetFullFrame();
                if (frame.FullImage.Length > 0)
                {
                    fullImageMemory = frame.FullImage;
                }
                else if ((screenReportFullType & ScreenReportFullType.Full) == ScreenReportFullType.Full)
                {
                    if (fullImageMemory.Length > 0)
                    {
                        frame.FullImage = fullImageMemory;
                    }
                    else
                    {
                        RandomCursorPos();
                    }
                }
                screenReportFullType &= ~ScreenReportFullType.Full;
            }
            else if (screenReportType == ScreenReportType.Region)
            {
                frame = screen.GetRegionFrame();
            }
            report.CT = (uint)((DateTime.UtcNow.Ticks - ticks) / TimeSpan.TicksPerMillisecond);

            return frame;
        }
        private async Task CaptureSendFrame(DesktopFrame frame)
        {
            if (frame == null)
            {
                return;
            }

            if (frame.FullImage.Length > 0)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)ScreenMessengerIds.CaptureFullReport,
                    Payload = frame.FullImage,
                });
            }
            else if (frame.RegionImage.Length > 0)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)ScreenMessengerIds.CaptureRegionReport,
                    Payload = frame.RegionImage,
                });
            }
            if (frame.UpdatedRegions != null)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)ScreenMessengerIds.CaptureRectangles,
                    Payload = MemoryPackSerializer.Serialize(frame.UpdatedRegions),
                });
            }
        }
        private void RandomCursorPos()
        {
            screen.WakeUp();
        }
    }

    public sealed class ScreenReportInfo
    {
        public uint CT { get; set; }
        public uint LT { get; set; }
        public int W { get; set; }
        public int H { get; set; }

        public DisplayInfo[] Displays { get; set; } = Array.Empty<DisplayInfo>();
    }
    public enum ScreenReportType : byte
    {
        Full = 1,
        Region = 2
    };
    public enum ScreenReportFullType : byte
    {
        Full = 1,
        Trim = 2
    };

    [MemoryPackable]
    public sealed partial class ScreenClipInfo
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
    }

   
}
