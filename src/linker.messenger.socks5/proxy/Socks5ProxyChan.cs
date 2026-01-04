using linker.tunnel.connection;
using linker.libs;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using linker.libs.socks5;
using System.Buffers.Binary;
using System.Text;
using System.Collections.Concurrent;

namespace linker.messenger.socks5
{
    public partial class Socks5Proxy : ITunnelConnectionReceiveCallback
    {
        private readonly NumberSpace ns = new NumberSpace();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new ConcurrentDictionary<string, SemaphoreSlim>();
        private readonly SemaphoreSlim slimGlobal = new SemaphoreSlim(1);

        private ILinkerSocks5Hook[] hooks = [];

        public void Start(IPEndPoint ep, byte bufferSize)
        {
            StartTcp(ep, bufferSize);
            StartUdp(new IPEndPoint(ep.Address, LocalEndpoint.Port), bufferSize);
        }
        /// <summary>
        /// 隧道已连接
        /// </summary>
        /// <param name="connection"></param>
        protected override void Connected(ITunnelConnection connection)
        {
            connection.BeginReceive(this, null);
        }

        /// <summary>
        /// 锁
        /// </summary>
        /// <param name="machineId"></param>
        /// <returns></returns>
        protected override async ValueTask<bool> WaitAsync(string machineId)
        {
            //不要同时去连太多，锁以下
            await slimGlobal.WaitAsync().ConfigureAwait(false);
            if (locks.TryGetValue(machineId, out SemaphoreSlim slim) == false)
            {
                slim = new SemaphoreSlim(1);
                locks.TryAdd(machineId, slim);
            }
            slimGlobal.Release();
            await slim.WaitAsync().ConfigureAwait(false);
            return true;
        }
        /// <summary>
        /// 释放锁
        /// </summary>
        /// <param name="machineId"></param>
        protected override void WaitRelease(string machineId)
        {
            if (locks.TryGetValue(machineId, out SemaphoreSlim slim))
            {
                slim.Release();
            }
        }

        /// <summary>
        /// 隧道来数据了
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="memory"></param>
        /// <param name="userToken"></param>
        /// <returns></returns>
        public async Task Receive(ITunnelConnection connection, ReadOnlyMemory<byte> memory, object userToken)
        {
            await InputPacket(connection, memory).ConfigureAwait(false);
        }

        /// <summary>
        /// 隧道已关闭
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="userToken"></param>
        /// <returns></returns>
        public async Task Closed(ITunnelConnection connection, object userToken)
        {
            Version.Increment();
            await Task.CompletedTask.ConfigureAwait(false);
        }
        /// <summary>
        /// 从隧道连接发送数据到对方
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<bool> SendToConnection(AsyncUserToken token)
        {
            if (token.Connection == null)
            {
                return false;
            }
            await token.Connection.SendAsync(token.ReadPacket.Buffer.AsMemory(token.ReadPacket.Offset, token.ReadPacket.Length)).ConfigureAwait(false);
            Add(token.Connection.RemoteMachineId, token.IPEndPoint, token.ReadPacket.Length, 0);
            return true;
        }
        private async Task<bool> SendToConnection(ITunnelConnection connection, ForwardReadPacket packet, IPEndPoint ep)
        {
            if (connection == null)
            {
                return false;
            }
            await connection.SendAsync(packet.Buffer.AsMemory(packet.Offset, packet.Length)).ConfigureAwait(false);
            Add(connection.RemoteMachineId, ep, packet.Length, 0);
            return true;
        }


