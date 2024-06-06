using cmonitor.plugins.relay;
using cmonitor.plugins.tuntap.vea;
using cmonitor.tunnel;
using cmonitor.tunnel.connection;
using cmonitor.tunnel.proxy;
using common.libs;
using common.libs.socks5;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace cmonitor.plugins.tuntap.proxy
{
    public sealed class TuntapProxy : TunnelProxy
    {
        private readonly TunnelTransfer tunnelTransfer;
        private readonly RelayTransfer relayTransfer;

        private IPEndPoint proxyEP;

        private uint maskValue = NetworkHelper.MaskValue(24);
        private readonly ConcurrentDictionary<uint, string> dic = new ConcurrentDictionary<uint, string>();
        private readonly ConcurrentDictionary<string, ITunnelConnection> dicConnections = new ConcurrentDictionary<string, ITunnelConnection>();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> dicLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

        public TuntapProxy(TunnelTransfer tunnelTransfer, RelayTransfer relayTransfer)
        {
            this.tunnelTransfer = tunnelTransfer;
            this.relayTransfer = relayTransfer;

            Start(0);
            proxyEP = new IPEndPoint(IPAddress.Any, LocalEndpoint.Port);
            Logger.Instance.Info($"start tuntap proxy, listen port : {LocalEndpoint}");


            tunnelTransfer.SetConnectedCallback("tuntap", OnConnected);
            relayTransfer.SetConnectedCallback("tuntap", OnConnected);
        }
        private void OnConnected(ITunnelConnection connection)
        {
            dicConnections.AddOrUpdate(connection.RemoteMachineName, connection, (a, b) => connection);
            BindConnectionReceive(connection);
        }

        public void SetIPs(List<TuntapVeaLanIPAddressList> ips)
        {
            dic.Clear();
            foreach (var item in ips)
            {
                foreach (var ip in item.IPS)
                {
                    dic.AddOrUpdate(ip.NetWork, item.MachineName, (a, b) => item.MachineName);
                }
            }
        }
        public void SetIP(string machineName, uint ip)
        {
            dic.AddOrUpdate(ip, machineName, (a, b) => machineName);
        }

        protected override async ValueTask<bool> ConnectTunnelConnection(AsyncUserToken token)
        {
            token.Proxy.TargetEP = null;
            token.Proxy.Rsv = (byte)Socks5EnumStep.Request;

            //步骤，request
            bool result = await ReceiveCommandData(token);
            if (result == false) return true;
            await token.Socket.SendAsync(new byte[] { 0x05, 0x00 });
            token.Proxy.Rsv = (byte)Socks5EnumStep.Command;
            token.Proxy.Data = Helper.EmptyArray;

            //步骤，command
            result = await ReceiveCommandData(token);
            if (result == false)
            {
                return true;
            }
            Socks5EnumRequestCommand command = (Socks5EnumRequestCommand)token.Proxy.Data.Span[1];

            //获取远端地址
            ReadOnlyMemory<byte> ipArray = Socks5Parser.GetRemoteEndPoint(token.Proxy.Data, out Socks5EnumAddressType addressType, out ushort port, out int index);
            token.Proxy.TargetEP = new IPEndPoint(new IPAddress(ipArray.Span), port);
            token.Proxy.Data = token.Proxy.Data.Slice(index);
            token.TargetIP = BinaryPrimitives.ReadUInt32BigEndian(ipArray.Span);
            //不支持域名
            if (addressType == Socks5EnumAddressType.Domain)
            {
                token.Proxy.TargetEP = null;
                byte[] response1 = Socks5Parser.MakeConnectResponse(proxyEP, (byte)Socks5EnumResponseCommand.AddressNotAllow);
                await token.Socket.SendAsync(response1.AsMemory());
                return true;
            }
            //是UDP中继，不做连接操作，等UDP数据过去的时候再绑定
            if (token.Proxy.TargetEP.Address.Equals(IPAddress.Any) || command == Socks5EnumRequestCommand.UdpAssociate)
            {
                token.Proxy.TargetEP = null;
                byte[] response1 = Socks5Parser.MakeConnectResponse(proxyEP, (byte)Socks5EnumResponseCommand.ConnecSuccess);
                await token.Socket.SendAsync(response1.AsMemory());
                return false;
            }

            token.Connection = await ConnectTunnel(token.TargetIP);

            Socks5EnumResponseCommand resp = token.Connection != null && token.Connection.Connected ? Socks5EnumResponseCommand.ConnecSuccess : Socks5EnumResponseCommand.NetworkError;
            byte[] response = Socks5Parser.MakeConnectResponse(proxyEP, (byte)resp);
            await token.Socket.SendAsync(response.AsMemory());

            return true;
        }
        protected override async ValueTask ConnectTunnelConnection(AsyncUserUdpToken token)
        {
            ReadOnlyMemory<byte> ipArray = Socks5Parser.GetRemoteEndPoint(token.Proxy.Data, out Socks5EnumAddressType addressType, out ushort port, out int index);
            token.Proxy.TargetEP = new IPEndPoint(new IPAddress(ipArray.Span), port);
            token.TargetIP = BinaryPrimitives.ReadUInt32BigEndian(ipArray.Span);
            //解析出udp包的数据部分
            token.Proxy.Data = Socks5Parser.GetUdpData(token.Proxy.Data);
            token.Connection = await ConnectTunnel(token.TargetIP);
        }
        protected override async ValueTask CheckTunnelConnection(AsyncUserToken token)
        {
            if (token.Connection == null || token.Connection.Connected == false)
            {
                token.Connection = await ConnectTunnel(token.TargetIP);
            }
        }

        protected override async ValueTask<bool> ConnectionReceiveUdp(AsyncUserTunnelToken token, AsyncUserUdpToken asyncUserUdpToken)
        {
            byte[] data = Socks5Parser.MakeUdpResponse(token.Proxy.TargetEP, token.Proxy.Data, out int length);
            try
            {
                await asyncUserUdpToken.SourceSocket.SendAsync(data.AsMemory(0, length), token.Proxy.SourceEP);
            }
            catch (Exception)
            {
            }
            finally
            {
                Socks5Parser.Return(data);
            }
            return true;
        }


        SemaphoreSlim slimGlobal = new SemaphoreSlim(1);
        private async ValueTask<ITunnelConnection> ConnectTunnel(uint ip)
        {
            uint network = ip & maskValue;
            if (dic.TryGetValue(ip, out string targetName) == false && dic.TryGetValue(network, out targetName) == false)
            {
                return null;
            }
            if (dicConnections.TryGetValue(targetName, out ITunnelConnection connection) && connection.Connected)
            {
                return connection;
            }

            await slimGlobal.WaitAsync();
            if (dicLocks.TryGetValue(targetName, out SemaphoreSlim slim) == false)
            {
                slim = new SemaphoreSlim(1);
                dicLocks.TryAdd(targetName, slim);
            }
            slimGlobal.Release();

            await slim.WaitAsync();

            try
            {

                if (dicConnections.TryGetValue(targetName, out connection) && connection.Connected)
                {
                    return connection;
                }

                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG) Logger.Instance.Debug($"tuntap tunnel to {targetName}");

                connection = await tunnelTransfer.ConnectAsync(targetName, "tuntap");
                if (connection != null)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG) Logger.Instance.Debug($"tuntap tunnel success,{connection.ToString()}");
                }
                if (connection == null)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG) Logger.Instance.Debug($"tuntap relay to {targetName}");

                    connection = await relayTransfer.ConnectAsync(targetName, "tuntap");
                    if (connection != null)
                    {
                        if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG) Logger.Instance.Debug($"tuntap relay success,{connection.ToString()}");
                    }
                }
                if (connection != null)
                {
                    dicConnections.AddOrUpdate(targetName, connection, (a, b) => connection);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                slim.Release();
            }
            return connection;
        }

        private async Task<bool> ReceiveCommandData(AsyncUserToken token)
        {
            int totalLength = token.Proxy.Data.Length;
            EnumProxyValidateDataResult validate = ValidateData(token.Proxy);
            if ((validate & EnumProxyValidateDataResult.TooShort) == EnumProxyValidateDataResult.TooShort)
            {
                //太短
                while ((validate & EnumProxyValidateDataResult.TooShort) == EnumProxyValidateDataResult.TooShort)
                {
                    totalLength += await token.Socket.ReceiveAsync(token.Saea.Buffer.AsMemory(token.Saea.Offset + totalLength), SocketFlags.None);
                    token.Proxy.Data = token.Saea.Buffer.AsMemory(token.Saea.Offset, totalLength);
                    validate = ValidateData(token.Proxy);
                }
            }

            //不短，又不相等，直接关闭连接
            if ((validate & EnumProxyValidateDataResult.Equal) != EnumProxyValidateDataResult.Equal)
            {
                return false;
            }
            return true;
        }
        public EnumProxyValidateDataResult ValidateData(ProxyInfo info)
        {
            return (Socks5EnumStep)info.Rsv switch
            {
                Socks5EnumStep.Request => Socks5Parser.ValidateRequestData(info.Data),
                Socks5EnumStep.Command => Socks5Parser.ValidateCommandData(info.Data),
                Socks5EnumStep.Auth => Socks5Parser.ValidateAuthData(info.Data, Socks5EnumAuthType.Password),
                Socks5EnumStep.Forward => EnumProxyValidateDataResult.Equal,
                Socks5EnumStep.ForwardUdp => EnumProxyValidateDataResult.Equal,
                _ => EnumProxyValidateDataResult.Equal
            };
        }
    }
}
