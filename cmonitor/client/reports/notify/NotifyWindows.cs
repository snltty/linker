using common.libs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmonitor.client.reports.notify
{
    public sealed class NotifyWindows : INotify
    {
        public void Update(NotifyInfo notify)
        {
            Task.Run(() =>
            {
                CommandHelper.Windows(string.Empty, new string[] {
                        $"start notify.win.exe {notify.Speed} \"{notify.Msg}\" {notify.Star1} {notify.Star2} {notify.Star3}"
                    });
            });
        }
    }
}