        /// <summary>
        /// 建立隧道
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async ValueTask<int> Tunneling(AsyncUserToken token, ProtocolType protocolType)
        {
            Memory<byte> memory = token.ReadPacket.Buffer.AsMemory(token.ReadPacket.HeaderLength);

            if (protocolType == ProtocolType.Tcp)
            {
                int length = await token.Socket.ReceiveAsync(memory, SocketFlags.None).ConfigureAwait(false);
                if (length == 0)
                {
                    return 0;
                }
                token.ReadPacket.DstAddr = 0;
                token.ReadPacket.DstPort = 0;
                token.ReadPacket.Length = token.ReadPacket.HeaderLength + length;

                if (memory.Span.IndexOf(hostBytes) >= 0)
                {
                    await ProcessHttp(token, memory).ConfigureAwait(false);
                    return memory.Span.IndexOf(connectBytes) == 0 ? 0 : length;
                }
                await ProcessSocks5(token, memory).ConfigureAwait(false);
                return 0;
            }

            ReadOnlyMemory<byte> ipArray = Socks5Parser.GetRemoteEndPoint(memory, out Socks5EnumAddressType addressType, out ushort port, out int index);
            if (addressType != Socks5EnumAddressType.IPV6)
            {
                token.ReadPacket.DstAddr = BinaryPrimitives.ReadUInt32BigEndian(ipArray.Span);
                token.ReadPacket.DstPort = port;
                token.IPEndPoint = new IPEndPoint(new IPAddress(ipArray.Span), port);

                var data = Socks5Parser.GetUdpData(memory);
                data.CopyTo(memory);
                token.ReadPacket.Length = token.ReadPacket.HeaderLength + data.Length;

                token.Connection = await ConnectTunnel(token.ReadPacket.DstAddr).ConfigureAwait(false);

                return data.Length;
            }
            return 0;
        }

