using common.libs;
using MemoryPack;
using System.Collections.Concurrent;

namespace cmonitor.server.client.reports.active
{
    public sealed class ActiveWindowReport : IReport
    {
        public string Name => "ActiveWindow";

        private readonly ClientConfig clientConfig;
        private readonly IActiveWindow activeWindow;
        private readonly ActiveWindowTimeManager activeWindowTimeManager = new ActiveWindowTimeManager();
        private ActiveReportInfo report = new ActiveReportInfo();

        private uint lastPid = 0;
        private string lastTitle = string.Empty;
        private int count = 0;
        public ActiveWindowReport(Config config, ClientConfig clientConfig, IActiveWindow activeWindow)
        {
            this.clientConfig = clientConfig;
            this.activeWindow = activeWindow;
            if (config.IsCLient)
            {
                DisallowRun(clientConfig.WindowNames);
                Loop();

                AppDomain.CurrentDomain.ProcessExit += (s, e) => DisallowRun(Array.Empty<string>());
                Console.CancelKeyPress += (s, e) => DisallowRun(Array.Empty<string>());
            }
        }

        long ticks = DateTime.UtcNow.Ticks;
        public object GetReports(ReportType reportType)
        {
            ticks = DateTime.UtcNow.Ticks;
            if (reportType == ReportType.Full || report.Pid != lastPid || report.Title != lastTitle || report.DisallowCount != count)
            {
                lastPid = report.Pid;
                lastTitle = report.Title;
                count = report.DisallowCount;
                return report;
            }
            return null;
        }

        public void DisallowRun(string[] names)
        {
            clientConfig.WindowNames = names;
            report.DisallowCount = names.Length;
            activeWindow.DisallowRun(names);
        }

        public ActiveWindowTimeReportInfo GetActiveWindowTimes()
        {
            return activeWindowTimeManager.GetActiveWindowTimes();
        }
        public void ClearActiveWindowTimes()
        {
            activeWindowTimeManager.Clear();
        }
        public Dictionary<uint, string> GetWindows()
        {
            return activeWindow.GetWindows();
        }

        private void Loop()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if ((DateTime.UtcNow.Ticks - ticks) / TimeSpan.TicksPerMillisecond < 1000 || report.DisallowCount > 0)
                    {
                        try
                        {

                            ActiveWindowInfo info = activeWindow.GetActiveWindow();
                            report.Title = info.Title;
                            report.FileName = info.FileName;
                            report.Desc = info.Desc;
                            report.Pid = info.Pid;
                            report.WindowCount = activeWindow.GetWindowCount();
                        }
                        catch (Exception ex)
                        {
                            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                Logger.Instance.Error(ex);
                        }
                    }

                    await Task.Delay(500);
                }
            }, TaskCreationOptions.LongRunning);
        }
    }

    public sealed class ActiveReportInfo
    {
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public uint Pid { get; set; }
        public int DisallowCount { get; set; }
        public int WindowCount { get; set; }
    }

    public sealed class ActiveWindowTimeManager
    {
        private ConcurrentDictionary<string, ActiveWindowTimeInfo> dic = new ConcurrentDictionary<string, ActiveWindowTimeInfo>();
        private string lastFileName = string.Empty;
        private string lastTitle = string.Empty;
        private DateTime StartTime = DateTime.Now;
        public void Clear()
        {
            StartTime = DateTime.Now;
            dic.Clear();
            GC.Collect();
        }
        public ActiveWindowTimeReportInfo GetActiveWindowTimes()
        {
            return new ActiveWindowTimeReportInfo
            {
                StartTime = StartTime,
                List = dic.Values.ToList()
            };
        }
        public void Update(ActiveReportInfo active)
        {
            if (string.IsNullOrWhiteSpace(active.FileName)) return;

            int index = active.FileName.LastIndexOf('\\');
            string filename = active.FileName;
            if (index >= 0)
            {
                filename = filename.Substring(index + 1, filename.Length - index - 1);
            }

            if (dic.TryGetValue(filename, out ActiveWindowTimeInfo info) == false)
            {
                info = new ActiveWindowTimeInfo
                {
                    FileName = filename,
                    Desc = active.Desc,
                    StartTime = DateTime.Now,
                    Titles = new Dictionary<string, uint>()
                };
                dic.TryAdd(filename, info);
            }
            if (string.IsNullOrWhiteSpace(lastFileName) == false)
            {
                if (dic.TryGetValue(lastFileName, out ActiveWindowTimeInfo lastInfo))
                {
                    lastInfo.Time += (ulong)(DateTime.Now - lastInfo.StartTime).TotalMilliseconds;

                    if (string.IsNullOrWhiteSpace(lastTitle) == false)
                    {
                        if (info.Titles.TryGetValue(lastTitle, out uint times) == false)
                        {
                            info.Titles.TryAdd(lastTitle, 0);
                        }
                        info.Titles[lastTitle] += (uint)(DateTime.Now - lastInfo.StartTime).TotalMilliseconds;
                    }
                }
            }

            info.StartTime = DateTime.Now;

            lastFileName = filename;
            lastTitle = active.Title;

        }
    }

    [MemoryPackable]
    public sealed partial class ActiveWindowTimeReportInfo
    {
        public DateTime StartTime { get; set; } = DateTime.Now;
        public List<ActiveWindowTimeInfo> List { get; set; } = new List<ActiveWindowTimeInfo>();
    }
    [MemoryPackable]
    public sealed partial class ActiveWindowTimeInfo
    {
        public string FileName { get; set; }
        public string Desc { get; set; }
        public ulong Time { get; set; }
        public DateTime StartTime { get; set; }
        public Dictionary<string, uint> Titles { get; set; }
    }
}
