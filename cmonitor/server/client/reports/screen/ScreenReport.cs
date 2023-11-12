using cmonitor.server.service;
using common.libs;
using cmonitor.server.service.messengers.screen;
using MemoryPack;
using cmonitor.server.client.reports.screen.helpers;
using cmonitor.server.client.reports.screen.winapiss;
using Microsoft.Win32;
using System.Runtime.InteropServices;

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

        private DisplayInfo[] displays;

        public ScreenReport(ClientSignInState clientSignInState, MessengerSender messengerSender, Config config)
        {
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
            this.config = config;
            if (config.IsCLient)
            {
                ScreenCaptureTask();
                dxgiDesktop = new DxgiDesktop(0, config);
                gdiDesktop = new GdiDesktop(config);
                InitSise();
            }

        }
        private void InitSise()
        {
            displays = DisplaysEnumerationHelper.GetDisplays();
            if (DisplayHelper.GetSystemScale(out _, out _, out int w, out int h))
            {
                report.W = w;
                report.H = h;
            }
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

            report.LT = LastInputHelper.GetLastInputInfo();
            if (reportType == ReportType.Full || report.LT < lastInput || report.LT - lastInput > 1000)
            {
                lastInput = report.LT;
                return report;
            }
            return null;
        }
        public void MonitorState(bool onState)
        {
            if (onState)
            {
                DisplayHelper.On();
            }
            else
            {
                DisplayHelper.Off();
            }

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
                        await Task.Delay(config.ScreenDelay - delayms);
                    }
                    else
                    {
                        await Task.Delay(config.ScreenDelay);
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
                frame = gdiDesktop.GetLatestFrame();
            }
            else if (screenReportType == ScreenReportType.Full)
            {
                frame = dxgiDesktop.GetLatestFullFrame();
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
                frame = dxgiDesktop.GetLatestRegionFrame();
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
            if (frame.UpdatedRegions != null)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)ScreenMessengerIds.Rectangles,
                    Payload = MemoryPackSerializer.Serialize(frame.UpdatedRegions),
                });
            }
        }


        private void RandomCursorPos()
        {
            if (config.WakeUp == false) return;
            try
            {
                if (CursorHelper.GetCursorPosition(out int x, out int y))
                {
                    User32.SetCursorPos(x + 1, y + 1);
                    MouseHelper.MouseMove(1, 1);
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


        private Thread messageLoopThread;
        private void MessageLoop()
        {
            if (messageLoopThread is not null)
            {
                return;
            }
            messageLoopThread = new Thread(() =>
            {
                //SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
                // SystemEvents.SessionEnding += SystemEvents_SessionEnding;
                // SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

                while (true)
                {
                    try
                    {
                        while (GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
                        {
                            //DispatchMessage(ref msg);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error(ex);
                    }
                }
            });
            if (OperatingSystem.IsWindows())
            {
                messageLoopThread.SetApartmentState(ApartmentState.STA);
            }
            messageLoopThread.Start();
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }
        }
        [DllImport("user32.dll")]
        private static extern bool DispatchMessage([In] ref MSG lpmsg);
        [DllImport("user32.dll")]
        private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
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
