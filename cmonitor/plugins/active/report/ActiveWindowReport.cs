using cmonitor.client;
using cmonitor.client.report;
using cmonitor.config;
using common.libs;
using MemoryPack;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Diagnostics;
using cmonitor.client.running;
using cmonitor.plugins.active.report;

namespace cmonitor.plugins.active.report
{
    public sealed class ActiveWindowReport : IClientReport
    {
        public string Name => "ActiveWindow";

        private readonly RunningConfig runningConfig;
        private readonly IActiveWindow activeWindow;
        private readonly ActiveWindowTimeManager activeWindowTimeManager = new ActiveWindowTimeManager();
        private ActiveReportInfo report = new ActiveReportInfo();

        public ActiveWindowReport(Config config, RunningConfig runningConfig, IActiveWindow activeWindow, ClientSignInState clientSignInState)
        {
            this.runningConfig = runningConfig;
            this.activeWindow = activeWindow;

            DisallowRun(Array.Empty<string>());
            clientSignInState.NetworkFirstEnabledHandle += () =>
            {
                DisallowRun(runningConfig.Data.Active.FileNames);
                Loop();
            };
        }

        long ticks = DateTime.UtcNow.Ticks;
        public object GetReports(ReportType reportType)
        {
            ticks = DateTime.UtcNow.Ticks;
            report.Ids1 = runningConfig.Data.Active.Ids1;
            report.Ids2 = runningConfig.Data.Active.Ids2;
            if (reportType == ReportType.Full || report.Updated())
            {
                return report;
            }
            return null;
        }


        public void DisallowRun(ActiveDisallowInfo activeDisallowInfo)
        {
            runningConfig.Data.Active = activeDisallowInfo;
            report.DisallowCount = activeDisallowInfo.FileNames.Length;
            activeWindow.DisallowRun(activeDisallowInfo.FileNames);
        }
        private void DisallowRun(string[] names)
        {
            report.DisallowCount = names.Length;
            activeWindow.DisallowRun(names);
        }

        public void Kill(uint pid)
        {
            activeWindow.Kill(pid);
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
            Task.Run(async () =>
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

                            Disallow(info);
                        }
                        catch (Exception ex)
                        {
                            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                Logger.Instance.Error(ex);
                        }
                    }

                    await Task.Delay(500);
                }
            });
        }

        private bool Disallow(ActiveWindowInfo window)
        {
            if (runningConfig.Data.Active.FileNames.Length > 0)
            {
                try
                {
                    ReadOnlySpan<char> filenameSpan = window.FileName.AsSpan();
                    uint pid = window.Pid;
                    foreach (string item in runningConfig.Data.Active.FileNames)
                    {
                        ReadOnlySpan<char> nameSpan = item.AsSpan();
                        bool result = item == window.Title
                            || (filenameSpan.Length >= nameSpan.Length && filenameSpan.Slice(filenameSpan.Length - nameSpan.Length, nameSpan.Length).SequenceEqual(nameSpan))
                            || (item.StartsWith('/') && item.EndsWith('/') && Regex.IsMatch(window.Title, item.Trim('/')));
                        if (result)
                        {
                            activeWindow.Kill(pid);
                        }
                    }
                }
                catch (Exception)
                {
                }
                return true;
            }
            return false;
        }
    }

    [MemoryPackable]
    public sealed partial class ActiveDisallowInfo
    {
        public string[] FileNames { get; set; } = Array.Empty<string>();
        public string[] Ids1 { get; set; } = Array.Empty<string>();
        public string[] Ids2 { get; set; } = Array.Empty<string>();
    }

    public sealed class ActiveReportInfo : ReportInfo
    {
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public uint Pid { get; set; }
        public int DisallowCount { get; set; }
        public int WindowCount { get; set; }

        public string[] Ids1 { get; set; }
        public string[] Ids2 { get; set; }
        public override int HashCode()
        {
            return Title.GetHashCode() ^ Pid.GetHashCode() ^ DisallowCount.GetHashCode() ^ Ids1.GetHashCode() ^ Ids2.GetHashCode();
        }

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

namespace cmonitor.client.running
{
    public sealed partial class RunningConfigInfo
    {
        private ActiveDisallowInfo active = new ActiveDisallowInfo();
        public ActiveDisallowInfo Active
        {
            get => active; set
            {
                Updated++;
                active = value;
            }
        }
    }
}
