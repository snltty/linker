using common.libs;

namespace cmonitor.server.client.reports.llock
{
    public sealed class LLockReport : IReport
    {
        public string Name => "LLock";

        private Dictionary<string, object> report = new Dictionary<string, object>() { { "Value", false } };
        public Dictionary<string, object> GetReports()
        {
            report["Value"] = WindowHelper.GetHasWindowByName("llock.win");
            return report;
        }

        public void Update(bool open)
        {
            if (open)
            {
                Task.Run(() =>
                {
                    CommandHelper.Windows(string.Empty, new string[] {
                        $"start llock.win.exe"
                    });
                });
            }
            else
            {
                CommandHelper.Windows(string.Empty, new string[] {
                        "taskkill /f /t /im \"llock.win.exe\""
                    });
            }
        }
    }
}
