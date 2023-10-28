using cmonitor.server.service;
using common.libs;
using cmonitor.server.service.messengers.screen;
using MemoryPack;
using System.Runtime.InteropServices;
using static cmonitor.server.client.reports.screen.WinApi;

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
        private readonly DxgiDesktop dxgiDesktop;
        private readonly GdiDesktop gdiDesktop;

        public ScreenReport(ClientSignInState clientSignInState, MessengerSender messengerSender, Config config)
        {
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
            this.config = config;
            if (config.IsCLient)
            {
                ScreenCaptureTask();
                WinApi.InitLastInputInfo();
                dxgiDesktop = new DxgiDesktop(0);
                gdiDesktop = new GdiDesktop();
                InitSise();
                //InitMOnitors();
            }

        }
        private void InitSise()
        {
            if (WinApi.GetSystemScale(out _, out _, out int w, out int h))
            {
                report.W = w;
                report.H = h;
            }
        }
        private void InitMOnitors()
        {
            MonitorEnumProc callback = new MonitorEnumProc(MonitorEnumCallback);
           WinApi.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
        }
        private bool MonitorEnumCallback(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
        {
            MONITORINFO mi = new MONITORINFO();
            mi.cbSize = (uint)Marshal.SizeOf(mi);

            if (GetMonitorInfo(hMonitor, ref mi))
            {
                // 检查显示器状态
                if ((mi.dwFlags & 1) == 0) // 1表示显示器已关闭
                {
                    Console.WriteLine("Display is closed.");
                }
                else
                {
                    Console.WriteLine("Display is not closed.");
                }
            }

            return true;
        }


        public object GetReports(ReportType reportType)
        {
            report.LT = WinApi.GetLastInputInfo();
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
        public void Full(ScreenReportFullType screenReportFullType)
        {
            ticks = DateTime.UtcNow.Ticks;
            screenReportType = ScreenReportType.Full;
            this.screenReportFullType |= screenReportFullType;
        }
        public void Clip(ScreenClipInfo screenClipInfo)
        {
            ticks = DateTime.UtcNow.Ticks;
            screenReportType = ScreenReportType.Full;
            gdiDesktop.Clip(screenClipInfo);
        }
        public void Region()
        {
            ticks = DateTime.UtcNow.Ticks;
            screenReportType = ScreenReportType.Region;
        }

        private void ScreenCaptureTask()
        {
            if (OperatingSystem.IsWindows() == false)
            {
                return;
            }
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    int delayms = 0;
                    if (clientSignInState.Connected == true && ((DateTime.UtcNow.Ticks - ticks) / TimeSpan.TicksPerMillisecond < 1000))
                    {
                        try
                        {
                            long start = DateTime.UtcNow.Ticks;
                            await ScreenCapture();
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
        private Memory<byte> fullImageMemory = Helper.EmptyArray;
        private async Task ScreenCapture()
        {
            DesktopFrame frame = GetFrame();
            await SendFrame(frame);
        }
        private DesktopFrame GetFrame()
        {
            DesktopFrame frame = null;

            long ticks = DateTime.UtcNow.Ticks;
            if (gdiDesktop.IsClip())
            {
                frame = gdiDesktop.GetLatestFrame(config.ScreenScale);
            }
            else if (screenReportType == ScreenReportType.Full)
            {
                frame = dxgiDesktop.GetLatestFullFrame(config.ScreenScale);
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
                    RandomCursorPos();
                }
                screenReportFullType &= ~ScreenReportFullType.Full;
            }
            else if (screenReportType == ScreenReportType.Region)
            {
                frame = dxgiDesktop.GetLatestRegionFrame(config.ScreenScale);
            }
            report.CT = (uint)((DateTime.UtcNow.Ticks - ticks) / TimeSpan.TicksPerMillisecond);

            return frame;
        }
        private async Task SendFrame(DesktopFrame frame)
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
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)ScreenMessengerIds.Rectangles,
                Payload = MemoryPackSerializer.Serialize(frame.UpdatedRegions),
            });
        }


        private void RandomCursorPos()
        {
            if (config.WakeUp == false) return;
            try
            {
                if (WinApi.GetCursorPosition(out int x, out int y))
                {
                    WinApi.SetCursorPos(x + 1, y + 1);
                    WinApi.MouseMove(1, 1);
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
            }
        }

    }

    public sealed class ScreenReportInfo
    {
        public uint CT { get; set; }
        public uint LT { get; set; }
        public int W { get; set; }
        public int H { get; set; }
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
