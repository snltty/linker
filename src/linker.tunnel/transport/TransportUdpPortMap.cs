using linker.tunnel.connection;
using linker.tunnel.wanport;
using System.Net.Sockets;
using System.Net;
using System.Text;
using linker.libs.extends;
using System.Collections.Concurrent;
using linker.libs;
using System.Security.Cryptography.X509Certificates;
using linker.libs.timer;
using System.Buffers;

namespace linker.tunnel.transport
{
    /// <summary>
    /// 基于端口映射
    /// 这个没什么说的，就是设置了固定端口，就监听，对方来连这个固定的端口即可
    /// </summary>
    public sealed class TransportUdpPortMap : ITunnelTransport
    {
        public string Name => "UdpPortMap";

        public string Label => "UDP、端口映射";

        public TunnelProtocolType ProtocolType => TunnelProtocolType.Udp;

        public TunnelWanPortProtocolType AllowWanPortProtocolType => TunnelWanPortProtocolType.Tcp | TunnelWanPortProtocolType.Udp;
        public TunnelType TunnelType => TunnelType.P2P;

        public bool Reverse => true;

        public bool DisableReverse => false;

        public bool SSL => true;

        public bool DisableSSL => false;

        public byte Order => 1;

        public Action<ITunnelConnection> OnConnected { get; set; } = (state) => { };


        private const string flagTexts = $"{Helper.GlobalString}.udp.portmap.tunnel";
        private byte[] flagBytes = Encoding.UTF8.GetBytes(flagTexts);


        private readonly ConcurrentDictionary<string, TaskCompletionSource<State>> distDic = new ConcurrentDictionary<string, TaskCompletionSource<State>>();
        private readonly ConcurrentDictionary<IPEndPoint, ConnectionCacheInfo> connectionsDic = new ConcurrentDictionary<IPEndPoint, ConnectionCacheInfo>(new IPEndPointComparer());

        private readonly ITunnelMessengerAdapter tunnelMessengerAdapter;
        public TransportUdpPortMap(ITunnelMessengerAdapter tunnelMessengerAdapter)
        {
            this.tunnelMessengerAdapter = tunnelMessengerAdapter;
            CleanTask();
        }
        private X509Certificate certificate;
        public void SetSSL(X509Certificate certificate)
        {
            this.certificate = certificate;
        }

        Socket socket;
        public async Task Listen(int localPort)
        {
            if (socket != null && (socket.LocalEndPoint as IPEndPoint).Port == localPort)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Warning($"{Name} {socket.LocalEndPoint} already exists");
                }
                return;
            }

