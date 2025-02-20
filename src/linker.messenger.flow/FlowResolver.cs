using linker.libs;
using linker.libs.extends;
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

        /// <summary>
        /// 在线 | 总数
        /// </summary>
        public ulong ReceiveBytes { get; private set; }
        /// <summary>
        /// 服务器数
        /// </summary>
        public ulong SendtBytes { get; private set; }


        private ConcurrentDictionary<IPAddress, OnlineFlowInfo> servers = new ConcurrentDictionary<IPAddress, OnlineFlowInfo>(new IPAddressComparer());
        private readonly SignInServerCaching signCaching;
        private readonly ISerializer serializer;
        public FlowResolver(SignInServerCaching signCaching, ISerializer serializer)
        {
            this.signCaching = signCaching;
            this.serializer = serializer;
            OnlineTask();
        }

        public async Task Resolve(Socket socket, Memory<byte> memory)
        {
            await Task.CompletedTask;
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

                if (memory.Length > 8)
                {
                    onlineFlowInfo.Nets = serializer.Deserialize<List<FlowReportNetInfo>>(memory.Slice(8).Span);
                }
            }
            catch (Exception)
            {
            }

            await Task.CompletedTask;
        }

        public List<FlowReportNetInfo> GetCitys()
        {
            return servers.Values.SelectMany(c => c.Nets).GroupBy(c => c.City).Select(c => new FlowReportNetInfo
            {
                City = c.Key,
                Count = c.Count(),
                Lat = c.Count() == 1 ? c.First().Lat : c.Average(c => c.Lat),
                Lon = c.Count() == 1 ? c.First().Lon : c.Average(c => c.Lon)
            }).ToList();
        }

        private void OnlineTask()
        {
            TimerHelper.SetInterval(() =>
            {
                try
                {
                    Counter();
                    Report();
                }
                catch (Exception)
                {
                }

                return true;
            }, 5000);
        }
        private void Counter()
        {
            long time = Environment.TickCount64;
            List<IPAddress> keys = servers.Where(c => time - c.Value.Time > 15000).Select(c => c.Key).ToList();

            foreach (IPAddress key in keys)
            {
                servers.TryRemove(key, out _);
            }

            ulong online = (ulong)servers.Sum(c => c.Value.Online) << 32;
            ulong total = (ulong)servers.Sum(c => c.Value.Total);

            ReceiveBytes = online | total;
            SendtBytes = (ulong)servers.Count;
        }
        private void Report()
        {
            List<FlowReportNetInfo> nets = signCaching.Get().Where(c => c.Args.ContainsKey("tunnelNet")).Select(c => c.Args["tunnelNet"].DeJson<SignInArgsNetInfo>()).GroupBy(c => c.City).Select(c => new FlowReportNetInfo
            {
                City = c.Key,
                Count = c.Count(),
                Lat = c.Count() == 1 ? c.First().Lat : c.Average(c => c.Lat),
                Lon = c.Count() == 1 ? c.First().Lon : c.Average(c => c.Lon)
            }).ToList();
            byte[] netBytes = serializer.Serialize(nets);


            byte[] buffer = ArrayPool<byte>.Shared.Rent(9 + netBytes.Length);

            try
            {
                signCaching.GetOnline(out int total, out int onlone);
                buffer[0] = (byte)ResolverType.FlowReport;
                onlone.ToBytes(buffer.AsMemory(1));
                total.ToBytes(buffer.AsMemory(5));

                netBytes.CopyTo(buffer.AsMemory(9));


                using UdpClient udpClient = new UdpClient(AddressFamily.InterNetwork);
                udpClient.Client.WindowsUdpBug();

                string domain = "linker.snltty.com";
#if DEBUG
                domain = "127.0.0.1";
#endif
                udpClient.Send(buffer.AsSpan(0, 9 + netBytes.Length), domain, 1802);
            }
            catch (Exception)
            {
            }
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public sealed class OnlineFlowInfo
    {
        public long Time { get; set; }
        public int Online { get; set; }
        public int Total { get; set; }

        public List<FlowReportNetInfo> Nets { get; set; } = new List<FlowReportNetInfo>();
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
