using cmonitor.server.client.reports.share;
using common.libs;
using common.libs.helpers;
using common.libs.winapis;
using System.Collections.Concurrent;

namespace cmonitor.server.client.reports.keyboard
{
    public sealed class KeyboardWindows : IKeyboard
    {
        private readonly Config config;
        private readonly ShareReport shareReport;
        public KeyboardWindows(Config config, ShareReport shareReport)
        {
            this.config = config;
            this.shareReport = shareReport;
            if (config.IsCLient)
            {
                CheckQueue();
            }
        }

        public void KeyBoard(KeyBoardInputInfo inputInfo)
        {
            KeyBoardInputInfo _inputInfo = inputInfo;
            TryOnInputDesktop(() =>
            {
                User32.keybd_event(_inputInfo.Key, (byte)User32.MapVirtualKey(_inputInfo.Key, 0), _inputInfo.Type, 0);
            });
        }
        public void MouseSet(MouseSetInfo setInfo)
        {
            MouseSetInfo _setInfo = setInfo;
            TryOnInputDesktop(() =>
            {
                User32.SetCursorPos(_setInfo.X, _setInfo.Y);
                MouseHelper.MouseSet(_setInfo.X, _setInfo.Y);
            });
        }
        public void MouseClick(MouseClickInfo clickInfo)
        {
            MouseClickInfo _clickInfo = clickInfo;
            TryOnInputDesktop(() =>
            {
                MouseHelper.MouseClick(_clickInfo.Flag, _clickInfo.Data);
            });
        }

        public void CtrlAltDelete()
        {
            try
            {
                shareReport.UpdateShare(new ShareItemInfo
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
        public void WinL()
        {
            User32.LockWorkStation();
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
}
