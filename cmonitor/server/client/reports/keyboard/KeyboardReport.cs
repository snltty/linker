using cmonitor.server.client.reports.screen;
using cmonitor.server.client.reports.screen.winapis;
using cmonitor.server.client.reports.screen.winapiss;
using cmonitor.server.client.reports.share;
using common.libs;
using common.libs.extends;
using MemoryPack;
using System.Collections.Concurrent;

namespace cmonitor.server.client.reports.command
{
    public sealed class KeyboardReport : IReport
    {
        public string Name => "Keyboard";

        private readonly Config config;
        private readonly ShareReport shareReport;
        public KeyboardReport(Config config, ShareReport shareReport)
        {
            this.config = config;
            this.shareReport = shareReport;
            if (OperatingSystem.IsWindows() && config.IsCLient)
            {
                CheckQueue();
            }

        }
        public object GetReports(ReportType reportType)
        {
            if (reportType == ReportType.Full)
            {
            }
            return null;
        }

        public void KeyBoard(KeyBoardInputInfo inputInfo)
        {
            KeyBoardInputInfo _inputInfo = inputInfo;
            TryOnInputDesktop(() =>
            {
                User32.keybd_event(_inputInfo.Key, (byte)User32.MapVirtualKey(_inputInfo.Key, 0), _inputInfo.Type, 0);
            });
        }

        public const int KEYEVENTF_EXTENDEDKEY = 0x0000;
        public const int KEYEVENTF_KEYUP = 0x0002;
        public void CtrlAltDelete()
        {
            try
            {
                shareReport.Update(new ShareItemInfo
                {
                    Index = Config.ShareMemorySASIndex,
                    Key = "cmonitor.sas.service",
                    Value = "ctrl+alt+delete"
                });
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }

        private readonly ConcurrentQueue<Action> inputActions = new();
        private void CheckQueue()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew((a) =>
            {
                CancellationTokenSource tks = a as CancellationTokenSource;
                while (tks.IsCancellationRequested == false)
                {
                    if (inputActions.IsEmpty == false)
                    {
                        try
                        {

                            if (inputActions.TryDequeue(out var action))
                            {
                                if (config.Elevated == true && !Win32Interop.SwitchToInputDesktop())
                                {
                                    uint code = Kernel32.GetLastError();
                                    tks.Cancel();
                                    CheckQueue();
                                }
                                action();
                            }
                        }
                        finally
                        {
                        }
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
            }, cancellationTokenSource, TaskCreationOptions.LongRunning);
        }
        private void TryOnInputDesktop(Action inputAction)
        {
            inputActions.Enqueue(() =>
            {
                try
                {
                    inputAction();
                }
                catch (Exception ex)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        Logger.Instance.Error(ex);
                }
            });
        }

    }

    [MemoryPackable]
    public partial struct KeyBoardInputInfo
    {
        /// <summary>
        /// System.Windows.Forms.Keys
        /// </summary>
        public byte Key { get; set; }
        /// <summary>
        /// 0 down,2 up
        /// </summary>
        public int Type { get; set; }
    }

}