            byte[] buffer = ArrayPool<byte>.Shared.Rent(8 * 1024);
            try
            {
                socket?.SafeClose();
                if (localPort == 0) return;

                IPAddress localIP = IPAddress.IPv6Any;

                socket = new Socket(localIP.AddressFamily, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
                socket.WindowsUdpBug();
                socket.IPv6Only(localIP.AddressFamily, false);
                socket.Bind(new IPEndPoint(localIP, localPort));

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Debug($"{Name} listen {localPort}");
                }

                IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                while (true)
                {
                    try
                    {
                        SocketReceiveFromResult result = await socket.ReceiveFromAsync(buffer.AsMemory(), ep).ConfigureAwait(false);
                        if (result.ReceivedBytes == 0)
                        {
                            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                LoggerHelper.Instance.Debug($"{Name} recv from {result.RemoteEndPoint} <0>");
                            await Task.Delay(1000);
                            continue;
                        }

                        IPEndPoint remoteEP = result.RemoteEndPoint as IPEndPoint;
                        Memory<byte> memory = buffer.AsMemory(0, result.ReceivedBytes);


                        if (memory.Length > flagBytes.Length && memory.Span.Slice(0, flagBytes.Length).SequenceEqual(flagBytes))
                        {
                            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                LoggerHelper.Instance.Debug($"{Name} recv from {result.RemoteEndPoint} <{memory.GetString()}>");

                            if (connectionsDic.TryGetValue(remoteEP, out ConnectionCacheInfo cache))
                            {
                                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                    LoggerHelper.Instance.Warning($"{Name} recv from {result.RemoteEndPoint} <{memory.GetString()}> connected");
                            }
                            else
                            {
                                string key = memory.GetString();
                                if (distDic.TryRemove(key, out TaskCompletionSource<State> tcs))
                                {
                                    connectionsDic.TryAdd(remoteEP, new ConnectionCacheInfo { });
                                    await socket.SendToAsync(memory, result.RemoteEndPoint).ConfigureAwait(false);
                                    tcs.TrySetResult(new State { Socket = socket, RemoteEndPoint = remoteEP });
                                }
                            }
                        }
                        else if (connectionsDic.TryGetValue(remoteEP, out ConnectionCacheInfo cache))
                        {
                            bool success = await cache.Connection.ProcessWrite(buffer, 0, result.ReceivedBytes).ConfigureAwait(false);
                            if (success == false)
                            {
                                connectionsDic.TryRemove(remoteEP, out _);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        socket.SafeClose();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                socket.SafeClose();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public async Task<ITunnelConnection> ConnectAsync(TunnelTransportInfo tunnelTransportInfo)
        {
            if (tunnelTransportInfo.Direction == TunnelDirection.Forward)
            {
                if (tunnelTransportInfo.Remote.PortMapWan == 0)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"ConnectAsync Forward【{Name}】{tunnelTransportInfo.Remote.MachineName} port mapping not configured");
                    return null;
                }
                //正向连接
                if (await tunnelMessengerAdapter.SendConnectBegin(tunnelTransportInfo).ConfigureAwait(false) == false)
                {
                    return null;
                }
                await Task.Delay(100).ConfigureAwait(false);
                ITunnelConnection connection = await ConnectForward(tunnelTransportInfo).ConfigureAwait(false);
                if (connection != null)
                {
                    await tunnelMessengerAdapter.SendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
                    return connection;
                }
            }
            else if (tunnelTransportInfo.Direction == TunnelDirection.Reverse)
            {
                if (tunnelTransportInfo.Local.PortMapWan == 0)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"ConnectAsync Reverse【{Name}】{tunnelTransportInfo.Local.MachineName} port mapping not configured");
                    return null;
                }
                //反向连接
                TunnelTransportInfo tunnelTransportInfo1 = tunnelTransportInfo.ToJsonFormat().DeJson<TunnelTransportInfo>();
                //等待对方连接，如果连接成功，我会收到一个socket，并且创建一个连接对象，失败的话会超时，那就是null
                var task = WaitConnect(tunnelTransportInfo1);
                if (await tunnelMessengerAdapter.SendConnectBegin(tunnelTransportInfo1).ConfigureAwait(false) == false)
                {
                    return null;
                }
                ITunnelConnection connection = await task.ConfigureAwait(false);
                if (connection != null)
                {
                    await tunnelMessengerAdapter.SendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
                    return connection;
                }
            }


            await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
            return null;
        }
        public async Task OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            if (tunnelTransportInfo.SSL && certificate == null)
            {
                LoggerHelper.Instance.Error($"{Name}->ssl Certificate not found");
                await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
                return;
            }
            //正向连接，等他来连
            if (tunnelTransportInfo.Direction == TunnelDirection.Forward)
            {
                if (tunnelTransportInfo.Local.PortMapWan == 0)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"OnBegin WaitConnect 【{Name}】{tunnelTransportInfo.Local.MachineName} port mapping not configured");
                    await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
                    return;
                }
                _ = WaitConnect(tunnelTransportInfo).ContinueWith((result) =>
                {
                    OnConnected(result.Result);
                });
            }
            //我要连它，那就连接
            else
            {
                if (tunnelTransportInfo.Remote.PortMapWan == 0)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"OnBegin ConnectForward 【{Name}】{tunnelTransportInfo.Remote.MachineName} port mapping not configured");
                    await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
                    return;
                }

                ITunnelConnection connection = await ConnectForward(tunnelTransportInfo).ConfigureAwait(false);
                if (connection != null)
                {
                    OnConnected(connection);
                    await tunnelMessengerAdapter.SendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
                }
                else
                {
                    await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
                }
            }
        }

        public void OnFail(TunnelTransportInfo tunnelTransportInfo)
        {
        }
        public void OnSuccess(TunnelTransportInfo tunnelTransportInfo)
        {
        }

        private async Task<ITunnelConnection> WaitConnect(TunnelTransportInfo tunnelTransportInfo)
        {
            TaskCompletionSource<State> tcs = new TaskCompletionSource<State>(TaskCreationOptions.RunContinuationsAsynchronously);
            string key = $"{flagTexts}-{tunnelTransportInfo.Remote.MachineId}-{tunnelTransportInfo.FlowId}";
            distDic.TryAdd(key, tcs);
            try
            {
                State state = await tcs.WithTimeout(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);

                TunnelConnectionUdp result = new TunnelConnectionUdp
                {
                    RemoteMachineId = tunnelTransportInfo.Remote.MachineId,
                    RemoteMachineName = tunnelTransportInfo.Remote.MachineName,
                    Direction = tunnelTransportInfo.Direction,
                    ProtocolType = TunnelProtocolType.Udp,
                    Type = TunnelType,
                    Mode = TunnelMode.Server,
                    TransactionId = tunnelTransportInfo.TransactionId,
                    TransactionTag = tunnelTransportInfo.TransactionTag,
                    TransportName = tunnelTransportInfo.TransportName,
                    IPEndPoint = NetworkHelper.TransEndpointFamily(state.RemoteEndPoint),
                    Label = string.Empty,
                    BufferSize = tunnelTransportInfo.BufferSize,
                    Receive = false,
                    UdpClient = state.Socket,
                    SSL = tunnelTransportInfo.SSL,
                    Crypto = CryptoFactory.CreateSymmetric(tunnelTransportInfo.Local.MachineId)
                };
                if (connectionsDic.TryGetValue(state.RemoteEndPoint, out ConnectionCacheInfo cache))
                {
                    cache.Connection = result;
                    return result;
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                distDic.TryRemove(key, out _);
            }
            return null;
        }
        private async Task<ITunnelConnection> ConnectForward(TunnelTransportInfo tunnelTransportInfo)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                LoggerHelper.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {string.Join("\r\n", tunnelTransportInfo.RemoteEndPoints.Select(c => c.ToString()))}");
            }

            List<IPEndPoint> eps = tunnelTransportInfo.RemoteEndPoints.Select(c => c.Address).Distinct().Select(c => new IPEndPoint(c, tunnelTransportInfo.Remote.PortMapWan)).ToList();

            using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent( 1024);
            foreach (var ep in eps)
            {
                Socket targetSocket = new(ep.AddressFamily, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
                using CancellationTokenSource cts = new CancellationTokenSource(500);
                try
                {
                    targetSocket.WindowsUdpBug();
                    targetSocket.IPv6Only(ep.AddressFamily, false);
                    targetSocket.ReuseBind(new IPEndPoint(ep.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, tunnelTransportInfo.Local.Local.Port));

                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {ep}");
                    }

                    byte[] sendt = $"{flagTexts}-{tunnelTransportInfo.Local.MachineId}-{tunnelTransportInfo.FlowId}".ToBytes();
                    await targetSocket.SendToAsync(sendt, ep).ConfigureAwait(false);

                    SocketReceiveFromResult recvRestlt = await targetSocket.ReceiveFromAsync(buffer.Memory, new IPEndPoint(ep.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0),cts.Token).ConfigureAwait(false);

                    if (buffer.Memory.Span.Slice(0, recvRestlt.ReceivedBytes).SequenceEqual(sendt) == false)
                    {
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        {
                            LoggerHelper.Instance.Error($"{Name} connect to {ep}, recv <{buffer.Memory.Span.Slice(0, recvRestlt.ReceivedBytes).GetString()}> tunnel fail");
                        }
                        continue;
                    }

                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Debug($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {ep} tunnel success");

                    TunnelConnectionUdp result = new TunnelConnectionUdp
                    {
                        IPEndPoint = NetworkHelper.TransEndpointFamily(ep),
                        TransactionId = tunnelTransportInfo.TransactionId,
                        TransactionTag = tunnelTransportInfo.TransactionTag,
                        RemoteMachineId = tunnelTransportInfo.Remote.MachineId,
                        RemoteMachineName = tunnelTransportInfo.Remote.MachineName,
                        TransportName = Name,
                        Direction = tunnelTransportInfo.Direction,
                        ProtocolType = TunnelProtocolType.Udp,
                        Type = TunnelType,
                        Mode = TunnelMode.Client,
                        Label = string.Empty,
                        BufferSize = tunnelTransportInfo.BufferSize,
                        Receive = true,
                        UdpClient = targetSocket,
                        SSL = tunnelTransportInfo.SSL,
                        Crypto = CryptoFactory.CreateSymmetric(tunnelTransportInfo.Remote.MachineId)
                    };
                    ConnectionCacheInfo cache = new ConnectionCacheInfo { Connection = result };
                    connectionsDic.AddOrUpdate(ep, cache, (a, b) => cache);
                    return result;
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error($"{Name} connect {ep} fail {ex}");
                    }
                }
                targetSocket.SafeClose();
            }
            return null;
        }


        private void CleanTask()
        {
            TimerHelper.SetIntervalLong(() =>
            {
                var keys = connectionsDic.Where(c => (c.Value.Connection == null && c.Value.LastTicks.DiffGreater(15000)) || (c.Value.Connection != null && c.Value.Connection.Connected == false)).Select(c => c.Key).ToList();
                foreach (var item in keys)
                {
                    connectionsDic.TryRemove(item, out _);
                }
            }, 30000);
        }
    }

    public sealed class State
    {
        public IPEndPoint RemoteEndPoint { get; set; }
        public Socket Socket { get; set; }
    }

    public sealed class ConnectionCacheInfo
    {
        public LastTicksManager LastTicks { get; set; } = new LastTicksManager();
        public TunnelConnectionUdp Connection { get; set; }
    }

    public sealed class IPEndPointComparer : IEqualityComparer<IPEndPoint>
    {
        public bool Equals(IPEndPoint x, IPEndPoint y)
        {
            return x.Equals(y);
        }
        public int GetHashCode(IPEndPoint obj)
        {
            if (obj == null) return 0;
            return obj.GetHashCode();
        }
    }
}