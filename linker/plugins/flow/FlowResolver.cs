using linker.libs;
using linker.libs.extends;
using linker.plugins.resolver;
using linker.plugins.signin.messenger;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace linker.plugins.flow
{
    public sealed class FlowResolver : IResolver, IFlow
    {
        public ResolverType Type => ResolverType.Flow;
        public string FlowName => "flow";

        /// <summary>
        /// 在线 | 总数
        /// </summary>
        public ulong ReceiveBytes { get; private set; }
        /// <summary>
        /// 服务器数
        /// </summary>
        public ulong SendtBytes { get; private set; }


        private ConcurrentDictionary<IPEndPoint, OnlineFlowInfo> servers = new ConcurrentDictionary<IPEndPoint, OnlineFlowInfo>();
        private readonly SignCaching signCaching;
        public FlowResolver(SignCaching signCaching)
        {
            this.signCaching = signCaching;
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

                if (servers.TryGetValue(ep, out OnlineFlowInfo onlineFlowInfo) == false)
                {
                    onlineFlowInfo = new OnlineFlowInfo { Time = time };
                    servers.TryAdd(ep, onlineFlowInfo);
                }
                onlineFlowInfo.Time = time;
                onlineFlowInfo.Online = memory.Slice(1, 4).ToInt32();
                onlineFlowInfo.Total = memory.Slice(5, 4).ToInt32();
            }
            catch (Exception)
            {
            }

            await Task.CompletedTask;
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
            List<IPEndPoint> keys = servers.Where(c => time - c.Value.Time > 15000).Select(c => c.Key).ToList();

            foreach (IPEndPoint key in keys)
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
            byte[] buffer = ArrayPool<byte>.Shared.Rent(9);

            try
            {
                signCaching.GetOnline(out int total, out int onlone);
                buffer[0] = (byte)ResolverType.Flow;
                onlone.ToBytes(buffer.AsMemory(1));
                total.ToBytes(buffer.AsMemory(5));

                using UdpClient udpClient = new UdpClient(AddressFamily.InterNetwork);
                udpClient.Client.WindowsUdpBug();

                udpClient.Send(buffer.AsSpan(0, 9), "linker.snltty.com", 1802);
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
    }
}
