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

        public ValueTask<bool> Receive(ITunnelConnection connection, ReadOnlyMemory<byte> buffer, object state)
        {
            return Callback.Receive(connection, buffer);
        }

        public ValueTask Closed(ITunnelConnection connection, object state)
        {
            Version.Increment();
            return Callback.Close(connection);

        }
        public ValueTask<bool> InputPacket(LinkerTunDevicPacket packet)
        {
            StopWatchHelper.StartTimestamp(StopWatchHelper.StopWatchType.Tun_Read_Connecttion);

            if ((packet.IPV4Broadcast || packet.IPV6Multicast) && tuntapConfigTransfer.Info.Multicast == false)
            {
                return SendAll(packet);
            }
            else
            {
                uint ip = BinaryPrimitives.ReadUInt32BigEndian(packet.DstIp.Span[^4..]);
                if (tuntapCidrConnectionManager.TryGet(ip, out ITunnelConnection connection) && connection.Connected)
                {
                    StopWatchHelper.EndTimestamp(StopWatchHelper.StopWatchType.Tun_Read_Connecttion);
                    return connection.SendAsync(packet.Buffer, packet.Offset, packet.Length);
                }
                return ConnectTunnel(ip);
            }

        }
        private async ValueTask<bool> SendAll(LinkerTunDevicPacket packet)
        {
            foreach (var item in Connections.Values)
            {
                await item.SendAsync(packet.Buffer, packet.Offset, packet.Length).ConfigureAwait(false);
            }
            return true;
        }

        public ValueTask<bool> InputPacket(LinkerSrcProxyReadPacket packet)
        {
            if (tuntapCidrConnectionManager.TryGet(packet.DstAddr, out ITunnelConnection connection) && connection.Connected && connection.HashCode == packet.HashCode)
            {
                return connection.SendAsync(packet.Memory);
            }
            return ValueTask.FromResult(false);
        }
        public int TestIp(uint ip)
        {
            if (tuntapCidrConnectionManager.TryGet(ip, out ITunnelConnection connection) && connection.Connected)
            {
                bool result = connection.ProtocolType == TunnelProtocolType.Tcp
                     && connection.Type != TunnelType.Mesh
                     && tuntapConfigTransfer.Info.SrcProxy
                     && tuntapDecenter.HasSwitchFlag(connection.RemoteMachineId, TuntapSwitch.SrcProxy);
                return result ? connection.HashCode : 0;
            }
            _ = ConnectTunnel(ip).ConfigureAwait(false);
            return 0;
        }

        private async ValueTask<bool> ConnectTunnel(uint ip)
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
                return true;
            }
            return false;
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
