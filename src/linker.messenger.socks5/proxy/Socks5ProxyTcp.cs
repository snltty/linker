using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.socks5
{
    public partial class Socks5Proxy
    {
        private readonly ConcurrentDictionary<(uint srcAddr, ushort srcPort, uint dstAddr, ushort dstPort), AsyncUserToken> tcpConnections = new();
        public IPEndPoint LocalEndpoint { get; private set; }

        private Socket listenSocketTcp;
        public bool Running => listenSocketTcp != null;


        /// <summary>
        /// 启动TCP转发
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="bufferSize"></param>
        private void StartTcp(IPEndPoint ep, byte bufferSize)
        {
            IPEndPoint _localEndPoint = ep;
            listenSocketTcp = new Socket(_localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocketTcp.IPv6Only(_localEndPoint.AddressFamily, false);
            listenSocketTcp.Bind(_localEndPoint);
            listenSocketTcp.Listen(int.MaxValue);

            LocalEndpoint = listenSocketTcp.LocalEndPoint as IPEndPoint;
            _ = StartAcceptTcp(bufferSize);
        }
        /// <summary>
        /// 开始接收TCP连接
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        private async Task StartAcceptTcp(byte bufferSize)
        {
            int hashcode = listenSocketTcp.GetHashCode();

            ushort port = (ushort)(listenSocketTcp.LocalEndPoint as IPEndPoint).Port;
            try
            {
                while (true)
                {
                    Socket client = await listenSocketTcp.AcceptAsync().ConfigureAwait(false);
                    _ = ProcessAcceptTcp(client, port, bufferSize);
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);

                if (listenSocketTcp != null && listenSocketTcp.GetHashCode() == hashcode)
                    StopTcp();
            }

        }
        /// <summary>
        /// 处理接收的TCP连接
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="listenPort"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        private async Task ProcessAcceptTcp(Socket socket, ushort listenPort, byte bufferSize)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent((1 << bufferSize) * 1024);
            (uint srcAddr, ushort srcPort, uint dstAddr, ushort dstPort) key = (0, 0, 0, 0);
            try
            {
                if (socket != null && socket.RemoteEndPoint != null)
                {
                    socket.KeepAlive();
                    AsyncUserToken token = new AsyncUserToken
                    {
                        Socket = socket,
                        ListenPort = listenPort,
                        Tcs = new TaskCompletionSource(),
                        ReadPacket = new ForwardReadPacket(buffer)
                        {
                            ProtocolType = ProtocolType.Tcp,
                            BufferSize = bufferSize,
                            Port = (ushort)(socket.RemoteEndPoint as IPEndPoint).Port,
                            SrcAddr = 0,
                            SrcPort = 0,
                            DstAddr = 0,
                            DstPort = 0
                        }
                    };

                    int length = await Tunneling(token, ProtocolType.Tcp).ConfigureAwait(false);
                    if (token.Connection == null || token.ReadPacket.DstAddr == 0)
                    {
                        token.Disponse();
                        return;
                    }

                    key = (0, token.ReadPacket.Port, token.ReadPacket.DstAddr, token.ReadPacket.DstPort);
                    tcpConnections.AddOrUpdate(key, token, (a, b) => token);

                    token.ReadPacket.Flag = ForwardFlags.Syn;
                    token.ReadPacket.Length = token.ReadPacket.HeaderLength + length;
                    await SendToConnection(token).ConfigureAwait(false);

                    await token.Tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(15000)).ConfigureAwait(false);

                    while (true)
                    {
                        length = await token.Socket.ReceiveAsync(buffer.AsMemory(token.ReadPacket.HeaderLength), SocketFlags.None).ConfigureAwait(false);

                        if (length == 0 || HookForward(token) == false)
                        {
                            break;
                        }

                        token.ReadPacket.Flag = ForwardFlags.Psh;
                        token.ReadPacket.Length = token.ReadPacket.HeaderLength + length;
                        await SendToConnection(token).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                if (tcpConnections.TryRemove(key, out AsyncUserToken token))
                {
                    token.ReadPacket.Flag = ForwardFlags.Rst;
                    token.ReadPacket.Length = token.ReadPacket.HeaderLength;
                    await SendToConnection(token).ConfigureAwait(false);
                    token.Disponse();
                }
                else
                {
                    socket.SafeClose();
                }
            }
        }

        /// <summary>
        /// B端处理SYN
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        private async Task HandleSynTcp(ITunnelConnection connection, ForwardWritePacket packet, ReadOnlyMemory<byte> memory)
        {
            IPEndPoint target = new IPEndPoint(NetworkHelper.ToIP(packet.DstAddr), packet.DstPort);
            target.Address = socks5CidrDecenterManager.GetMapRealDst(target.Address);

            if (HookConnect(connection.RemoteMachineId, target, ProtocolType.Tcp) == false) return;

            byte[] buffer = ArrayPool<byte>.Shared.Rent((1 << packet.BufferSize) * 1024);
            (uint srcAddr, ushort srcPort, uint dstAddr, ushort dstPort) key = (0, 0, 0, 0);
            (byte bufferSize, ushort port, uint dstAddr, ushort dstPort) = (packet.BufferSize, packet.Port, packet.DstAddr, packet.DstPort);
            try
            {
                int length = memory.Length - packet.HeaderLength;
                if (length > 0) memory.Slice(packet.HeaderLength).CopyTo(buffer.AsMemory());

                Socket socket = new Socket(target.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.KeepAlive();
                await socket.ConnectAsync(target).ConfigureAwait(false);
                if (length > 0) await socket.SendAsync(buffer.AsMemory(0, length), SocketFlags.None).ConfigureAwait(false);

                IPEndPoint local = socket.LocalEndPoint as IPEndPoint;
                AsyncUserToken token = new AsyncUserToken
                {
                    Connection = connection,
                    Socket = socket,
                    ListenPort = local.Port,
                    IPEndPoint = target,
                    ReadPacket = new ForwardReadPacket(buffer)
                    {
                        ProtocolType = ProtocolType.Tcp,
                        BufferSize = bufferSize,
                        Port = port,
                        SrcAddr = NetworkHelper.ToValue(local.Address),
                        SrcPort = (ushort)local.Port,
                        DstAddr = dstAddr,
                        DstPort = dstPort
                    }
                };
                key = (token.ReadPacket.SrcAddr, token.ReadPacket.SrcPort, token.ReadPacket.DstAddr, token.ReadPacket.DstPort);
                tcpConnections.AddOrUpdate(key, token, (a, b) => token);

                token.ReadPacket.Flag = ForwardFlags.SynAck;
                token.ReadPacket.Length = token.ReadPacket.HeaderLength;
                await SendToConnection(token).ConfigureAwait(false);

                while (true)
                {
                    length = await token.Socket.ReceiveAsync(buffer.AsMemory(token.ReadPacket.HeaderLength), SocketFlags.None).ConfigureAwait(false);
                    if (length == 0 || HookForward(token) == false) break;

                    token.ReadPacket.Flag = ForwardFlags.PshAck;
                    token.ReadPacket.Length = token.ReadPacket.HeaderLength + length;
                    await SendToConnection(token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error($"connect error -> {ex}");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);

                if (tcpConnections.TryRemove(key, out AsyncUserToken token))
                {
                    token.ReadPacket.Flag = ForwardFlags.RstAck;
                    token.ReadPacket.Length = token.ReadPacket.HeaderLength;
                    await SendToConnection(token).ConfigureAwait(false);
                    token.Disponse();
                }
            }
        }
        /// <summary>
        /// A端处理SYN+ACK
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="packet"></param>
        private void HandleSynAckTcp(ITunnelConnection connection, ForwardWritePacket packet)
        {
            if (tcpConnections.TryGetValue((0, packet.Port, packet.DstAddr, packet.DstPort), out AsyncUserToken token))
            {
                token.ReadPacket.SrcAddr = packet.SrcAddr;
                token.ReadPacket.SrcPort = packet.SrcPort;
                token.Tcs?.SetResult();
            }
        }
        /// <summary>
        /// B端处理PSH
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="packet"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        private async ValueTask HandlePshTcp(ITunnelConnection connection, ForwardWritePacket packet, ReadOnlyMemory<byte> memory)
        {
            if (tcpConnections.TryGetValue((packet.SrcAddr, packet.SrcPort, packet.DstAddr, packet.DstPort), out AsyncUserToken token))
            {
                try
                {
                    token.Connection = connection;
                    Add(token.Connection.RemoteMachineId, token.IPEndPoint, 0, memory.Length);
                    await token.Socket.SendAsync(memory.Slice(packet.HeaderLength), SocketFlags.None).AsTask().WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error(ex);
                }
            }
        }
        /// <summary>
        /// A端处理PSH+ACK
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="packet"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        private async ValueTask HandlePshAckTcp(ITunnelConnection connection, ForwardWritePacket packet, ReadOnlyMemory<byte> memory)
        {
            if (tcpConnections.TryGetValue((0, packet.Port, packet.DstAddr, packet.DstPort), out AsyncUserToken token))
            {
                try
                {
                    token.Connection = connection;
                    Add(token.Connection.RemoteMachineId, token.IPEndPoint, 0, memory.Length);
                    await token.Socket.SendAsync(memory.Slice(packet.HeaderLength), SocketFlags.None).AsTask().WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error(ex);
                }
            }
        }
        /// <summary>
        /// B端处理RST
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="packet"></param>
        private void HandleRstTcp(ITunnelConnection connection, ForwardWritePacket packet)
        {
            if (tcpConnections.TryRemove((packet.SrcAddr, packet.SrcPort, packet.DstAddr, packet.DstPort), out AsyncUserToken token))
            {
                token.Disponse();
            }
        }
        /// <summary>
        /// A端处理RST+ACK
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="packet"></param>
        private void HandleRstAckTcp(ITunnelConnection connection, ForwardWritePacket packet)
        {
            if (tcpConnections.TryRemove((0, packet.SrcPort, packet.DstAddr, packet.DstPort), out AsyncUserToken token))
            {
                token.Disponse();
            }
        }

        /// <summary>
        /// 停止所有转发
        /// </summary>
        private void StopTcp()
        {
            listenSocketTcp?.SafeClose();
            listenSocketTcp = null;

            foreach (var item in tcpConnections)
            {
                item.Value?.Socket?.SafeClose();
            }
            tcpConnections.Clear();
        }

    }

}
