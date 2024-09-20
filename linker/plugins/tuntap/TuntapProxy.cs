using linker.config;
using linker.plugins.relay;
using linker.tunnel;
using linker.tunnel.connection;
using linker.libs;
using System.Collections.Concurrent;
using linker.plugins.tuntap.config;
using linker.tun;
using System.Buffers.Binary;
using linker.plugins.client;
using linker.plugins.tunnel;
using System.Buffers;

namespace linker.plugins.tuntap
{
    public sealed class TuntapProxy : TunnelBase, ILinkerTunDeviceCallback, ITunnelConnectionReceiveCallback
    {
        private uint[] maskValues = Array.Empty<uint>();
        private readonly ConcurrentDictionary<uint, List<string>> ip2MachineDic = new ConcurrentDictionary<uint, List<string>>();
        private readonly ConcurrentDictionary<uint, ITunnelConnection> ipConnections = new ConcurrentDictionary<uint, ITunnelConnection>();
        private readonly OperatingMultipleManager operatingMultipleManager = new OperatingMultipleManager();

        protected override string TransactionId => "tuntap";
        private readonly LinkerTunDeviceAdapter linkerTunDeviceAdapter;

        public TuntapProxy(FileConfig config, TunnelTransfer tunnelTransfer, RelayTransfer relayTransfer, ClientSignInTransfer clientSignInTransfer , LinkerTunDeviceAdapter linkerTunDeviceAdapter) 
            : base(config, tunnelTransfer, relayTransfer, clientSignInTransfer)
        {
            this.linkerTunDeviceAdapter = linkerTunDeviceAdapter;
        }

        protected override void Connected(ITunnelConnection connection)
        {
            connection.BeginReceive(this, null);

            //有哪些目标IP用了相同目标隧道，更新一下
            List<uint> keys = ipConnections.Where(c => c.Value.RemoteMachineId == connection.RemoteMachineId).Select(c => c.Key).ToList();
            foreach (uint ip in keys)
            {
                ipConnections.AddOrUpdate(ip, connection, (a, b) => connection);
            };
        }
        protected override void OffLine(string machineId)
        {
            foreach (var item in ip2MachineDic.Values)
            {
                item.Remove(machineId);
            }
        }

        /// <summary>
        /// 收到隧道数据，写入网卡
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="buffer"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task Receive(ITunnelConnection connection, ReadOnlyMemory<byte> buffer, object state)
        {
            linkerTunDeviceAdapter.Write(buffer);
            await Task.CompletedTask;
        }
        /// <summary>
        /// 隧道关闭
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task Closed(ITunnelConnection connection, object state)
        {
            Version.Add();
            await Task.CompletedTask;
        }
        /// <summary>
        /// 收到网卡数据，发送给对方
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public async Task Callback(LinkerTunDevicPacket packet)
        {
            //IPV4广播组播
            if (packet.IPV4Broadcast)
            {
                if (connections.IsEmpty == false)
                {
                    await Task.WhenAll(connections.Values.Where(c => c != null && c.Connected).Select(c => c.SendAsync(packet.Packet)));
                }
                return;
            }
            //IPV6 多播
            else if (packet.IPV6Multicast)
            {
                if (connections.IsEmpty == false)
                {
                    await Task.WhenAll(connections.Values.Where(c => c != null && c.Connected).Select(c => c.SendAsync(packet.Packet)));
                }
                return;
            }

            //IPV4+IPV6 单播
            uint ip = BinaryPrimitives.ReadUInt32BigEndian(packet.DistIPAddress.Span[^4..]);

            if (ipConnections.TryGetValue(ip, out ITunnelConnection connection) == false || connection == null || connection.Connected == false)
            {
                //开始操作，开始失败直接丢包
                if (operatingMultipleManager.StartOperation(ip) == false)
                {
                    return;
                }

                _ = ConnectTunnel(ip).ContinueWith((result, state) =>
                {
                    //结束操作
                    operatingMultipleManager.StopOperation((uint)state);
                    //连接成功就缓存隧道
                    if (result.Result != null)
                    {
                        ipConnections.AddOrUpdate((uint)state, result.Result, (a, b) => result.Result);
                    }
                }, ip);
                return;
            }
            await connection.SendAsync(packet.Packet);
        }


        /// <summary>
        /// 设置IP，等下有连接进来，用IP匹配，才能知道这个连接是要连谁
        /// </summary>
        /// <param name="ips"></param>
        public void SetIPs(TuntapVeaLanIPAddress[] ips)
        {
            var dic = ips.GroupBy(c => c.NetWork).ToDictionary(c => c.Key, d => d.Select(e => e.MachineId).ToList());
            foreach (var item in dic)
            {
                ip2MachineDic.AddOrUpdate(item.Key, item.Value, (a, b) => item.Value);
            }
            maskValues = ips.Select(c => c.MaskValue).Distinct().OrderBy(c => c).ToArray();

        }
        /// <summary>
        /// 设置IP，等下有连接进来，用IP匹配，才能知道这个连接是要连谁
        /// </summary>
        /// <param name="machineId"></param>
        /// <param name="ip"></param>
        public void SetIP(string machineId, uint ip)
        {
            if (ip2MachineDic.TryGetValue(ip,out List<string> list) == false)
            {
                list = new List<string>();
                ip2MachineDic.AddOrUpdate(ip, list, (a, b) => list);
            }
            list.Add(machineId);
        }

        /// <summary>
        /// 打洞或者中继
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        private async Task<ITunnelConnection> ConnectTunnel(uint ip)
        {
            //直接按IP查找
            if (ip2MachineDic.TryGetValue(ip, out List<string> machineId) && machineId.Count > 0)
            {
                return await ConnectTunnel(machineId[0], TunnelProtocolType.Quic).ConfigureAwait(false);
            }

            //匹配掩码查找
            for (int i = 0; i < maskValues.Length; i++)
            {
                uint network = ip & maskValues[i];
                if (ip2MachineDic.TryGetValue(network, out machineId) && machineId.Count > 0)
                {
                    ip2MachineDic.TryAdd(ip, machineId);
                    return await ConnectTunnel(machineId[0], TunnelProtocolType.Quic).ConfigureAwait(false);
                }
            }
            return null;

        }
    }
}
