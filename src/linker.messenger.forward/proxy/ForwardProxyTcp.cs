using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.IO.Pipelines;
using System.Data;

namespace linker.messenger.forward.proxy
{
    /// <summary>
    /// 端口转发的TCP部分
    /// </summary>
    public partial class ForwardProxy
    {
        private readonly ConcurrentDictionary<int, Socket> tcpListens = new();
        private readonly ConcurrentDictionary<(uint srcAddr, ushort srcPort, uint dstAddr, ushort dstPort), AsyncUserToken> tcpConnections = new();
        private Socket socketTcp;
        public IPEndPoint LocalEndpoint { get; private set; }

        /// <summary>
        /// 启动TCP转发
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="bufferSize"></param>
        private void StartTcp(IPEndPoint ep, byte bufferSize)
        {
            IPEndPoint _localEndPoint = ep;
            socketTcp = new Socket(_localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socketTcp.IPv6Only(_localEndPoint.AddressFamily, false);
            socketTcp.Bind(_localEndPoint);
            socketTcp.Listen(int.MaxValue);

            LocalEndpoint = socketTcp.LocalEndPoint as IPEndPoint;
            _ = StartAcceptTcp(socketTcp, bufferSize).ConfigureAwait(false);
        }
        /// <summary>
        /// 开始接收TCP连接
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        private async Task StartAcceptTcp(Socket socket, byte bufferSize)
        {
            ushort port = (ushort)(socket.LocalEndPoint as IPEndPoint).Port;
            tcpListens.AddOrUpdate(port, socket, (a, b) => socket);
            try
            {
                while (true)
                {
                    Socket client = await socket.AcceptAsync().ConfigureAwait(false);
                    _ = ProcessAcceptTcp(client, port, bufferSize).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);

                tcpListens.TryRemove(port, out _);
                socket.SafeClose();
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
                        Pipe = new Pipe(new PipeOptions(minimumSegmentSize: 8192, pauseWriterThreshold: 2 * 1024 * 1024, resumeWriterThreshold: 512 * 1024, useSynchronizationContext: false)),
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

                    bool closeConnect = await Tunneling(token).ConfigureAwait(false);
                    if (token.Connection == null || token.ReadPacket.DstAddr == 0)
                    {
                        if (closeConnect)
                            token.Disponse();
                        return;
                    }

                    key = (0, token.ReadPacket.Port, token.ReadPacket.DstAddr, token.ReadPacket.DstPort);
                    tcpConnections.AddOrUpdate(key, token, (a, b) => token);

                    token.ReadPacket.Flag = ForwardFlags.Syn;
                    token.ReadPacket.Length = token.ReadPacket.HeaderLength;
                    await SendToConnection(token).ConfigureAwait(false);

                    await token.Tcs.WithTimeout(TimeSpan.FromMilliseconds(15000)).ConfigureAwait(false);

                    token.ReadPacket.Flag = ForwardFlags.Psh;
                    await Task.WhenAny(Sender(token), Recver(token, buffer, ForwardFlags.Psh)).ConfigureAwait(false);
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
            IPEndPoint ep = new IPEndPoint(NetworkHelper.ToIP(packet.DstAddr), packet.DstPort);
            if (HookConnect(connection.RemoteMachineId, ep, ProtocolType.Tcp) == false)
            {
                return;
            }

            byte[] buffer = ArrayPool<byte>.Shared.Rent((1 << packet.BufferSize) * 1024);
            (uint srcAddr, ushort srcPort, uint dstAddr, ushort dstPort) key = (0, 0, 0, 0);
            (byte bufferSize, ushort port, uint dstAddr, ushort dstPort) = (packet.BufferSize, packet.Port, packet.DstAddr, packet.DstPort);
            using CancellationTokenSource cts = new CancellationTokenSource(100);
            try
            {
                int length = memory.Length - packet.HeaderLength;
                if (length > 0) memory.Slice(packet.HeaderLength).CopyTo(buffer.AsMemory());

                Socket socket = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.KeepAlive();
                await socket.ConnectAsync(ep,cts.Token).ConfigureAwait(false);

                if (length > 0) await socket.SendAsync(buffer.AsMemory(0, length), SocketFlags.None).ConfigureAwait(false);

                IPEndPoint local = socket.LocalEndPoint as IPEndPoint;
                AsyncUserToken token = new AsyncUserToken
                {
                    Connection = connection,
                    Socket = socket,
                    ListenPort = local.Port,
                    IPEndPoint = ep,
                    Pipe = new Pipe(new PipeOptions(minimumSegmentSize: 8192, pauseWriterThreshold: 2 * 1024 * 1024, resumeWriterThreshold: 512 * 1024, useSynchronizationContext: false)),
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

                token.ReadPacket.Flag = ForwardFlags.PshAck;
                await Task.WhenAny(Sender(token), Recver(token, buffer, ForwardFlags.PshAck)).ConfigureAwait(false);
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
                    token.Sending = packet.BufferSize > 0;

                    memory = memory.Slice(packet.HeaderLength);
                    if (memory.Length > 0)
                    {
                        await token.Pipe.Writer.WriteAsync(memory).ConfigureAwait(false);
                        token.AddReceived(memory.Length);
                    }
                    if (token.NeedPause) await SendWindow(token, 0).ConfigureAwait(false);
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
                    token.Sending = packet.BufferSize > 0;

                    memory = memory.Slice(packet.HeaderLength);
                    if (memory.Length > 0)
                    {
                        await token.Pipe.Writer.WriteAsync(memory).ConfigureAwait(false);
                        token.AddReceived(memory.Length);
                    }
                    if (token.NeedPause) await SendWindow(token, 0).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error(ex);
                }
            }
        }

        private async Task SendWindow(AsyncUserToken token, byte window)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(token.ReadPacket.HeaderLength);
            try
            {
                token.ReadPacket.BufferSize = window;
                token.Receiving = window > 0;
                token.ReadPacket.Buffer.AsMemory(0, token.ReadPacket.HeaderLength).CopyTo(buffer);

                using ForwardReadPacket packet = new ForwardReadPacket(buffer);
                token.ReadPacket.Length = token.ReadPacket.HeaderLength;
                await SendToConnection(token.Connection, packet, token.IPEndPoint).ConfigureAwait(false);
                packet.Dispose();
            }
            catch (Exception)
            {
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        private async Task Sender(AsyncUserToken token)
        {
            while (true)
            {
                ReadResult result = await token.Pipe.Reader.ReadAsync().ConfigureAwait(false);
                if (result.IsCompleted && result.Buffer.IsEmpty)
                {
                    break;
                }
                ReadOnlySequence<byte> buffer = result.Buffer;
                foreach (ReadOnlyMemory<byte> memoryBlock in result.Buffer)
                {
                    await token.Socket.SendAsync(memoryBlock, SocketFlags.None).ConfigureAwait(false);
                    Add(token.Connection.RemoteMachineId, token.IPEndPoint, 0, memoryBlock.Length);
                    token.AddReceived(-memoryBlock.Length);
                    if (token.NeedResume) await SendWindow(token, 1).ConfigureAwait(false);
                }
                token.Pipe.Reader.AdvanceTo(buffer.End);
            }
        }
        private async Task Recver(AsyncUserToken token, byte[] buffer, ForwardFlags flag)
        {
            int bytesRead;
            while ((bytesRead = await token.Socket.ReceiveAsync(buffer.AsMemory(token.ReadPacket.HeaderLength), SocketFlags.None).ConfigureAwait(false)) != 0)
            {
                if (HookForward(token) == false)
                {
                    break;
                }

                token.ReadPacket.Flag = flag;
                token.ReadPacket.Length = bytesRead + token.ReadPacket.HeaderLength;
                await SendToConnection(token).ConfigureAwait(false);

                if (token.Sending == false)
                {
                    while (token.Sending == false && token.Socket != null)
                    {
                        await Task.Delay(10).ConfigureAwait(false);
                    }
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
            foreach (var item in tcpListens)
            {
                item.Value.SafeClose();
            }
            tcpListens.Clear();
            foreach (var item in tcpConnections)
            {
                item.Value?.Socket?.SafeClose();
            }
            tcpConnections.Clear();
        }
        /// <summary>
        /// 停止指定端口转发
        /// </summary>
        /// <param name="port"></param>
        private void StopTcp(int port)
        {
            if (tcpListens.TryRemove(port, out Socket socket))
            {
                socket.SafeClose();
            }
            foreach (var item in tcpConnections.Where(c => c.Value.ListenPort == port).ToList())
            {
                item.Value.Disponse();
                tcpConnections.TryRemove(item.Key, out _);
            }
        }

    }

}
