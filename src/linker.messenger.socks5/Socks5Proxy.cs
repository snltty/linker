using linker.tunnel;
using linker.tunnel.connection;
using linker.libs;
using linker.libs.socks5;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using linker.messenger.relay.client;
using linker.messenger.channel;
using linker.messenger.signin;
using linker.messenger.pcp;

namespace linker.messenger.socks5
{
    public sealed partial class TunnelProxy : Channel
    {
        private IPEndPoint proxyEP;

        private uint[] maskValues = Array.Empty<uint>();
        private readonly ConcurrentDictionary<uint, string> ip2MachineDic = new ConcurrentDictionary<uint, string>();

        private readonly SignInClientTransfer signInClientTransfer;

        protected override string TransactionId => "socks5";

        public TunnelProxy(ISignInClientStore signInClientStore, TunnelTransfer tunnelTransfer, RelayClientTransfer relayTransfer, PcpTransfer pcpTransfer, SignInClientTransfer signInClientTransfer, IRelayClientStore relayClientStore)
             : base(tunnelTransfer, relayTransfer, pcpTransfer, signInClientTransfer, signInClientStore, relayClientStore)
        {
            this.signInClientTransfer = signInClientTransfer;
            TaskUdp();
        }

        /// <summary>
        /// 设置IP，等下有连接进来，用IP匹配，才能知道这个连接是要连谁
        /// </summary>
        /// <param name="ips"></param>
        public void SetIPs(Socks5LanIPAddress[] ips)
        {
            var dic = ips.GroupBy(c => c.NetWork).ToDictionary(c => c.Key, d => d.Select(e => e.MachineId).ToList());
            foreach (var item in dic.Where(c => c.Value.Count > 0))
            {
                string machineId = item.Value[0];
                ip2MachineDic.AddOrUpdate(item.Key, machineId, (a, b) => machineId);
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
            ip2MachineDic.AddOrUpdate(ip, machineId, (a, b) => machineId);
        }


        public void Start(int port)
        {
            if (proxyEP != null)
            {
                Stop(proxyEP.Port);
            }
            Start(new IPEndPoint(IPAddress.Any, port), 3);
            proxyEP = new IPEndPoint(IPAddress.Any, LocalEndpoint.Port);
        }

        protected override void Connected(ITunnelConnection connection)
        {
            BindConnectionReceive(connection);
        }
        /// <summary>
        /// 接收隧道的数据
        /// </summary>
        /// <param name="connection"></param>
        private void BindConnectionReceive(ITunnelConnection connection)
        {
            connection.BeginReceive(this, new AsyncUserTunnelToken
            {
                Connection = connection,
                Proxy = new ProxyInfo { }
            });
        }

        /// <summary>
        /// tcp连接隧道
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async ValueTask<bool> ConnectTunnelConnection(AsyncUserToken token)
        {
            int length = await token.Socket.ReceiveAsync(token.Buffer.AsMemory(), SocketFlags.None).ConfigureAwait(false);
            if (length == 0)
            {
                return true;
            }
            token.Proxy.Data = token.Buffer.AsMemory(0, length);
            token.Proxy.TargetEP = null;


            //步骤，request
            token.Proxy.Rsv = (byte)Socks5EnumStep.Request;
            if (await ReceiveCommandData(token).ConfigureAwait(false) == false)
            {
                return true;
            }
            await token.Socket.SendAsync(new byte[] { 0x05, 0x00 }).ConfigureAwait(false);


            //步骤，command
            token.Proxy.Data = Helper.EmptyArray;
            token.Proxy.Rsv = (byte)Socks5EnumStep.Command;
            if (await ReceiveCommandData(token).ConfigureAwait(false) == false)
            {
                return true;
            }
            Socks5EnumRequestCommand command = (Socks5EnumRequestCommand)token.Proxy.Data.Span[1];
            //是UDP中继，不做连接操作，等UDP数据过去的时候再绑定
            if (command == Socks5EnumRequestCommand.UdpAssociate)
            {
                await token.Socket.SendAsync(Socks5Parser.MakeConnectResponse(new IPEndPoint(IPAddress.Any, proxyEP.Port), (byte)Socks5EnumResponseCommand.ConnecSuccess).AsMemory()).ConfigureAwait(false);
                return false;
            }


            //获取远端地址
            uint targetIP;
            ReadOnlyMemory<byte> ipArray = Socks5Parser.GetRemoteEndPoint(token.Proxy.Data, out Socks5EnumAddressType addressType, out ushort port, out int index);
            token.Proxy.Data = token.Proxy.Data.Slice(index);
            if (addressType == Socks5EnumAddressType.IPV6)
            {
                byte[] response1 = Socks5Parser.MakeConnectResponse(new IPEndPoint(IPAddress.Any, 0), (byte)Socks5EnumResponseCommand.AddressNotAllow);
                await token.Socket.SendAsync(response1.AsMemory()).ConfigureAwait(false);
                return true;
            }
            if (addressType == Socks5EnumAddressType.Domain)
            {
                if (IPAddress.TryParse(Encoding.UTF8.GetString(ipArray.Span), out IPAddress ip) == false)
                {
                    byte[] response1 = Socks5Parser.MakeConnectResponse(new IPEndPoint(IPAddress.Any, 0), (byte)Socks5EnumResponseCommand.AddressNotAllow);
                    await token.Socket.SendAsync(response1.AsMemory()).ConfigureAwait(false);
                    return true;
                }
                token.Proxy.TargetEP = new IPEndPoint(ip, port);
                targetIP = BinaryPrimitives.ReadUInt32BigEndian(ip.GetAddressBytes());
            }
            else
            {
                token.Proxy.TargetEP = new IPEndPoint(new IPAddress(ipArray.Span), port);
                targetIP = BinaryPrimitives.ReadUInt32BigEndian(ipArray.Span);
            }



            token.Connection = await ConnectTunnel(targetIP).ConfigureAwait(false);

            Socks5EnumResponseCommand resp = token.Connection != null && token.Connection.Connected ? Socks5EnumResponseCommand.ConnecSuccess : Socks5EnumResponseCommand.NetworkError;
            byte[] response = Socks5Parser.MakeConnectResponse(new IPEndPoint(IPAddress.Any, 0), (byte)resp);
            await token.Socket.SendAsync(response.AsMemory()).ConfigureAwait(false);

            return true;
        }
        /// <summary>
        /// udp连接隧道
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async ValueTask ConnectTunnelConnection(AsyncUserUdpToken token)
        {
            ReadOnlyMemory<byte> ipArray = Socks5Parser.GetRemoteEndPoint(token.Proxy.Data, out Socks5EnumAddressType addressType, out ushort port, out int index);
            if (addressType == Socks5EnumAddressType.IPV6)
            {
                return;
            }

            uint targetIP = BinaryPrimitives.ReadUInt32BigEndian(ipArray.Span);
            token.Proxy.TargetEP = new IPEndPoint(new IPAddress(ipArray.Span), port);
            //解析出udp包的数据部分
            token.Proxy.Data = Socks5Parser.GetUdpData(token.Proxy.Data);

            token.Connection = await ConnectTunnel(targetIP).ConfigureAwait(false);
        }

        /// <summary>
        /// udp回复消息自定义处理，因为socks5，要打包一些格式才能返回
        /// </summary>
        /// <param name="token"></param>
        /// <param name="asyncUserUdpToken"></param>
        /// <returns></returns>
        private async ValueTask<bool> ConnectionReceiveUdp(AsyncUserTunnelToken token, AsyncUserUdpToken asyncUserUdpToken)
        {
            IPEndPoint target = token.Proxy.TargetEP;
            byte[] data = Socks5Parser.MakeUdpResponse(target, token.Proxy.Data, out int length);
            try
            {
                await asyncUserUdpToken.SourceSocket.SendToAsync(data.AsMemory(0, length), token.Proxy.SourceEP).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            finally
            {
                Socks5Parser.Return(data);
            }
            return true;
        }

        /// <summary>
        /// 打洞或者中继
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        private async ValueTask<ITunnelConnection> ConnectTunnel(uint ip)
        {
            //匹配掩码查找
            for (int i = 0; i < maskValues.Length; i++)
            {
                uint network = ip & maskValues[i];
                if (ip2MachineDic.TryGetValue(network, out string machineId))
                {
                    return await ConnectTunnel(machineId, TunnelProtocolType.Udp).ConfigureAwait(false);
                }
            }

            return null;

        }
        /// <summary>
        /// 接收socks5完整数据包
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<bool> ReceiveCommandData(AsyncUserToken token)
        {
            int totalLength = token.Proxy.Data.Length;
            EnumProxyValidateDataResult validate = ValidateData(token.Proxy);
            //太短
            while ((validate & EnumProxyValidateDataResult.TooShort) == EnumProxyValidateDataResult.TooShort)
            {
                totalLength += await token.Socket.ReceiveAsync(token.Buffer.AsMemory(totalLength), SocketFlags.None).ConfigureAwait(false);
                token.Proxy.Data = token.Buffer.AsMemory(0, totalLength);
                validate = ValidateData(token.Proxy);
            }

            return (validate & EnumProxyValidateDataResult.Equal) == EnumProxyValidateDataResult.Equal;
        }
        /// <summary>
        /// 验证socks5数据包完整性
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private EnumProxyValidateDataResult ValidateData(ProxyInfo info)
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
