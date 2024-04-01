using common.libs;

namespace cmonitor.plugins.notify.report
{
    public sealed class NotifyWindows : INotify
    {
        public NotifyWindows()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                CommandHelper.Windows(string.Empty, new string[] { "taskkill /f /im \"notify.win.exe\"" }, true);
            };
            Console.CancelKeyPress += (sender, e) =>
            {
                CommandHelper.Windows(string.Empty, new string[] { "taskkill /f /im \"notify.win.exe\"" }, true);
            };
        }
        public void Update(NotifyInfo notify)
        {
            Task.Run(() =>
            {
                CommandHelper.Windows(string.Empty, new string[] {
                        $"start cmonitor.notify.win.exe {notify.Speed} \"{notify.Msg}\" {notify.Star1} {notify.Star2} {notify.Star3}"
                    }, false);
            });
        }
    }
}
