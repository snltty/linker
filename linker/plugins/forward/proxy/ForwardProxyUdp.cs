using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace linker.plugins.forward.proxy
{
    public partial class ForwardProxy
    {
        private ConcurrentDictionary<int, AsyncUserUdpToken> udpListens = new ConcurrentDictionary<int, AsyncUserUdpToken>();
        private ConcurrentDictionary<ConnectIdUdp, AsyncUserUdpTokenTarget> udpConnections = new(new ConnectIdUdpComparer());

        /// <summary>
        /// 监听一个端口
        /// </summary>
        /// <param name="port"></param>
        private void StartUdp(IPEndPoint ep, byte buffersize)
        {
            try
            {
                Socket socketUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socketUdp.EnableBroadcast = true;
                socketUdp.WindowsUdpBug();
                socketUdp.Bind(ep);
                AsyncUserUdpToken asyncUserUdpToken = new AsyncUserUdpToken
                {
                    ListenPort = ep.Port,
                    SourceSocket = socketUdp,
                    Proxy = new ProxyInfo { Port = (ushort)ep.Port, Step = ProxyStep.Forward, ConnectId = 0, Protocol = ProxyProtocol.Udp, Direction = ProxyDirection.Forward }
                };
                udpListens.AddOrUpdate(ep.Port, asyncUserUdpToken, (a, b) => asyncUserUdpToken);

                _ = ReceiveUdp(asyncUserUdpToken, buffersize);

            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
        }
        private async Task ReceiveUdp(AsyncUserUdpToken token, byte buffersize)
        {
            byte[] bytes = new byte[(1 << buffersize) * 1024];
            IPEndPoint tempRemoteEP = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
            while (true)
            {
                try
                {
                    SocketReceiveFromResult result = await token.SourceSocket.ReceiveFromAsync(bytes, tempRemoteEP).ConfigureAwait(false);

                    token.Proxy.SourceEP = result.RemoteEndPoint as IPEndPoint;
                    token.Proxy.Data = bytes.AsMemory(0, result.ReceivedBytes);
                    await ConnectTunnelConnection(token).ConfigureAwait(false);
                    if (token.Proxy.TargetEP != null)
                    {
                        if (token.Connection != null)
                        {
                            await SendToConnection(token).ConfigureAwait(false);
                        }
                        else if (token.Connections != null && token.Connections.Count > 0)
                        {
                            await SendToConnections(token).ConfigureAwait(false);
                        }
                    }

                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                    break;
                }
            }
            CloseClientSocket(token);
        }
        /// <summary>
        /// 发送给对方，通过隧道
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task SendToConnection(AsyncUserUdpToken token)
        {
            if (token.Connection == null) return;

            byte[] connectData = token.Proxy.ToBytes(out int length);
            try
            {
                bool res = await token.Connection.SendAsync(connectData.AsMemory(0, length)).ConfigureAwait(false);
                if (res == false)
                {
                    if (token.Connection.Connected == false)
                        await ConnectTunnelConnection(token).ConfigureAwait(false);
                    res = await token.Connection.SendAsync(connectData.AsMemory(0, length)).ConfigureAwait(false);

                    if (res == false)
                        CloseClientSocket(token);
                }
            }
            catch (Exception)
            {
                CloseClientSocket(token);
            }
            finally
            {
                token.Proxy.Return(connectData);
            }
        }
        /// <summary>
        /// 发送给很多对方，通过隧道
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task SendToConnections(AsyncUserUdpToken token)
        {
            byte[] connectData = token.Proxy.ToBytes(out int length);
            try
            {
                await Task.WhenAll(token.Connections.Select(c => c.SendAsync(connectData.AsMemory(0, length)))).ConfigureAwait(false);
            }
            catch (Exception)
            {
                CloseClientSocket(token);
            }
            finally
            {
                token.Proxy.Return(connectData);
            }
        }

        /// <summary>
        /// 收到隧道数据，确定是udp，该连接连接，该发送发送
        /// </summary>
        /// <param name="tunnelToken"></param>
        /// <returns></returns>
        private async Task SendToSocketUdp(AsyncUserTunnelToken tunnelToken)
        {

            if (tunnelToken.Proxy.Direction == ProxyDirection.Forward)
            {
                ConnectIdUdp connectId = tunnelToken.GetUdpConnectId();
                try
                {
                    IPEndPoint target = new IPEndPoint(tunnelToken.Proxy.TargetEP.Address, tunnelToken.Proxy.TargetEP.Port);
                    if (udpConnections.TryGetValue(connectId, out AsyncUserUdpTokenTarget token))
                    {
                        token.Connection = tunnelToken.Connection;
                        await token.TargetSocket.SendToAsync(tunnelToken.Proxy.Data, target).ConfigureAwait(false);
                        token.LastTicks.Update();
                        return;
                    }

                    _ = ConnectUdp(tunnelToken, target);

                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                    if (udpConnections.TryRemove(connectId, out AsyncUserUdpTokenTarget token))
                    {
                        CloseClientSocket(token);
                    }
                }
            }
            else
            {
                if (udpListens.TryGetValue(tunnelToken.Proxy.Port, out AsyncUserUdpToken asyncUserUdpToken))
                {
                    try
                    {
                        asyncUserUdpToken.Connection = tunnelToken.Connection;
                        await asyncUserUdpToken.SourceSocket.SendToAsync(tunnelToken.Proxy.Data, tunnelToken.Proxy.SourceEP).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        {
                            LoggerHelper.Instance.Error(ex);
                        }
                    }
                }
            }
        }
        private async Task ConnectUdp(AsyncUserTunnelToken tunnelToken, IPEndPoint target)
        {
            Socket socket = new Socket(target.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socket.WindowsUdpBug();
            await socket.SendToAsync(tunnelToken.Proxy.Data, target).ConfigureAwait(false);

            ConnectIdUdp connectId = tunnelToken.GetUdpConnectId();
            AsyncUserUdpTokenTarget udpToken = new AsyncUserUdpTokenTarget
            {
                Proxy = new ProxyInfo
                {
                    ConnectId = tunnelToken.Proxy.ConnectId,
                    Direction = ProxyDirection.Reverse,
                    Protocol = tunnelToken.Proxy.Protocol,
                    SourceEP = tunnelToken.Proxy.SourceEP,
                    TargetEP = tunnelToken.Proxy.TargetEP,
                    Step = ProxyStep.Forward,
                    Port = tunnelToken.Proxy.Port,
                    BufferSize = tunnelToken.Proxy.BufferSize,
                },
                TargetSocket = socket,
                ConnectId = connectId,
                Connection = tunnelToken.Connection,
                Buffer = new byte[(1 << tunnelToken.Proxy.BufferSize) * 1024]
            };
            udpToken.Proxy.Direction = ProxyDirection.Reverse;
            udpConnections.AddOrUpdate(connectId, udpToken, (a, b) => udpToken);

            try
            {
                while (true)
                {
                    SocketReceiveFromResult result = await socket.ReceiveFromAsync(udpToken.Buffer, SocketFlags.None, target).ConfigureAwait(false);
                    udpToken.Proxy.Data = udpToken.Buffer.AsMemory(0, result.ReceivedBytes);
                    udpToken.LastTicks.Update();
                    await SendToConnection(udpToken).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                if (udpConnections.TryRemove(connectId, out AsyncUserUdpTokenTarget token))
                {
                    CloseClientSocket(token);
                }
            }

        }


        /// <summary>
        /// b端接收到服务的数据，通过隧道发送给a
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task SendToConnection(AsyncUserUdpTokenTarget token)
        {
            byte[] connectData = token.Proxy.ToBytes(out int length);
            try
            {
                bool res = await token.Connection.SendAsync(connectData.AsMemory(0, length)).ConfigureAwait(false);
                if (res == false)
                {
                    CloseClientSocket(token);
                }
            }
            catch (Exception)
            {
                CloseClientSocket(token);
            }
            finally
            {
                token.Proxy.Return(connectData);
            }
        }

        /// <summary>
        /// 检查udp过期
        /// </summary>
        private void TaskUdp()
        {
            TimerHelper.SetInterval(() =>
            {
                if(udpConnections.Count > 0)
                {
                    var connections = udpConnections.Where(c => c.Value.Timeout).Select(c => c.Key).ToList();
                    foreach (var item in connections)
                    {
                        if (udpConnections.TryRemove(item, out AsyncUserUdpTokenTarget token))
                        {
                            try
                            {
                                token.Clear();
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
                
                return true;
            }, 30000);
        }

        private void CloseClientSocketUdp(ITunnelConnection connection)
        {
            int hashcode1 = connection.RemoteMachineId.GetHashCode();
            int hashcode2 = connection.TransactionId.GetHashCode();
            var tokens = udpConnections.Where(c => c.Key.hashcode1 == hashcode1 && c.Key.hashcode2 == hashcode2).ToList();
            foreach (var item in tokens)
            {
                try
                {
                    if (udpConnections.TryRemove(item.Key, out AsyncUserUdpTokenTarget token))
                    {
                        token.Clear();
                    }
                }
                catch (Exception)
                {
                }
            }
        }
        private void CloseClientSocket(AsyncUserUdpToken token)
        {
            if (token == null) return;
            token.Clear();
        }
        private void CloseClientSocket(AsyncUserUdpTokenTarget token)
        {
            if (token == null) return;
            if (udpConnections.TryRemove(token.ConnectId, out _))
            {
                token.Clear();
            }
            token.Clear();
        }

        private void StopUdp()
        {
            foreach (var item in udpListens)
            {
                item.Value.Clear();
            }
            udpListens.Clear();

            foreach (var item in udpConnections)
            {
                item.Value.Clear();
            }
            udpConnections.Clear();
        }
        private void StopUdp(int port)
        {
            if (udpListens.TryRemove(port, out AsyncUserUdpToken udpClient))
            {
                udpClient.Clear();
            }

            if (udpListens.Count == 0)
            {
                foreach (var item in udpConnections)
                {
                    item.Value.Clear();
                }
                udpConnections.Clear();
            }
        }

    }

    public sealed class AsyncUserUdpToken
    {
        public int ListenPort { get; set; }
        public Socket SourceSocket { get; set; }
        public ITunnelConnection Connection { get; set; }
        public List<ITunnelConnection> Connections { get; set; }
        public ProxyInfo Proxy { get; set; }

        public void Clear()
        {
            SourceSocket?.SafeClose();
            SourceSocket = null;
            GC.Collect();
            GC.SuppressFinalize(this);
        }
    }
    public sealed class AsyncUserUdpTokenTarget
    {
        public Socket TargetSocket { get; set; }

        public ITunnelConnection Connection { get; set; }
        public ProxyInfo Proxy { get; set; }

        public ConnectIdUdp ConnectId { get; set; }

        public byte[] Buffer { get; set; }

        public LastTicksManager LastTicks { get; set; } = new LastTicksManager();
        public bool Timeout => LastTicks.Expired(60 * 1000);
        public void Clear()
        {
            TargetSocket?.SafeClose();
            TargetSocket = null;
            GC.Collect();
            GC.SuppressFinalize(this);
        }
    }
    public sealed class ConnectIdUdpComparer : IEqualityComparer<ConnectIdUdp>
    {
        public bool Equals(ConnectIdUdp x, ConnectIdUdp y)
        {
            return x.source != null && x.source.Equals(y.source) && x.hashcode1 == y.hashcode1 && x.hashcode2 == y.hashcode2;
        }
        public int GetHashCode(ConnectIdUdp obj)
        {
            if (obj.source == null) return 0;
            return obj.source.GetHashCode() ^ obj.hashcode1 ^ obj.hashcode2;
        }
    }
    public readonly struct ConnectIdUdp
    {
        public readonly IPEndPoint source { get; }
        public int hashcode1 { get; }
        public int hashcode2 { get; }

        public ConnectIdUdp(IPEndPoint source, int hashcode1, int hashcode2)
        {
            this.source = source;
            this.hashcode1 = hashcode1;
            this.hashcode2 = hashcode2;
        }
    }
}
