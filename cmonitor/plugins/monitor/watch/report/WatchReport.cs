using cmonitor.client.report;
using cmonitor.config;
using common.libs;

namespace cmonitor.plugins.watch.report
{
    public sealed class WatchReport : IClientReport
    {
        public string Name => "Watch";

        public WatchReport(Config config)
        {
#if DEBUG
#else
        OpenFiles();        
#endif
            OpenFiles();
        }

        public object GetReports(ReportType reportType)
        {
            return null;
        }

        List<FileStream> fss = new List<FileStream>();
        private void OpenFiles()
        {
            foreach (var item in Directory.GetFiles("./"))
            {
                try
                {
                    fss.Add(new FileStream(item, FileMode.Open, FileAccess.Read, FileShare.Read));
                    Logger.Instance.Warning($"watch file {item}");
                }
                catch (Exception)
                {
                }
            }
            OpenFiles("./plugins");
        }

        private void OpenFiles(string path)
        {
            foreach (var item in Directory.GetFiles(path))
            {
                try
                {
                    fss.Add(new FileStream(item, FileMode.Open, FileAccess.Read, FileShare.Read));
                    Logger.Instance.Warning($"watch file {item}");
                }
                catch (Exception)
                {
                }
            }
            foreach (var item in Directory.GetDirectories(path))
            {
                OpenFiles(item);
            }
        }
    }
}
