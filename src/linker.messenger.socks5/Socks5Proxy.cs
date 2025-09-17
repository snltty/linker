using linker.tunnel;
using linker.tunnel.connection;
using linker.libs;
using linker.libs.socks5;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using linker.messenger.relay.client;
using linker.messenger.channel;
using linker.messenger.signin;
using linker.messenger.pcp;
using static linker.snat.WinDivert;
using System.Buffers;

namespace linker.messenger.socks5
{
    public partial class Socks5Proxy : Channel
    {
        private IPEndPoint proxyEP;
        protected override string TransactionId => "socks5";

        private readonly Socks5CidrDecenterManager socks5CidrDecenterManager;

        public Socks5Proxy(ISignInClientStore signInClientStore, TunnelTransfer tunnelTransfer, RelayClientTransfer relayTransfer, PcpTransfer pcpTransfer,
            SignInClientTransfer signInClientTransfer, IRelayClientStore relayClientStore, Socks5CidrDecenterManager socks5CidrDecenterManager)
             : base(tunnelTransfer, relayTransfer, pcpTransfer, signInClientTransfer, signInClientStore, relayClientStore)
        {
            this.socks5CidrDecenterManager = socks5CidrDecenterManager;
            TaskUdp();

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


        byte[] hostBytes = Encoding.UTF8.GetBytes("Host: ");
        byte[] newlineBytes = Encoding.UTF8.GetBytes("\r\n");
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

            if (token.Proxy.Data.Span.IndexOf(hostBytes) >= 0)
            {
                return await ProcessHttp(token).ConfigureAwait(false);
            }
            return await ProcessSocks5(token).ConfigureAwait(false);
        }
        private async Task<bool> ProcessHttp(AsyncUserToken token)
        {
            int start = token.Proxy.Data.Span.IndexOf(hostBytes) + hostBytes.Length;
            int end = token.Proxy.Data.Span.Slice(start).IndexOf(newlineBytes);
            string host = Encoding.UTF8.GetString(token.Proxy.Data.Span.Slice(start, end));

            IPEndPoint target = await NetworkHelper.GetEndPointAsync(host, 80);
            token.Proxy.TargetEP = target;
            uint targetIp = NetworkHelper.ToValue(target.Address);
            token.Connection = await ConnectTunnel(targetIp).ConfigureAwait(false);

            return true;
        }
        private async Task<bool> ProcessSocks5(AsyncUserToken token)
        {
            using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(1024);
            buffer.Memory.Span[0] = 0x05;
            buffer.Memory.Span[1] = 0x00;

            //步骤，request
            token.Proxy.Rsv = (byte)Socks5EnumStep.Request;
            if (await ReceiveCommandData(token).ConfigureAwait(false) == false)
            {
                return true;
            }
            await token.Socket.SendAsync(buffer.Memory.Slice(0, 2)).ConfigureAwait(false);

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
                await token.Socket.SendAsync(Socks5Parser.MakeConnectResponse(buffer.Memory, new IPEndPoint(IPAddress.Any, proxyEP.Port), (byte)Socks5EnumResponseCommand.ConnecSuccess)).ConfigureAwait(false);
                return false;
            }

            //获取远端地址
            uint targetIp;
            ReadOnlyMemory<byte> ipArray = Socks5Parser.GetRemoteEndPoint(token.Proxy.Data, out Socks5EnumAddressType addressType, out ushort port, out int index);
            token.Proxy.Data = token.Proxy.Data.Slice(index);
            //ipv6不支持
            if (addressType == Socks5EnumAddressType.IPV6)
            {
                await token.Socket.SendAsync(Socks5Parser.MakeConnectResponse(buffer.Memory, new IPEndPoint(IPAddress.Any, 0), (byte)Socks5EnumResponseCommand.AddressNotAllow)).ConfigureAwait(false);
                return true;
            }
            //解析域名
            if (addressType == Socks5EnumAddressType.Domain)
            {
                IPAddress ip = NetworkHelper.GetDomainIp(Encoding.UTF8.GetString(ipArray.Span));
                if (ip == null)
                {
                    await token.Socket.SendAsync(Socks5Parser.MakeConnectResponse(buffer.Memory, new IPEndPoint(IPAddress.Any, 0), (byte)Socks5EnumResponseCommand.AddressNotAllow)).ConfigureAwait(false);
                    return true;
                }
                token.Proxy.TargetEP = new IPEndPoint(ip, port);
                targetIp = BinaryPrimitives.ReadUInt32BigEndian(ip.GetAddressBytes());
            }
            else
            {
                token.Proxy.TargetEP = new IPEndPoint(new IPAddress(ipArray.Span), port);
                targetIp = BinaryPrimitives.ReadUInt32BigEndian(ipArray.Span);
            }
            //连接隧道
            token.Connection = await ConnectTunnel(targetIp).ConfigureAwait(false);

            //如果连接失败，返回错误
            Socks5EnumResponseCommand resp = token.Connection != null && token.Connection.Connected ? Socks5EnumResponseCommand.ConnecSuccess : Socks5EnumResponseCommand.NetworkError;
            await token.Socket.SendAsync(Socks5Parser.MakeConnectResponse(buffer.Memory, new IPEndPoint(IPAddress.Any, 0), (byte)resp)).ConfigureAwait(false);

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
            if (socks5CidrDecenterManager.FindValue(ip, out string machineId))
            {
                return await ConnectTunnel(machineId, TunnelProtocolType.Udp).ConfigureAwait(false);
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
                int length = await token.Socket.ReceiveAsync(token.Buffer.AsMemory(totalLength), SocketFlags.None).ConfigureAwait(false);
                if (length == 0) return false;
                totalLength += length;
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
