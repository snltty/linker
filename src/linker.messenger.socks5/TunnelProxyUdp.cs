using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using linker.libs.timer;

namespace linker.messenger.socks5
{
    public partial class TunnelProxy
    {
        private ConcurrentDictionary<int, AsyncUserUdpToken> udpListens = new ConcurrentDictionary<int, AsyncUserUdpToken>();
        private ConcurrentDictionary<(IPAddress sip, ushort sport, string remoteId, string transId), AsyncUserUdpTokenTarget> udpConnections = new();

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
            byte[] bytes = new byte[65 * 1024];
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

        private async Task SendToConnection(AsyncUserUdpToken token)
        {
            if (token.Connection == null) return;

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

        private async Task SendToSocketUdp(AsyncUserTunnelToken tunnelToken)
        {

            if (tunnelToken.Proxy.Direction == ProxyDirection.Forward)
            {
                var connectId = tunnelToken.GetUdpConnectId();
                try
                {
                    if (udpConnections.TryGetValue(connectId, out AsyncUserUdpTokenTarget token))
                    {
                        token.Connection = tunnelToken.Connection;
                        await token.TargetSocket.SendToAsync(tunnelToken.Proxy.Data, token.TargetRealEP).ConfigureAwait(false);
                        token.Update();
                        return;
                    }

                    _ = ConnectUdp(tunnelToken);

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
                        if (await ConnectionReceiveUdp(tunnelToken, asyncUserUdpToken).ConfigureAwait(false) == false)
                        {
                            await asyncUserUdpToken.SourceSocket.SendToAsync(tunnelToken.Proxy.Data, tunnelToken.Proxy.SourceEP).ConfigureAwait(false);
                        }
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
        private async Task ConnectUdp(AsyncUserTunnelToken tunnelToken)
        {
            IPAddress ip = mapping.GetRealDst(tunnelToken.Proxy.TargetEP.Address);
            IPEndPoint target = new IPEndPoint(ip, tunnelToken.Proxy.TargetEP.Port);

            if (linkerFirewall.Check(tunnelToken.Connection.RemoteMachineId, target, ProtocolType.Udp) == false)
            {
                return;
            }


            Socket socket = new Socket(target.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socket.WindowsUdpBug();
            await socket.SendToAsync(tunnelToken.Proxy.Data, target).ConfigureAwait(false);

            var connectId = tunnelToken.GetUdpConnectId();
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
                TargetRealEP = target,
                ConnectId = connectId,
                Connection = tunnelToken.Connection,
                Buffer = new byte[65 * 1024]
            };
            udpToken.Proxy.Direction = ProxyDirection.Reverse;
            udpConnections.AddOrUpdate(connectId, udpToken, (a, b) => udpToken);

            try
            {
                while (true)
                {
                    SocketReceiveFromResult result = await socket.ReceiveFromAsync(udpToken.Buffer, SocketFlags.None, target).ConfigureAwait(false);
                    udpToken.Proxy.Data = udpToken.Buffer.AsMemory(0, result.ReceivedBytes);
                    udpToken.Update();
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

        private void TaskUdp()
        {
            TimerHelper.SetIntervalLong(() =>
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
            }, 3 * 60 * 1000);
        }

        private void CloseClientSocketUdp(ITunnelConnection connection)
        {
            var tokens = udpConnections.Where(c => c.Key.remoteId == connection.RemoteMachineId && c.Key.transId == connection.TransactionId).ToList();
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

        public void StopUdp()
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
        public void StopUdp(int port)
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
        public IPEndPoint TargetRealEP { get; set; }

        public ITunnelConnection Connection { get; set; }
        public ProxyInfo Proxy { get; set; }

        public (IPAddress sip, ushort sport, string remoteId, string transId) ConnectId { get; set; }

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
        public void Update()
        {
            LastTicks.Update();
        }
    }

}
