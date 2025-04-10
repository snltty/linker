using linker.tunnel;
using linker.tunnel.connection;
using linker.libs;
using System.Collections.Concurrent;
using linker.tun;
using System.Buffers.Binary;
using System.Buffers;
using linker.messenger.relay.client;
using linker.messenger.signin;
using linker.messenger.pcp;

namespace linker.messenger.tuntap
{
    public interface ITuntapProxyCallback
    {
        public ValueTask Close(ITunnelConnection connection);
        public void Receive(ITunnelConnection connection, ReadOnlyMemory<byte> packet);
    }

    public sealed class TuntapProxy : channel.Channel, ITunnelConnectionReceiveCallback
    {
        public ITuntapProxyCallback Callback { get; set; }

        private readonly IPAddessCidrManager<string> cidrManager = new IPAddessCidrManager<string>();

        private readonly ConcurrentDictionary<uint, ITunnelConnection> ipConnections = new ConcurrentDictionary<uint, ITunnelConnection>();
        private readonly OperatingMultipleManager operatingMultipleManager = new OperatingMultipleManager();
        protected override string TransactionId => "tuntap";


        private readonly TuntapConfigTransfer tuntapConfigTransfer;
        public TuntapProxy(ISignInClientStore signInClientStore,
            TunnelTransfer tunnelTransfer, RelayClientTransfer relayTransfer, PcpTransfer pcpTransfer,
            SignInClientTransfer signInClientTransfer, IRelayClientStore relayClientStore, TuntapConfigTransfer tuntapConfigTransfer)
            : base(tunnelTransfer, relayTransfer, pcpTransfer, signInClientTransfer, signInClientStore, relayClientStore)
        {
            this.tuntapConfigTransfer = tuntapConfigTransfer;
        }

        protected override void Connected(ITunnelConnection connection)
        {
            connection.BeginReceive(this, null);
            if (tuntapConfigTransfer.Info.TcpMerge)
                connection.StartPacketMerge();
            //有哪些目标IP用了相同目标隧道，更新一下
            List<uint> keys = ipConnections.Where(c => c.Value.RemoteMachineId == connection.RemoteMachineId).Select(c => c.Key).ToList();
            foreach (uint ip in keys)
            {
                ipConnections.AddOrUpdate(ip, connection, (a, b) => connection);
            };
        }

        /// <summary>
        /// 收到隧道数据，写入网卡
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="buffer"></param>
        /// <param name="state"></param>
        /// <returns></returns>
#pragma warning disable CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行
        public async Task Receive(ITunnelConnection connection, ReadOnlyMemory<byte> buffer, object state)
#pragma warning restore CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行
        {
            //LoggerHelper.Instance.Warning($"tuntap write {buffer.Length}");
            Callback.Receive(connection, buffer);
        }
        /// <summary>
        /// 隧道关闭
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task Closed(ITunnelConnection connection, object state)
        {
            await Callback.Close(connection).ConfigureAwait(false);
            Version.Increment();
        }

        /// <summary>
        /// 收到网卡数据，发送给对方
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public async Task InputPacket(LinkerTunDevicPacket packet)
        {
            //LoggerHelper.Instance.Warning($"tuntap read to {new IPEndPoint(new IPAddress(packet.DistIPAddress.Span), packet.DistPort)} {packet.Length}");
            //IPV4广播组播、IPV6 多播
            try
            {
                if (packet.IPV4Broadcast || packet.IPV6Multicast)
                {
                    if (tuntapConfigTransfer.Switch.HasFlag(TuntapSwitch.Multicast) == false && connections.IsEmpty == false)
                    {
                        await Task.WhenAll(connections.Values.Where(c => c != null && c.Connected).Select(c => c.SendAsync(packet.Buffer, packet.Offset, packet.Length)));
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
                await connection.SendAsync(packet.Buffer, packet.Offset, packet.Length).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error($"tuntap proxy Exception {ex}");
                }
                   
            }
        }

        /// <summary>
        /// 打洞或者中继
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        private async Task<ITunnelConnection> ConnectTunnel(uint ip)
        {
            /*
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                Console.WriteLine($"tuntap connect to {NetworkHelper.ToIP(ip)}");
            }
            */
            if (cidrManager.FindValue(ip, out string machineId))
            {
                return await ConnectTunnel(machineId, TunnelProtocolType.Quic).ConfigureAwait(false);
            }
            return null;

        }

        /// <summary>
        /// 清除IP
        /// </summary>
        public void ClearIPs()
        {
            cidrManager.Clear();
            ipConnections.Clear();
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                LoggerHelper.Instance.Debug($"tuntap cache clear");
            }
        }

        /// <summary>
        /// 设置IP，等下有连接进来，用IP匹配，才能知道这个连接是要连谁
        /// </summary>
        /// <param name="ips"></param>
        public void SetIPs(TuntapVeaLanIPAddress[] ips)
        {
            foreach (var ip in ips)
            {
                foreach (var item in ipConnections.Where(c => (c.Key & ip.MaskValue) == ip.NetWork && c.Value.Connected && c.Value.RemoteMachineId != ip.MachineId).ToList())
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Debug($"tuntap {NetworkHelper.ToIP(item.Key)} target machine change {item.Value.RemoteMachineId} to {ip.MachineId}");
                    }
                    ipConnections.TryRemove(item.Key, out _);
                }
            }
            cidrManager.Add(ips.Select(c => new CidrAddInfo<string> { IPAddress = c.IPAddress, PrefixLength = c.PrefixLength, Value = c.MachineId }).ToArray());

        }
        /// <summary>
        /// 设置IP，等下有连接进来，用IP匹配，才能知道这个连接是要连谁
        /// </summary>
        /// <param name="machineId"></param>
        /// <param name="ip"></param>
        public void SetIP(string machineId, uint ip)
        {
            cidrManager.Add(new CidrAddInfo<string> { IPAddress = ip, PrefixLength = 32, Value = machineId });
            if (ipConnections.TryGetValue(ip, out ITunnelConnection connection) && machineId != connection.RemoteMachineId)
            {
                ipConnections.TryRemove(ip, out _);
            }
        }
        /// <summary>
        /// 移除
        /// </summary>
        /// <param name="machineId"></param>
        public void RemoveIP(string machineId)
        {
            cidrManager.Delete(machineId, (a, b) => a == b);
            foreach (var item in ipConnections.Where(c => c.Value.RemoteMachineId == machineId).ToList())
            {
                ipConnections.TryRemove(item.Key, out _);
            }
        }

    }
}
