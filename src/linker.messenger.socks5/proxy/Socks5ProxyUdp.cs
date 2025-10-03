using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using linker.libs.timer;
using System.Buffers;
using linker.libs.socks5;

namespace linker.messenger.socks5
{
    public partial class Socks5Proxy
    {
        private Socket socketUdp;
        private readonly ConcurrentDictionary<(int srcId, uint srcAddr, ushort srcPort, uint dstAddr, ushort dstPort), AsyncUserToken> udpConnections = new();

        /// <summary>
        /// 开始UDP转发
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="buffersize"></param>
        private void StartUdp(IPEndPoint ep, byte buffersize)
        {
            try
            {
                socketUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socketUdp.EnableBroadcast = true;
                socketUdp.WindowsUdpBug();
                socketUdp.Bind(ep);

                _ = ReceiveFromAsync(socketUdp, (ushort)ep.Port, buffersize).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
            }
        }
        /// <summary>
        /// 开始接收UDP数据
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="port"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        private async Task ReceiveFromAsync(Socket socket, ushort port, byte bufferSize)
        {
            int hashcode = socketUdp.GetHashCode();
            byte[] buffer = ArrayPool<byte>.Shared.Rent(65535);
            AsyncUserToken token = new AsyncUserToken
            {
                Socket = socket,
                ListenPort = port,
                ReadPacket = new ForwardReadPacket(buffer)
                {
                    ProtocolType = ProtocolType.Udp,
                    BufferSize = bufferSize,
                    Port = 0,
                    SrcAddr = 0,
                    SrcPort = 0,
                    DstAddr = 0,
                    DstPort = 0
                }
            };
            try
            {
                IPEndPoint ep = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
                while (true)
                {
                    SocketReceiveFromResult result = await socket.ReceiveFromAsync(buffer.AsMemory(token.ReadPacket.HeaderLength), ep).ConfigureAwait(false);
                    if (result.ReceivedBytes == 0)
                    {
                        continue;
                    }
                    token.ReadPacket.Flag = ForwardFlags.Psh;
                    token.ReadPacket.Length = token.ReadPacket.HeaderLength + result.ReceivedBytes;
                    token.ReadPacket.SrcAddr = NetworkHelper.ToValue((result.RemoteEndPoint as IPEndPoint).Address);
                    token.ReadPacket.SrcPort = (ushort)(result.RemoteEndPoint as IPEndPoint).Port;
                    int length = await Tunneling(token, ProtocolType.Udp).ConfigureAwait(false);
                    if (token.ReadPacket.DstAddr > 0 && token.Connection != null)
                    {
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
                token.Disponse();
                if (socketUdp != null && socketUdp.GetHashCode() == hashcode)
                    StopUdp();
            }
        }

        /// <summary>
        /// 处理UDP PSH
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="packet"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        private async Task HndlePshUdp(ITunnelConnection connection, ForwardWritePacket packet, ReadOnlyMemory<byte> memory)
        {
            var connectId = (connection.RemoteMachineId.GetHashCode(), packet.SrcAddr, packet.SrcPort, packet.DstAddr, packet.DstPort);
            try
            {
                if (udpConnections.TryGetValue(connectId, out AsyncUserToken token))
                {
                    token.Connection = connection;
                    Add(connection.RemoteMachineId, token.IPEndPoint, 0, memory.Length);
                    await token.Socket.SendToAsync(memory.Slice(packet.HeaderLength), token.IPEndPoint).ConfigureAwait(false);
                    token.LastTicks.Update();
                    return;
                }
                _ = ConnectUdp(connection, packet, memory).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
                if (udpConnections.TryRemove(connectId, out AsyncUserToken token))
                {
                    token.Disponse();
                }
            }
        }
        /// <summary>
        /// 处理UDP PSH+ACK
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="packet"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        private async Task HndlePshAckUdp(ITunnelConnection connection, ForwardWritePacket packet, ReadOnlyMemory<byte> memory)
        {
            if (socketUdp != null)
            {
                IPEndPoint src = new IPEndPoint(NetworkHelper.ToIP(packet.SrcAddr), packet.SrcPort);
                IPEndPoint dst = new IPEndPoint(NetworkHelper.ToIP(packet.DstAddr), packet.DstPort);

                using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(memory.Length + 1024);
                int length = Socks5Parser.MakeUdpResponse(dst, memory.Slice(packet.HeaderLength), memoryOwner.Memory);
                try
                {
                    Add(connection.RemoteMachineId, dst, 0, memory.Length);
                    await socketUdp.SendToAsync(memoryOwner.Memory.Slice(0, length), src).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error(ex);
                }
            }
        }

        private async Task ConnectUdp(ITunnelConnection connection, ForwardWritePacket packet, ReadOnlyMemory<byte> memory)
        {
            IPEndPoint target = new IPEndPoint(NetworkHelper.ToIP(packet.DstAddr), packet.DstPort);
            target.Address = socks5CidrDecenterManager.GetMapRealDst(target.Address);

            if (HookConnect(connection.RemoteMachineId, target, ProtocolType.Udp) == false)
            {
                return;
            }
            var connectId = (connection.RemoteMachineId.GetHashCode(), packet.SrcAddr, packet.SrcPort, packet.DstAddr, packet.DstPort);
            (byte bufferSize, ushort port, uint srcAddr, ushort srcPort, uint dstAddr, ushort dstPort, byte hlen) = (packet.BufferSize, packet.Port, packet.SrcAddr, packet.SrcPort, packet.DstAddr, packet.DstPort, packet.HeaderLength);

            byte[] buffer = ArrayPool<byte>.Shared.Rent(65535);
            try
            {
                Socket socket = new Socket(target.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                socket.WindowsUdpBug();
                await socket.SendToAsync(memory.Slice(hlen), target).ConfigureAwait(false);

                AsyncUserToken token = new AsyncUserToken
                {
                    Connection = connection,
                    Socket = socket,
                    ListenPort = 0,
                    IPEndPoint = target,
                    ReadPacket = new ForwardReadPacket(buffer)
                    {
                        ProtocolType = ProtocolType.Udp,
                        BufferSize = bufferSize,
                        Port = port,
                        SrcAddr = srcAddr,
                        SrcPort = srcPort,
                        DstAddr = dstAddr,
                        DstPort = dstPort
                    }
                };
                udpConnections.AddOrUpdate(connectId, token, (a, b) => token);

                while (true)
                {
                    SocketReceiveFromResult result = await socket.ReceiveFromAsync(buffer.AsMemory(token.ReadPacket.HeaderLength), SocketFlags.None, target).ConfigureAwait(false);

                    if (result.ReceivedBytes == 0)
                    {
                        continue;
                    }
                    if (HookForward(token) == false)
                    {
                        break;
                    }

                    token.LastTicks.Update();
                    token.ReadPacket.Flag = ForwardFlags.PshAck;
                    token.ReadPacket.Length = token.ReadPacket.HeaderLength + result.ReceivedBytes;
                    Add(connection.RemoteMachineId, target, result.ReceivedBytes, 0);

                    await SendToConnection(token).ConfigureAwait(false);
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
                if (udpConnections.TryRemove(connectId, out AsyncUserToken token))
                {
                    token.Disponse();
                }
            }
        }

        /// <summary>
        /// 检查udp过期
        /// </summary>
        private void TaskUdp()
        {
            TimerHelper.SetIntervalLong(() =>
            {
                if (udpConnections.IsEmpty == false)
                {
                    var connections = udpConnections.Where(c => c.Value.Timeout).Select(c => c.Key).ToList();
                    foreach (var item in connections)
                    {
                        if (udpConnections.TryRemove(item, out AsyncUserToken token))
                        {
                            token.Disponse();
                        }
                    }
                }
            }, 30000);
        }

        /// <summary>
        /// 停止UDP所有转发
        /// </summary>
        private void StopUdp()
        {
            socketUdp?.SafeClose();
            socketUdp = null;

            foreach (var item in udpConnections)
            {
                item.Value.Disponse();
            }
            udpConnections.Clear();
        }
    }
}
