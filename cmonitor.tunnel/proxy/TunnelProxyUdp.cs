using cmonitor.tunnel.connection;
using common.libs;
using common.libs.extends;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace cmonitor.tunnel.proxy
{
    public partial class TunnelProxy
    {
        private ConcurrentDictionary<int, AsyncUserUdpToken> udpListens = new ConcurrentDictionary<int, AsyncUserUdpToken>();
        private ConcurrentDictionary<ConnectIdUdp, AsyncUserUdpTokenTarget> udpConnections = new(new ConnectIdUdpComparer());

        private void StartUdp(int port)
        {
            try
            {
                UdpClient udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
                AsyncUserUdpToken asyncUserUdpToken = new AsyncUserUdpToken
                {
                    ListenPort = port,
                    SourceSocket = udpClient,
                    Proxy = new ProxyInfo { Port = (ushort)port, Step = ProxyStep.Forward, ConnectId = 0, Protocol = ProxyProtocol.Udp, Direction = ProxyDirection.Forward }
                };

                udpClient.Client.EnableBroadcast = true;
                udpClient.Client.WindowsUdpBug();
                IAsyncResult result = udpClient.BeginReceive(ReceiveCallbackUdp, asyncUserUdpToken);
                udpListens.AddOrUpdate(port, asyncUserUdpToken, (a, b) => asyncUserUdpToken);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
        private async void ReceiveCallbackUdp(IAsyncResult result)
        {
            try
            {
                AsyncUserUdpToken token = result.AsyncState as AsyncUserUdpToken;

                byte[] bytes = token.SourceSocket.EndReceive(result, ref token.TempRemoteEP);

                token.Proxy.SourceEP = token.TempRemoteEP;
                token.Proxy.Data = bytes;
                await ConnectTunnelConnection(token);
                if (token.Proxy.TargetEP != null)
                {
                    if (token.Connection != null)
                    {
                        await SendToConnection(token).ConfigureAwait(false);
                    }
                    else if (token.Connections != null)
                    {
                        await SendToConnections(token).ConfigureAwait(false);
                    }
                }

                result = token.SourceSocket.BeginReceive(ReceiveCallbackUdp, token);
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
            }
        }
        private async Task SendToConnection(AsyncUserUdpToken token)
        {
            SemaphoreSlim semaphoreSlim = token.Proxy.Direction == ProxyDirection.Forward ? semaphoreSlimForward : semaphoreSlimReverse;
            await semaphoreSlim.WaitAsync();

            byte[] connectData = token.Proxy.ToBytes(out int length);
            try
            {
                if (token.Connection != null)
                {
                    bool res = await token.Connection.SendAsync(connectData.AsMemory(0, length)).ConfigureAwait(false);
                    if (res == false)
                    {
                        CloseClientSocket(token);
                    }
                }
                else if (token.Connections != null)
                {
                    foreach (var connection in token.Connections)
                    {
                        await connection.SendAsync(connectData.AsMemory(0, length)).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception)
            {
                CloseClientSocket(token);
            }
            finally
            {
                token.Proxy.Return(connectData);
                semaphoreSlim.Release();
            }
        }
        private async Task SendToConnections(AsyncUserUdpToken token)
        {
            SemaphoreSlim semaphoreSlim = token.Proxy.Direction == ProxyDirection.Forward ? semaphoreSlimForward : semaphoreSlimReverse;
            await semaphoreSlim.WaitAsync();

            byte[] connectData = token.Proxy.ToBytes(out int length);
            try
            {
                await Task.WhenAll(token.Connections.Select(c => c.SendAsync(connectData.AsMemory(0, length)).AsTask())).ConfigureAwait(false);
            }
            catch (Exception)
            {
                CloseClientSocket(token);
            }
            finally
            {
                token.Proxy.Return(connectData);
                semaphoreSlim.Release();
            }
        }
        /// <summary>
        /// 连接UDP
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        protected virtual async ValueTask ConnectTunnelConnection(AsyncUserUdpToken token)
        {
            await ValueTask.CompletedTask;
        }

        private async Task SendToSocketUdp(AsyncUserTunnelToken tunnelToken)
        {

            if (tunnelToken.Proxy.Direction == ProxyDirection.Forward)
            {
                ConnectIdUdp connectId = new ConnectIdUdp(tunnelToken.Proxy.ConnectId, tunnelToken.Proxy.SourceEP, tunnelToken.Connection.GetHashCode());
                try
                {
                    IPEndPoint target = new IPEndPoint(tunnelToken.Proxy.TargetEP.Address, tunnelToken.Proxy.TargetEP.Port);
                    if (target.Address.GetAddressBytes()[3] == 255)
                    {
                        target.Address = IPAddress.Loopback;
                    }

                    if (udpConnections.TryGetValue(connectId, out AsyncUserUdpTokenTarget token))
                    {
                        token.Connection = tunnelToken.Connection;
                        await token.TargetSocket.SendAsync(tunnelToken.Proxy.Data, target);
                        token.Update();
                        return;
                    }
                    _ = ConnectUdp(tunnelToken, target);

                }
                catch (Exception ex)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Error(ex);
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
                        if (await ConnectionReceiveUdp(tunnelToken, asyncUserUdpToken) == false)
                        {
                            await asyncUserUdpToken.SourceSocket.SendAsync(tunnelToken.Proxy.Data, tunnelToken.Proxy.SourceEP);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        {
                            Logger.Instance.Error(ex);
                        }
                    }
                }
            }
        }
        private async Task ConnectUdp(AsyncUserTunnelToken tunnelToken, IPEndPoint target)
        {
            ConnectIdUdp connectId = new ConnectIdUdp(tunnelToken.Proxy.ConnectId, tunnelToken.Proxy.SourceEP, tunnelToken.Connection.GetHashCode());

            UdpClient udpClient = new UdpClient();
            udpClient.Client.WindowsUdpBug();
            AsyncUserUdpTokenTarget udpToken = new AsyncUserUdpTokenTarget
            {
                Proxy = new ProxyInfo
                {
                    ConnectId = tunnelToken.Proxy.ConnectId,
                    Direction = ProxyDirection.Reverse,
                    Protocol = tunnelToken.Proxy.Protocol,
                    SourceEP = tunnelToken.Proxy.SourceEP,
                    TargetEP = tunnelToken.Proxy.TargetEP,
                    Step = tunnelToken.Proxy.Step,
                    Port = tunnelToken.Proxy.Port,
                },
                TargetSocket = udpClient,
                ConnectId = connectId,
                Connection = tunnelToken.Connection
            };
            udpToken.Proxy.Direction = ProxyDirection.Reverse;
            udpConnections.AddOrUpdate(connectId, udpToken, (a, b) => udpToken);

            await udpClient.SendAsync(tunnelToken.Proxy.Data, target);
            try
            {
                while (true)
                {
                    UdpReceiveResult result = await udpClient.ReceiveAsync();
                    udpToken.Proxy.Data = result.Buffer;
                    udpToken.Update();
                    await SendToConnection(udpToken);
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex.Message);
                }
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
        /// 连接对方返回UDP，是否要自己处理
        /// </summary>
        /// <param name="token"></param>
        /// <param name="asyncUserUdpToken"></param>
        /// <returns>true表示自己已经处理过了，不需要再处理了</returns>
        protected virtual async ValueTask<bool> ConnectionReceiveUdp(AsyncUserTunnelToken token, AsyncUserUdpToken asyncUserUdpToken)
        {
            return await ValueTask.FromResult(false);
        }

        private async Task SendToConnection(AsyncUserUdpTokenTarget token)
        {
            SemaphoreSlim semaphoreSlim = token.Proxy.Direction == ProxyDirection.Forward ? semaphoreSlimForward : semaphoreSlimReverse;
            await semaphoreSlim.WaitAsync();


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
                semaphoreSlim.Release();
            }
        }


        private void TaskUdp()
        {
            Task.Run(async () =>
            {
                while (true)
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

                    await Task.Delay(5000);
                }
            });
        }

        private void CloseClientSocketUdp(ITunnelConnection connection)
        {
            int hashcode = connection.GetHashCode();
            var tokens = udpConnections.Where(c => c.Key.hashcode == hashcode).ToList();
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
        public virtual void StopUdp(int port)
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
        public UdpClient SourceSocket { get; set; }
        public ITunnelConnection Connection { get; set; }
        public List<ITunnelConnection> Connections { get; set; }
        public ProxyInfo Proxy { get; set; }

        public IPEndPoint TempRemoteEP = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);

        public uint TargetIP { get; set; }

        public void Clear()
        {
            SourceSocket?.Close();
            SourceSocket?.Dispose();
            SourceSocket = null;

            //Connection = null;
            //Connections = null;

            //Proxy = null;
            //TempRemoteEP = null;

            GC.Collect();


        }
    }
    public sealed class AsyncUserUdpTokenTarget
    {
        public UdpClient TargetSocket { get; set; }

        public ITunnelConnection Connection { get; set; }
        public ProxyInfo Proxy { get; set; }

        public ConnectIdUdp ConnectId { get; set; }

        public long LastTime { get; set; } = Environment.TickCount64;
        public bool Timeout => Environment.TickCount64 - LastTime > 15000;
        public void Clear()
        {
            TargetSocket?.Close();
            TargetSocket?.Dispose();
            TargetSocket = null;
            GC.Collect();
            GC.SuppressFinalize(this);
        }
        public void Update()
        {
            LastTime = Environment.TickCount64;
        }
    }

    public sealed class ConnectIdUdpComparer : IEqualityComparer<ConnectIdUdp>
    {
        public bool Equals(ConnectIdUdp x, ConnectIdUdp y)
        {
            return x.source != null && x.source.Equals(y.source) && x.connectId == y.connectId && x.hashcode == y.hashcode;
        }
        public int GetHashCode(ConnectIdUdp obj)
        {
            if (obj.source == null) return 0;
            return obj.source.GetHashCode() ^ obj.connectId.GetHashCode() ^ obj.hashcode;
        }
    }
    public readonly struct ConnectIdUdp
    {
        public readonly IPEndPoint source { get; }
        public readonly ulong connectId { get; }
        public int hashcode { get; }

        public ConnectIdUdp(ulong connectId, IPEndPoint source, int hashcode)
        {
            this.connectId = connectId;
            this.source = source;
            this.hashcode = hashcode;
        }
    }
}
