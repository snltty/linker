using linker.libs;
using linker.libs.extends;
using linker.tunnel.connection;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace linker.forward
{
    public partial class ForwardProxy
    {
        private readonly ConcurrentDictionary<int, Socket> tcpListens = new();
        private readonly ConcurrentDictionary<(uint srcAddr, ushort srcPort, uint dstAddr, ushort dstPort), AsyncUserToken> tcpConnections = new();

        public IPEndPoint LocalEndpoint { get; private set; }
        public bool Running => tcpListens.Count > 0;

        private void StartTcp(IPEndPoint ep, byte bufferSize)
        {
            IPEndPoint _localEndPoint = ep;
            Socket socket = new Socket(_localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.IPv6Only(_localEndPoint.AddressFamily, false);
            socket.Bind(_localEndPoint);
            socket.Listen(int.MaxValue);

            LocalEndpoint = socket.LocalEndPoint as IPEndPoint;

            _ = StartAcceptTcp(socket, bufferSize).ConfigureAwait(false);
        }

        private async Task StartAcceptTcp(Socket socket, byte bufferSize)
        {
            int hashcode = socket.GetHashCode();
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
                        token.Dispose();
                        return;
                    }

                    key = (0, token.ReadPacket.Port, token.ReadPacket.DstAddr, token.ReadPacket.DstPort);
                    tcpConnections.AddOrUpdate(key, token, (a, b) => token);

                    token.ReadPacket.Flag = ForwardFlags.Syn;
                    token.ReadPacket.Length = length;
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
                    token.ReadPacket.Length = 0;
                    await SendToConnection(token).ConfigureAwait(false);

                    token.Dispose();
                }
                else
                {
                    socket.SafeClose();
                }
            }
        }

        private async Task HandleSynTcp(ITunnelConnection connection, ForwardWritePacket packet, ReadOnlyMemory<byte> memory)
        {
            IPEndPoint target = new IPEndPoint(NetworkHelper.ToIP(packet.DstAddr), packet.DstPort);
            target.Address = MapIp(target.Address);
           
            if (HookConnect(connection.RemoteMachineId, target, ProtocolType.Tcp) == false)
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

                Socket socket = new Socket(target.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.KeepAlive();
                await socket.ConnectAsync(target, cts.Token).ConfigureAwait(false);
                if (length > 0) await socket.SendAllAsync(buffer.AsMemory(0, length)).ConfigureAwait(false);

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
                token.ReadPacket.Length = 0;
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
                    token.ReadPacket.Length = 0;
                    await SendToConnection(token).ConfigureAwait(false);
                    token.Dispose();
                }
            }
        }

        private void HandleSynAckTcp(ITunnelConnection connection, ForwardWritePacket packet)
        {
            if (tcpConnections.TryGetValue((0, packet.Port, packet.DstAddr, packet.DstPort), out AsyncUserToken token))
            {
                token.ReadPacket.SrcAddr = packet.SrcAddr;
                token.ReadPacket.SrcPort = packet.SrcPort;
                token.Tcs?.SetResult();
            }
        }
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

        private async ValueTask SendWindow(AsyncUserToken token, byte window)
        {
            token.ReadPacket.BufferSize = window;
            token.Receiving = window > 0;
            token.ReadPacket.Length = 0;
            await SendToConnection(token.Connection, token.ReadPacket, token.IPEndPoint).ConfigureAwait(false);
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
                    await token.Socket.SendAllAsync(memoryBlock).ConfigureAwait(false);
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
            while ((bytesRead = await token.Socket.ReceiveAsync(buffer.AsMemory(ForwardReadPacket.HeaderLength), SocketFlags.None).ConfigureAwait(false)) != 0)
            {
                if (HookForward(token) == false)
                {
                    break;
                }

                token.ReadPacket.Flag = flag;
                token.ReadPacket.Length = bytesRead;
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

        private void HandleRstTcp(ITunnelConnection connection, ForwardWritePacket packet)
        {
            if (tcpConnections.TryRemove((packet.SrcAddr, packet.SrcPort, packet.DstAddr, packet.DstPort), out AsyncUserToken token))
            {
                token.Dispose();
            }
        }

        private void HandleRstAckTcp(ITunnelConnection connection, ForwardWritePacket packet)
        {
            if (tcpConnections.TryRemove((0, packet.SrcPort, packet.DstAddr, packet.DstPort), out AsyncUserToken token))
            {
                token.Dispose();
            }
        }

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
        private void StopTcp(int port)
        {
            if (tcpListens.TryRemove(port, out Socket socket))
            {
                socket.SafeClose();
            }
            foreach (var item in tcpConnections.Where(c => c.Value.ListenPort == port).ToList())
            {
                item.Value.Dispose();
                tcpConnections.TryRemove(item.Key, out _);
            }
        }
    }
}
