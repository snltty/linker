using linker.libs;
using linker.libs.extends;
using linker.messenger.channel;
using linker.messenger.signin;
using linker.messenger.tuntap.cidr;
using linker.nat;
using linker.tun.device;
using linker.tunnel;
using linker.tunnel.connection;
using System.Buffers.Binary;

namespace linker.messenger.tuntap.client
{
    public interface ITuntapProxyCallback
    {
        public ValueTask Close(ITunnelConnection connection);
        public ValueTask<bool> Receive(ITunnelConnection connection, ReadOnlyMemory<byte> packet);
    }

    public class TuntapProxy : Channel, ITunnelConnectionReceiveCallback
    {
        public ITuntapProxyCallback Callback { get; set; }
        protected override string TransactionId => "tuntap";

        private readonly TuntapConfigTransfer tuntapConfigTransfer;
        private readonly TuntapCidrConnectionManager tuntapCidrConnectionManager;
        private readonly TuntapCidrDecenterManager tuntapCidrDecenterManager;
        private readonly TuntapDecenter tuntapDecenter;

        public TuntapProxy(ISignInClientStore signInClientStore,
            TunnelTransfer tunnelTransfer,
            SignInClientTransfer signInClientTransfer, TuntapConfigTransfer tuntapConfigTransfer,
            TuntapCidrConnectionManager tuntapCidrConnectionManager, TuntapCidrDecenterManager tuntapCidrDecenterManager,
            TuntapDecenter tuntapDecenter, ChannelConnectionCaching channelConnectionCaching)
            : base(tunnelTransfer, signInClientTransfer, signInClientStore, channelConnectionCaching)
        {
            this.tuntapConfigTransfer = tuntapConfigTransfer;
            this.tuntapCidrConnectionManager = tuntapCidrConnectionManager;
            this.tuntapCidrDecenterManager = tuntapCidrDecenterManager;
            this.tuntapDecenter = tuntapDecenter;
        }

        protected override void Connected(ITunnelConnection connection)
        {
            Add(connection);
            connection.BeginReceive(this, null);
            tuntapCidrConnectionManager.Update(connection);
        }

        public async ValueTask Receive(ITunnelConnection connection, ReadOnlyMemory<byte> buffer, object state)
        {
            await Callback.Receive(connection, buffer).ConfigureAwait(false);
        }

        public async ValueTask Closed(ITunnelConnection connection, object state)
        {
            await Callback.Close(connection).ConfigureAwait(false);
            Version.Increment();
        }
        public async ValueTask<bool> InputPacket(LinkerTunDevicPacket packet)
        {
            if ((packet.IPV4Broadcast || packet.IPV6Multicast) && tuntapConfigTransfer.Info.Multicast == false && Connections.IsEmpty == false)
            {
                foreach (var item in Connections.Values)
                {
                    await item.SendAsync(packet.Buffer, packet.Offset, packet.Length).ConfigureAwait(false);
                }
                return true;
            }
            uint ip = BinaryPrimitives.ReadUInt32BigEndian(packet.DstIp.Span[^4..]);
            if (tuntapCidrConnectionManager.TryGet(ip, out ITunnelConnection connection) && connection.Connected)
            {
                await connection.SendAsync(packet.Buffer, packet.Offset, packet.Length);
                return true;
            }
            await ConnectTunnel(ip).ConfigureAwait(false);
            return false;
        }
        public async ValueTask<bool> InputPacket(LinkerSrcProxyReadPacket packet)
        {
            if (tuntapCidrConnectionManager.TryGet(packet.DstAddr, out ITunnelConnection connection) && connection.Connected)
            {
                return await connection.SendAsync(packet.Memory).ConfigureAwait(false);
            }
            await ConnectTunnel(packet.DstAddr).ConfigureAwait(false);
            if (tuntapCidrConnectionManager.TryGet(packet.DstAddr, out connection) && connection.Connected)
            {
                return await connection.SendAsync(packet.Memory).ConfigureAwait(false);
            }
            return false;
        }
        public bool TestIp(uint ip)
        {
            if (tuntapCidrConnectionManager.TryGet(ip, out ITunnelConnection connection) && connection.Connected)
            {
                return connection.ProtocolType == TunnelProtocolType.Tcp
                    && connection.Type != TunnelType.Mesh
                    && tuntapConfigTransfer.Info.SrcProxy
                    && tuntapDecenter.HasSwitchFlag(connection.RemoteMachineId, TuntapSwitch.SrcProxy);
            }
            _ = ConnectTunnel(ip).ConfigureAwait(false);
            return false;
        }

        private async ValueTask ConnectTunnel(uint ip)
        {
            ITunnelConnection connection = null;

            if (tuntapCidrDecenterManager.FindValue(ip, out string machineId, out uint dst, out uint prefix))
            {
                connection = await ConnectTunnel(machineId, new Dictionary<string, string>()
                {
                    ["fec"] = GetFecProfileInfo(machineId).ToJson()
                }).ConfigureAwait(false);
            }
            if (connection != null)
            {
                tuntapCidrConnectionManager.Add(ip, connection);
            }
        }

        private TuntapFecProfileInfo[] GetFecProfileInfo(string machineId)
        {
            var profile = tuntapConfigTransfer.Info.FecProfile.Where(c => c.Disabled == false).ToArray();
            if (profile.Length > 0)
            {
                return profile;
            }
            if (tuntapDecenter.Infos.TryGetValue(machineId, out var info))
            {
                profile = info.FecProfile.Where(c => c.Disabled == false).ToArray();
                if (profile.Length > 0)
                {
                    return profile;
                }
            }
            return [];
        }
    }
}
