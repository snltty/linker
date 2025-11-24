using linker.libs;
using linker.libs.extends;
using linker.libs.timer;
using linker.messenger.signin;
using linker.messenger.tunnel;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.flow
{
    public sealed class FlowResolver : IResolver, IFlow
    {
        public byte Type => (byte)ResolverType.FlowReport;
        public string FlowName => "flow";
        public VersionManager Version { get; } = new VersionManager();

        /// <summary>
        /// 在线 | 总数
        /// </summary>
        public long ReceiveBytes { get; private set; }
        /// <summary>
        /// 服务器数
        /// </summary>
        public long SendtBytes { get; private set; }


        private readonly ConcurrentDictionary<IPAddress, OnlineFlowInfo> servers = new(new IPAddressComparer());
        private readonly SignInServerCaching signCaching;
        private readonly ISerializer serializer;
        public FlowResolver(SignInServerCaching signCaching, ISerializer serializer)
        {
            this.signCaching = signCaching;
            this.serializer = serializer;
            OnlineTask();
        }
        public (long, long) GetDiffBytes(long recv, long sent)
        {
            return (ReceiveBytes, SendtBytes);
        }

        public async Task Resolve(Socket socket, Memory<byte> memory)
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }
        public async Task Resolve(Socket socket, IPEndPoint ep, Memory<byte> memory)
        {
            try
            {
                long time = Environment.TickCount64;

                if (servers.TryGetValue(ep.Address, out OnlineFlowInfo onlineFlowInfo) == false)
                {
                    onlineFlowInfo = new OnlineFlowInfo { Time = time };
                    servers.TryAdd(ep.Address, onlineFlowInfo);
                }
                onlineFlowInfo.Time = time;
                onlineFlowInfo.Online = memory.Slice(0, 4).ToInt32();
                onlineFlowInfo.Total = memory.Slice(4, 4).ToInt32();

                long online = (long)servers.Where(c => time - c.Value.Time < 15000).Sum(c => c.Value.Online) << 32;
                long total = servers.Where(c => time - c.Value.Time < 15000).Sum(c => c.Value.Total);
                ReceiveBytes = online | total;
                SendtBytes = servers.Count(c => time - c.Value.Time < 15000);

                if (memory.Length > 8)
                {
                    var nets = serializer.Deserialize<List<FlowReportNetInfo>>(memory.Slice(8).Span);
                    onlineFlowInfo.Nets = nets.Where(c => c.Lon > 0 && c.Lat > 0 && (string.IsNullOrWhiteSpace(c.City) || c.City.IndexOf('-') < 0)).ToList();
                    onlineFlowInfo.Systems = nets.Where(c => c.Lon == 0 && c.Lat == 0 && string.IsNullOrWhiteSpace(c.City) == false && c.City.IndexOf('-') > 0).ToList();
                }
                Version.Increment();

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Debug($"online:{online},total:{total},server:{SendtBytes}");
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }


        public string GetItems() => string.Empty;
        public void SetItems(string json) { }
        public void SetBytes(long receiveBytes, long sendtBytes) { }
        public void Clear() { }


        public List<FlowReportNetInfo> GetCitys()
        {
            return servers.Values.SelectMany(c => c.Nets).GroupBy(c => c.City).Select(c => new FlowReportNetInfo
            {
                City = c.Key,
                Count = c.Sum(d => d.Count),
                Lat = c.Count() == 1 ? c.First().Lat : c.Average(c => c.Lat),
                Lon = c.Count() == 1 ? c.First().Lon : c.Average(c => c.Lon)
            }).ToList();
        }
        public Dictionary<string, int> GetSystems()
        {
            return servers.Values.SelectMany(c => c.Systems).GroupBy(c => c.City).Select(c => new FlowReportNetInfo
            {
                City = c.Key,
                Count = c.Sum(d => d.Count)
            }).ToDictionary(c => c.City, d => d.Count);
        }

        private void OnlineTask()
        {
            UdpClient udpClient = new UdpClient(AddressFamily.InterNetwork);
            udpClient.Client.WindowsUdpBug();
            TimerHelper.SetIntervalLong(() =>
            {
                try
                {
                    Report(udpClient);
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                }
            }, 5000);
        }
        private void Report(UdpClient udpClient)
        {
            var clients = signCaching.Get();
            var nets = clients.Where(c => c.Args.ContainsKey("tunnelNet")).Select(c => c.Args["tunnelNet"].DeJson<SignInArgsNetInfo>()).GroupBy(c => c.City).Select(c => new FlowReportNetInfo
            {
                City = c.Key,
                Count = c.Count(),
                Lat = c.Count() == 1 ? c.First().Lat : c.Average(c => c.Lat),
                Lon = c.Count() == 1 ? c.First().Lon : c.Average(c => c.Lon)
            });
            var systems = clients.Where(c => c.Args.ContainsKey("machineStr")).Select(c => c.Args["machineStr"]).GroupBy(c => c).Select(c => new FlowReportNetInfo
            {
                City = c.Key,
                Count = c.Count(),
                Lat = 0,
                Lon = 0
            });

            byte[] netBytes = serializer.Serialize(nets.Concat(systems).ToList());
            Span<byte> buffer = stackalloc byte[9 + netBytes.Length];
            signCaching.GetOnline(out int total, out int onlone);
            buffer[0] = (byte)ResolverType.FlowReport;
            onlone.ToBytes(buffer.Slice(1));
            total.ToBytes(buffer.Slice(5));
            netBytes.CopyTo(buffer.Slice(9));

            string domain = "linker.snltty.com";
#if DEBUG
            domain = "127.0.0.1";
#endif
            udpClient.Send(buffer.Slice(0, 9 + netBytes.Length), domain, 1802);
        }
    }

    public sealed class OnlineFlowInfo
    {
        public long Time { get; set; }
        public int Online { get; set; }
        public int Total { get; set; }

        public List<FlowReportNetInfo> Nets { get; set; } = new List<FlowReportNetInfo>();
        public List<FlowReportNetInfo> Systems { get; set; } = new List<FlowReportNetInfo>();
    }
    public sealed class IPAddressComparer : IEqualityComparer<IPAddress>
    {
        public bool Equals(IPAddress x, IPAddress y)
        {
            return x.Equals(y);
        }
        public int GetHashCode(IPAddress obj)
        {
            if (obj == null) return 0;
            return obj.GetHashCode();
        }
    }
}
