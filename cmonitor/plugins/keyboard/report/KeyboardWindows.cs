using cmonitor.config;
using cmonitor.libs;
using common.libs;
using common.libs.helpers;
using common.libs.winapis;
using System.Collections.Concurrent;

namespace cmonitor.plugins.keyboard.report
{
    public sealed class KeyboardWindows : IKeyboard
    {
        private readonly Config config;
        private readonly ShareMemory shareMemory;
        public KeyboardWindows(Config config, ShareMemory shareMemory)
        {
            this.config = config;
            this.shareMemory = shareMemory;
            CheckQueue();
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
                shareMemory.Update((int)ShareMemoryIndexs.Sas, "cmonitor.sas.service", "ctrl+alt+delete");
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
            Task.Run(async () =>
            {
                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    if (inputActions.IsEmpty == false)
                    {
                        try
                        {

                            if (inputActions.TryDequeue(out var action))
                            {
                                if (config.Data.Elevated == true && !Win32Interop.SwitchToInputDesktop())
                                {
                                    uint code = Kernel32.GetLastError();
                                    cancellationTokenSource.Cancel();
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
                        await Task.Delay(10);
                    }
                }
            });
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
