using linker.libs;
using linker.messenger.channel;
using linker.messenger.pcp;
using linker.messenger.signin;
using linker.messenger.tuntap.cidr;
using linker.nat;
using linker.tun.device;
using linker.tunnel;
using linker.tunnel.connection;
using System.Buffers.Binary;

namespace linker.messenger.tuntap
{
    public interface ITuntapProxyCallback
    {
        public ValueTask Close(ITunnelConnection connection);
        public ValueTask Receive(ITunnelConnection connection, ReadOnlyMemory<byte> packet);
    }

    public class TuntapProxy : Channel, ITunnelConnectionReceiveCallback
    {
        public ITuntapProxyCallback Callback { get; set; }
        protected override string TransactionId => "tuntap";

        private readonly TuntapConfigTransfer tuntapConfigTransfer;
        private readonly TuntapCidrConnectionManager tuntapCidrConnectionManager;
        private readonly TuntapCidrDecenterManager tuntapCidrDecenterManager;
        private readonly TuntapCidrMapfileManager tuntapCidrMapfileManager;
        private readonly TuntapDecenter tuntapDecenter;

        public TuntapProxy(ISignInClientStore signInClientStore,
            TunnelTransfer tunnelTransfer, PcpTransfer pcpTransfer,
            SignInClientTransfer signInClientTransfer, TuntapConfigTransfer tuntapConfigTransfer,
            TuntapCidrConnectionManager tuntapCidrConnectionManager, TuntapCidrDecenterManager tuntapCidrDecenterManager,
            TuntapCidrMapfileManager tuntapCidrMapfileManager, TuntapDecenter tuntapDecenter, ChannelConnectionCaching channelConnectionCaching)
            : base(tunnelTransfer, pcpTransfer, signInClientTransfer, signInClientStore, channelConnectionCaching)
        {
            this.tuntapConfigTransfer = tuntapConfigTransfer;
            this.tuntapCidrConnectionManager = tuntapCidrConnectionManager;
            this.tuntapCidrDecenterManager = tuntapCidrDecenterManager;
            this.tuntapCidrMapfileManager = tuntapCidrMapfileManager;
            this.tuntapDecenter = tuntapDecenter;
        }

        protected override void Connected(ITunnelConnection connection)
        {
            if (connection.ProtocolType == TunnelProtocolType.Tcp && tuntapConfigTransfer.Info.SrcProxy && tuntapDecenter.HasSwitchFlag(connection.RemoteMachineId, TuntapSwitch.SrcProxy))
            {
                connection.PacketBuffer = Helper.TrueArray;
            }

            Add(connection);
            connection.BeginReceive(this, null);
            //有哪些目标IP用了相同目标隧道，更新一下
            tuntapCidrConnectionManager.Update(connection);
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
            await Callback.Receive(connection, buffer).ConfigureAwait(false);
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
            //IPV4广播组播、IPV6 多播
            if ((packet.IPV4Broadcast || packet.IPV6Multicast) && tuntapConfigTransfer.Info.Multicast == false && Connections.IsEmpty == false)
            {
                await Task.WhenAll(Connections.Values.Where(c => c != null && c.Connected).Select(c => c.SendAsync(packet.Buffer, packet.Offset, packet.Length))).ConfigureAwait(false);
                return;
            }

            //IPV4+IPV6 单播
            uint ip = BinaryPrimitives.ReadUInt32BigEndian(packet.DstIp.Span[^4..]);
            if (tuntapCidrConnectionManager.TryGet(ip, out ITunnelConnection connection) && connection.Connected)
            {
                await connection.SendAsync(packet.Buffer, packet.Offset, packet.Length).ConfigureAwait(false);
                return;
            }

            await ConnectTunnel(ip).ConfigureAwait(false);

        }
        public async Task InputPacket(LinkerSrcProxyReadPacket packet)
        {
            if (tuntapCidrConnectionManager.TryGet(packet.DstAddr, out ITunnelConnection connection) && connection.Connected)
            {
                if (connection.PacketBuffer.Length > 0)
                    await connection.SendAsync(packet.Buffer, packet.Offset, packet.Length).ConfigureAwait(false);
                return;
            }

            await ConnectTunnel(packet.DstAddr).ConfigureAwait(false);

        }
        public bool TestIp(uint ip)
        {
            if (tuntapCidrConnectionManager.TryGet(ip, out ITunnelConnection connection) && connection.Connected)
            {
                return connection.PacketBuffer.Length > 0;
            }
            _ = ConnectTunnel(ip).ConfigureAwait(false);

            return false;
        }


        /// <summary>
        /// 打洞或者中继
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        private async Task ConnectTunnel(uint ip)
        {
            ITunnelConnection connection = null;

            if (tuntapCidrDecenterManager.FindValue(ip, out string machineId))
            {
                connection = await ConnectTunnel(machineId, TunnelProtocolType.Quic).ConfigureAwait(false);
            }
            else if (tuntapCidrMapfileManager.FindValue(ip, out machineId))
            {
                connection = await ConnectTunnel(machineId, TunnelProtocolType.Quic).ConfigureAwait(false);
            }
            if (connection != null)
            {
                tuntapCidrConnectionManager.Add(ip, connection);
            }
        }
    }
}