        private readonly byte[] hostBytes = Encoding.UTF8.GetBytes("Host: ");
        private readonly byte[] newlineBytes = Encoding.UTF8.GetBytes("\r\n");
        private readonly byte[] connectBytes = Encoding.UTF8.GetBytes("CONNECT");
        private readonly byte[] connectSuccess = Encoding.UTF8.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n");
        private readonly byte[] connectFail = Encoding.UTF8.GetBytes("HTTP/1.1 502 Bad Gateway\r\n\r\n");
        private async ValueTask ProcessHttp(AsyncUserToken token, Memory<byte> memory)
        {
            int start = memory.Span.IndexOf(hostBytes) + hostBytes.Length;
            int end = memory.Span.Slice(start).IndexOf(newlineBytes);
            string host = Encoding.UTF8.GetString(memory.Span.Slice(start, end));
            IPEndPoint target = await NetworkHelper.GetEndPointAsync(host, 80);

            token.ReadPacket.DstAddr = NetworkHelper.ToValue(target.Address);
            token.ReadPacket.DstPort = (ushort)target.Port;
            token.IPEndPoint = target;

            token.Connection = await ConnectTunnel(token.ReadPacket.DstAddr).ConfigureAwait(false);

            //如果是CONNECT方法，则返回200
            if (memory.Span.IndexOf(connectBytes) == 0)
            {
                await token.Socket.SendAsync(token.Connection != null ? connectSuccess.AsMemory() : connectFail.AsMemory()).ConfigureAwait(false);
            }
        }
        private async ValueTask ProcessSocks5(AsyncUserToken token, Memory<byte> memory)
        {
            using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(1024);
            buffer.Memory.Span[0] = 0x05;
            buffer.Memory.Span[1] = 0x00;

            //步骤，request
            if (await ReceiveCommandData(token, memory, Socks5EnumStep.Request, token.ReadPacket.Length - token.ReadPacket.HeaderLength) == false) return;
            await token.Socket.SendAsync(buffer.Memory.Slice(0, 2)).ConfigureAwait(false);

            //步骤，command
            if (await ReceiveCommandData(token, memory, Socks5EnumStep.Command) == false) return;
            Socks5EnumRequestCommand command = (Socks5EnumRequestCommand)memory.Span[1];
            //是UDP中继，不做连接操作，等UDP数据过去的时候再绑定
            if (command == Socks5EnumRequestCommand.UdpAssociate)
            {
                await token.Socket.SendAsync(Socks5Parser.MakeConnectResponse(buffer.Memory, new IPEndPoint(IPAddress.Any, proxyEP.Port), (byte)Socks5EnumResponseCommand.ConnecSuccess)).ConfigureAwait(false);
                return;
            }

            //获取远端地址
            ReadOnlyMemory<byte> ipArray = Socks5Parser.GetRemoteEndPoint(memory, out Socks5EnumAddressType addressType, out ushort port, out int index);
            //ipv6不支持
            if (addressType == Socks5EnumAddressType.IPV6)
            {
                await token.Socket.SendAsync(Socks5Parser.MakeConnectResponse(buffer.Memory, new IPEndPoint(IPAddress.Any, 0), (byte)Socks5EnumResponseCommand.AddressNotAllow)).ConfigureAwait(false);
                return;
            }
            //解析域名
            if (addressType == Socks5EnumAddressType.Domain)
            {
                IPAddress ip = NetworkHelper.GetDomainIp(Encoding.UTF8.GetString(ipArray.Span));
                if (ip == null)
                {
                    await token.Socket.SendAsync(Socks5Parser.MakeConnectResponse(buffer.Memory, new IPEndPoint(IPAddress.Any, 0), (byte)Socks5EnumResponseCommand.AddressNotAllow)).ConfigureAwait(false);
                    return;
                }
                token.ReadPacket.DstAddr = NetworkHelper.ToValue(ip);
                token.ReadPacket.DstPort = port;
                token.IPEndPoint = new IPEndPoint(ip, port);
            }
            else
            {
                token.IPEndPoint = new IPEndPoint(new IPAddress(ipArray.Span), port);
                token.ReadPacket.DstAddr = NetworkHelper.ToValue(token.IPEndPoint.Address);
                token.ReadPacket.DstPort = port;
            }

            //连接隧道
            token.Connection = await ConnectTunnel(token.ReadPacket.DstAddr).ConfigureAwait(false);

            //如果连接失败，返回错误
            Socks5EnumResponseCommand resp = token.Connection != null && token.Connection.Connected ? Socks5EnumResponseCommand.ConnecSuccess : Socks5EnumResponseCommand.NetworkError;
            await token.Socket.SendAsync(Socks5Parser.MakeConnectResponse(buffer.Memory, new IPEndPoint(IPAddress.Any, 0), (byte)resp)).ConfigureAwait(false);

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
        private async ValueTask<bool> ReceiveCommandData(AsyncUserToken token, Memory<byte> memory, Socks5EnumStep step, int totalLength = 0)
        {
            using CancellationTokenSource cts = new CancellationTokenSource(3000);
            EnumProxyValidateDataResult validate = ValidateData(memory.Slice(0, totalLength), step);
            //太短
            while ((validate & EnumProxyValidateDataResult.TooShort) == EnumProxyValidateDataResult.TooShort)
            {
                int length = await token.Socket.ReceiveAsync(memory.Slice(totalLength), SocketFlags.None,cts.Token);
                if (length == 0) return false;
                totalLength += length;
                validate = ValidateData(memory.Slice(0, totalLength), step);
            }

            return (validate & EnumProxyValidateDataResult.Equal) == EnumProxyValidateDataResult.Equal;
        }
        /// <summary>
        /// 验证socks5数据包完整性
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private EnumProxyValidateDataResult ValidateData(Memory<byte> memory, Socks5EnumStep step)
        {
            return step switch
            {
                Socks5EnumStep.Request => Socks5Parser.ValidateRequestData(memory),
                Socks5EnumStep.Command => Socks5Parser.ValidateCommandData(memory),
                Socks5EnumStep.Auth => Socks5Parser.ValidateAuthData(memory, Socks5EnumAuthType.Password),
                Socks5EnumStep.Forward => EnumProxyValidateDataResult.Equal,
                Socks5EnumStep.ForwardUdp => EnumProxyValidateDataResult.Equal,
                _ => EnumProxyValidateDataResult.Equal
            };
        }
    }
}
