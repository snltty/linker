using linker.libs;
using System.Net;

namespace linker.messenger.tuntap.cidr
{
    public sealed class TuntapCidrMapfileManager
    {
        private readonly IPAddessCidrManager<string> cidrManager = new IPAddessCidrManager<string>();
        private List<MapInfo> maps = [];
        private FileSystemWatcher watcher;

        private readonly TuntapDecenter tuntapDecenter;
        public TuntapCidrMapfileManager(TuntapDecenter tuntapDecenter)
        {
            this.tuntapDecenter = tuntapDecenter;
            tuntapDecenter.OnChanged += ProcessMaps;
            tuntapDecenter.OnClear += cidrManager.Clear;

            Initialize(Path.Join(Helper.CurrentDirectory, "configs"), "ip-list.lin");
        }

        private void Initialize(string path, string file)
        {
            try
            {
                watcher = new FileSystemWatcher(Path.GetFullPath(path), file);
                watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;
                watcher.Created += (sender, e) => Reload(path, file);
                watcher.Changed += (sender, e) => Reload(path, file);
                watcher.Renamed += (sender, e) => Reload(path, file);
                watcher.Deleted += (sender, e) => Reload(path, file);
                watcher.Error += (sender, e) => { if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Error(e.GetException()); };
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;

                Reload(path, file);
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
        }
        private void Reload(string path, string file)
        {
            try
            {
                if (File.Exists(Path.Join(path, file)) == false) return;

                maps = File.ReadAllText(Path.Join(path, file)).Split(Environment.NewLine)
                    .Where(c => string.IsNullOrWhiteSpace(c) == false)
                    .Select(c => c.Split(','))
                    .Select(c => new MapInfo { Src = IPAddress.Parse(c[0]), PrefixLength = byte.Parse(c[1]), Dst = IPAddress.Parse(c[2]) })
                    .ToList();

                ProcessMaps();
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                maps = [];
                ProcessMaps();
            }
        }
        private void ProcessMaps()
        {
            try
            {
                Dictionary<IPAddress, string> ip2machine = tuntapDecenter.Infos.Values.Where(c=>c.Available).DistinctBy(c=>c.IP).ToDictionary(c => c.IP, c => c.MachineId);
                CidrAddInfo<string>[] cidrs = maps.Select(c =>
                {
                    ip2machine.TryGetValue(c.Dst, out string machineId);
                    return new CidrAddInfo<string> { IPAddress = NetworkHelper.ToValue(c.Src), PrefixLength = c.PrefixLength, Value = machineId };
                }).Where(c => string.IsNullOrWhiteSpace(c.Value) == false).ToArray();

                cidrManager.Clear();
                cidrManager.Add(cidrs);
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
        }

        public bool FindValue(uint ip, out string value)
        {
            return cidrManager.FindValue(ip, out value);
        }

        struct MapInfo
        {
            public IPAddress Src { get; set; }
            public byte PrefixLength { get; set; }
            public IPAddress Dst { get; set; }
        }
    }
}
