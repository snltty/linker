using cmonitor.client.report;
using cmonitor.config;

namespace cmonitor.plugins.watch.report
{
    public sealed class WatchReport : IClientReport
    {
        public string Name => "Watch";

        public WatchReport(Config config)
        {
#if RELEASE
        OpenFiles();        
#endif
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
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
