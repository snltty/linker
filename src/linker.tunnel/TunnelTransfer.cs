using linker.tunnel.connection;
using linker.tunnel.transport;
using linker.libs;
using linker.libs.extends;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using linker.tunnel.wanport;
using linker.libs.timer;
using System.Buffers;

namespace linker.tunnel
{
    public sealed class TunnelTransfer
    {
        private readonly NetworkInfo networkInfo = new NetworkInfo();

        private readonly List<ITunnelTransport> transports;
        private readonly TunnelWanPortTransfer tunnelWanPortTransfer;
        private readonly TunnelUpnpTransfer tunnelUpnpTransfer;

        public ConcurrentDictionary<string, bool> Operating => operating.StringKeyValue;
        private readonly OperatingMultipleManager operating = new OperatingMultipleManager();
        private uint flowid = 1;
        private Dictionary<string, List<Action<ITunnelConnection>>> OnConnectedDic { get; } = new Dictionary<string, List<Action<ITunnelConnection>>>();


        private readonly ITunnelMessengerAdapter tunnelMessengerAdapter;
        public TunnelTransfer(ITunnelMessengerAdapter tunnelMessengerAdapter)
        {
            this.tunnelMessengerAdapter = tunnelMessengerAdapter;

            TransportUdpPortMap transportUdpPortMap = new TransportUdpPortMap(tunnelMessengerAdapter);
            TransportTcpPortMap transportTcpPortMap = new TransportTcpPortMap(tunnelMessengerAdapter);
            this.tunnelUpnpTransfer = new TunnelUpnpTransfer(transportUdpPortMap, transportTcpPortMap);
            tunnelWanPortTransfer = new TunnelWanPortTransfer();
            transports = new List<ITunnelTransport> {
                new TransportUdp(tunnelMessengerAdapter),
                new TransportUdpP2PNAT(tunnelMessengerAdapter),
                new TransportTcpP2PNAT(tunnelMessengerAdapter),
                new TransportTcpNutssb(tunnelMessengerAdapter),
                transportUdpPortMap,
                transportTcpPortMap,
                new TransportMsQuic(tunnelMessengerAdapter)
            };

            foreach (var item in transports)
            {
                item.OnConnected = OnConnected;
            }
            _ = RebuildTransports();
        }
        private async Task RebuildTransports()
        {
            var transportItems = (await tunnelMessengerAdapter.GetTunnelTransports("default").ConfigureAwait(false)).ToList();
            //有新的协议
            var newTransportNames = transports.Select(c => c.Name).Except(transportItems.Select(c => c.Name));
            if (newTransportNames.Any())
            {
                transportItems.AddRange(transports.Where(c => newTransportNames.Contains(c.Name)).Select(c => new TunnelTransportItemInfo
                {
                    Label = c.Label,
                    Name = c.Name,
                    ProtocolType = c.ProtocolType.ToString(),
                    Reverse = c.Reverse,
                    DisableReverse = c.DisableReverse,
                    SSL = c.SSL,
                    DisableSSL = c.DisableSSL,
                    Order = c.Order
                }));
            }
            //有已移除的协议
            var oldTransportNames = transportItems.Select(c => c.Name).Except(transports.Select(c => c.Name));
            if (oldTransportNames.Any())
            {
                foreach (var item in transportItems.Where(c => oldTransportNames.Contains(c.Name)))
                {
                    transportItems.Remove(item);
                }
            }
            //强制更新一些信息
            foreach (var item in transportItems)
            {
                var transport = transports.FirstOrDefault(c => c.Name == item.Name);
                if (transport != null)
                {
                    item.DisableReverse = transport.DisableReverse;
                    item.DisableSSL = transport.DisableSSL;
                    item.Name = transport.Name;
                    item.Label = transport.Label;
                    if (transport.DisableReverse)
                    {
                        item.Reverse = transport.Reverse;
                    }
                    if (transport.DisableSSL)
                    {
                        item.SSL = transport.SSL;
                    }
                    if (item.Order == 0)
                    {
                        item.Order = transport.Order;
                    }
                }
            }

            await tunnelMessengerAdapter.SetTunnelTransports("default",transportItems).ConfigureAwait(false);

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Info($"load tunnel transport:{string.Join(",", transports.Select(c => c.GetType().Name))}");
        }

        /// <summary>
        /// 刷新一下网络信息，比如路由级别，本机IP等
        /// </summary>
        public void Refresh()
        {
            RefreshNetwork();
        }
        private void RefreshNetwork()
        {
            TimerHelper.Async(() =>
            {
                if (tunnelMessengerAdapter.ServerHost == null) return;

                GetLocalIP(tunnelMessengerAdapter.ServerHost).ContinueWith((result) =>
                {
                    if (tunnelMessengerAdapter.PortMapPrivate > 0)
                    {
                        tunnelUpnpTransfer.SetMap(tunnelMessengerAdapter.PortMapPrivate, tunnelMessengerAdapter.PortMapPublic);
                    }
                    else
                    {
                        if (result.Result.Equals(IPAddress.Any) == false)
                        {
                            int ip = result.Result.GetAddressBytes()[3];
                            tunnelUpnpTransfer.SetMap(18180 + ip);
                        }
                    }
                });

                networkInfo.RouteLevel = NetworkHelper.GetRouteLevel(tunnelMessengerAdapter.ServerHost.ToString(), out List<IPAddress> ips);
                networkInfo.LocalIps = NetworkHelper.GetIPV6().Concat(NetworkHelper.GetIPV4()).ToArray();
                networkInfo.MachineId = tunnelMessengerAdapter.MachineId;

                async Task<IPAddress> GetLocalIP(IPEndPoint server)
                {
                    using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(16);
                    buffer.Memory.Span[0] = 255;

                    var socket = new Socket(server.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        await socket.ConnectAsync(server).ConfigureAwait(false);
                        await socket.SendAsync(buffer.Memory[..1]).ConfigureAwait(false);
                        return (socket.LocalEndPoint as IPEndPoint).Address;
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        socket.SafeClose();
                    }
                    return IPAddress.Any;
                }
            });
        }


        /// <summary>
        /// 设置成功打洞回调
        /// </summary>
        /// <param name="transactionId"></param>
        /// <param name="callback"></param>
        public void SetConnectedCallback(string transactionId, Action<ITunnelConnection> callback)
        {
            if (OnConnectedDic.TryGetValue(transactionId, out List<Action<ITunnelConnection>> callbacks) == false)
            {
                callbacks = new List<Action<ITunnelConnection>>();
                OnConnectedDic[transactionId] = callbacks;
            }
            callbacks.Add(callback);
        }
        /// <summary>
        /// 移除打洞成功回调
        /// </summary>
        /// <param name="transactionId"></param>
        /// <param name="callback"></param>
        public void RemoveConnectedCallback(string transactionId, Action<ITunnelConnection> callback)
        {
            if (OnConnectedDic.TryGetValue(transactionId, out List<Action<ITunnelConnection>> callbacks))
            {
                callbacks.Remove(callback);
            }
        }


        /// <summary>
        /// 开始连接对方
        /// </summary>
        /// <param name="remoteMachineId">对方id</param>
        /// <param name="transactionId">事务id，随便起，你喜欢就好</param>
        /// <param name="denyProtocols">本次连接排除那些打洞协议</param>
        /// <returns></returns>
        public async Task<ITunnelConnection> ConnectAsync(string remoteMachineId, string transactionId, TunnelProtocolType denyProtocols)
        {
            return await ConnectAsync(remoteMachineId, transactionId, transactionId, denyProtocols).ConfigureAwait(false);
        }
        /// <summary>
        /// 开始连接对方
        /// </summary>
        /// <param name="remoteMachineId">对方id</param>
        /// <param name="transactionId">事务id，随便起，你喜欢就好</param>
        /// <param name="transactionTag">事务tag，随便起，你喜欢就好</param>
        /// <param name="denyProtocols">本次连接排除那些打洞协议</param>
        /// <returns></returns>
        public async Task<ITunnelConnection> ConnectAsync(string remoteMachineId, string transactionId, string transactionTag, TunnelProtocolType denyProtocols)
        {
            if (operating.StartOperation(BuildKey(remoteMachineId, transactionId)) == false) return null;

            try
            {
                var _transports = await tunnelMessengerAdapter.GetTunnelTransports(remoteMachineId).ConfigureAwait(false);

                foreach (TunnelTransportItemInfo transportItem in _transports.OrderBy(c => c.Order).Where(c => c.Disabled == false))
                {
                    ITunnelTransport transport = transports.FirstOrDefault(c => c.Name == transportItem.Name);

                    //找不到这个打洞协议，或者是不支持的协议
                    if (transport == null || (transport.ProtocolType & denyProtocols) == transport.ProtocolType)
                    {
                        continue;
                    }
                    transport.SetSSL(tunnelMessengerAdapter.Certificate);

                    foreach (var wanPortProtocol in tunnelWanPortTransfer.Protocols)
                    {

                        //这个打洞协议不支持这个外网端口协议
                        if ((transport.AllowWanPortProtocolType & wanPortProtocol) != wanPortProtocol)
                        {
                            continue;
                        }

                        TunnelTransportInfo tunnelTransportInfo = null;
                        //是否在失败后尝试反向连接
                        int times = transportItem.Reverse ? 1 : 0;
                        for (int i = 0; i <= times; i++)
                        {
                            try
                            {
                                //获取自己的外网ip
                                Task<TunnelTransportWanPortInfo> localInfo = GetLocalInfo(wanPortProtocol);
                                //获取对方的外网ip
                                Task<TunnelTransportWanPortInfo> remoteInfo = tunnelMessengerAdapter.GetRemoteWanPort(new TunnelWanPortProtocolInfo
                                {
                                    MachineId = remoteMachineId,
                                    ProtocolType = wanPortProtocol
                                });
                                await Task.WhenAll(localInfo, remoteInfo).ConfigureAwait(false);

                                if (localInfo.Result == null)
                                {
                                    LoggerHelper.Instance.Error($"tunnel {transport.Name} get local external ip fail ");
                                    break;
                                }

                                if (remoteInfo.Result == null)
                                {
                                    LoggerHelper.Instance.Error($"tunnel {transport.Name} get remote {remoteMachineId} external ip fail ");
                                    break;
                                }
                                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                    LoggerHelper.Instance.Info($"tunnel {transport.Name} got local external ip {localInfo.Result.ToJson()}");
                                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                    LoggerHelper.Instance.Info($"tunnel {transport.Name} got remote external ip {remoteInfo.Result.ToJson()}");
                                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                    LoggerHelper.Instance.Info($"tunnel {transportItem.ToJson()}");


                                tunnelTransportInfo = new TunnelTransportInfo
                                {
                                    Direction = (TunnelDirection)i,
                                    TransactionId = transactionId,
                                    TransactionTag = transactionTag,
                                    TransportName = transport.Name,
                                    TransportType = transport.ProtocolType,
                                    Local = localInfo.Result,
                                    Remote = remoteInfo.Result,
                                    SSL = transportItem.SSL,
                                    FlowId = Interlocked.Increment(ref flowid),
                                };
                                OnConnecting(tunnelTransportInfo);
                                ParseRemoteEndPoint(tunnelTransportInfo);
                                ITunnelConnection connection = await transport.ConnectAsync(tunnelTransportInfo).ConfigureAwait(false);
                                if (connection != null)
                                {
                                    OnConnected(connection);
                                    return connection;
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
                        if (tunnelTransportInfo != null)
                        {
                            OnConnectFail(tunnelTransportInfo);
                        }
                    }
                }
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
                operating.StopOperation(BuildKey(remoteMachineId, transactionId));
            }
            return null;
        }
        /// <summary>
        /// 收到对方开始连接的消息
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        public async Task OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {

            if (operating.StartOperation(BuildKey(tunnelTransportInfo.Remote.MachineId, tunnelTransportInfo.TransactionId)) == false)
            {
                return;
            }
            try
            {
                var _transports = await tunnelMessengerAdapter.GetTunnelTransports(tunnelTransportInfo.Remote.MachineId).ConfigureAwait(false);

                ITunnelTransport transport = transports.FirstOrDefault(c => c.Name == tunnelTransportInfo.TransportName && c.ProtocolType == tunnelTransportInfo.TransportType);
                TunnelTransportItemInfo item = _transports.FirstOrDefault(c => c.Name == tunnelTransportInfo.TransportName && c.Disabled == false);
                if (transport != null && item != null)
                {
                    transport.SetSSL(tunnelMessengerAdapter.Certificate);
                    OnConnectBegin(tunnelTransportInfo);
                    ParseRemoteEndPoint(tunnelTransportInfo);
                    _ = transport.OnBegin(tunnelTransportInfo).ContinueWith((result) =>
                    {
                        operating.StopOperation(BuildKey(tunnelTransportInfo.Remote.MachineId, tunnelTransportInfo.TransactionId));
                    });
                }
                else
                {
                    operating.StopOperation(BuildKey(tunnelTransportInfo.Remote.MachineId, tunnelTransportInfo.TransactionId));
                    _ = tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo);
                }
            }
            catch (Exception ex)
            {
                operating.StopOperation(BuildKey(tunnelTransportInfo.Remote.MachineId, tunnelTransportInfo.TransactionId));
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }


        }
        /// <summary>
        /// 收到对方发来的连接失败的消息
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        public void OnFail(TunnelTransportInfo tunnelTransportInfo)
        {
            ITunnelTransport _transports = transports.FirstOrDefault(c => c.Name == tunnelTransportInfo.TransportName && c.ProtocolType == tunnelTransportInfo.TransportType);
            _transports?.OnFail(tunnelTransportInfo);
        }
        /// <summary>
        /// 收到对方发来的连接成功的消息
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        public void OnSuccess(TunnelTransportInfo tunnelTransportInfo)
        {
            ITunnelTransport _transports = transports.FirstOrDefault(c => c.Name == tunnelTransportInfo.TransportName && c.ProtocolType == tunnelTransportInfo.TransportType);
            _transports?.OnSuccess(tunnelTransportInfo);
        }

        /// <summary>
        /// 获取自己的外网IP，给别人调用
        /// </summary>
        /// <returns></returns>
        public async Task<TunnelTransportWanPortInfo> GetWanPort(TunnelWanPortProtocolInfo _info)
        {
            return await GetLocalInfo(_info.ProtocolType).ConfigureAwait(false);
        }
        /// <summary>
        /// 获取自己的外网IP
        /// </summary>
        /// <returns></returns>
        private async Task<TunnelTransportWanPortInfo> GetLocalInfo(TunnelWanPortProtocolType tunnelWanPortProtocolType)
        {
            if (tunnelMessengerAdapter.ServerHost == null || string.IsNullOrWhiteSpace(tunnelMessengerAdapter.MachineId))
            {
                LoggerHelper.Instance.Error($"wan port ServerHost is null or MachineId is null");
                return null;
            }

            TunnelWanPortEndPoint ip = await tunnelWanPortTransfer.GetWanPortAsync(tunnelMessengerAdapter.ServerHost, tunnelWanPortProtocolType).ConfigureAwait(false);
            if (ip == null)
            {
                LoggerHelper.Instance.Error($"wan port get ip is null");
                return null;
            }
            MapInfo portMapInfo = tunnelUpnpTransfer.PortMap ?? new MapInfo { PrivatePort = 0, PublicPort = 0 };
            return new TunnelTransportWanPortInfo
            {
                Local = ip.Local,
                Remote = ip.Remote,
                LocalIps = networkInfo.LocalIps,
                RouteLevel = networkInfo.RouteLevel + tunnelMessengerAdapter.RouteLevelPlus,
                MachineId = tunnelMessengerAdapter.MachineId,
                PortMapLan = portMapInfo.PrivatePort,
                PortMapWan = portMapInfo.PublicPort,
            };
        }


        private static void OnConnecting(TunnelTransportInfo tunnelTransportInfo)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Info($"tunnel connecting {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName}");
        }
        private static void OnConnectBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Info($"tunnel connecting from {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName}");
        }
        /// <summary>
        /// 连接成功
        /// </summary>
        /// <param name="connection"></param>
        private void OnConnected(ITunnelConnection connection)
        {
            if (connection == null) return;
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Debug($"tunnel connect {connection.RemoteMachineId}->{connection.RemoteMachineName} success->{connection.IPEndPoint}");

            //调用以下别人注册的回调
            if (OnConnectedDic.TryGetValue(Helper.GlobalString, out List<Action<ITunnelConnection>> callbacks))
            {
                foreach (var item in callbacks)
                {
                    item(connection);
                }
            }
            if (OnConnectedDic.TryGetValue(connection.TransactionId, out callbacks))
            {
                foreach (var item in callbacks)
                {
                    item(connection);
                }
            }
        }
        private static void OnConnectFail(TunnelTransportInfo tunnelTransportInfo)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Error($"tunnel connect {tunnelTransportInfo.Remote.MachineId} fail");
        }

        private void ParseRemoteEndPoint(TunnelTransportInfo tunnelTransportInfo)
        {
            //要连接哪些IP
            List<IPEndPoint> eps = new List<IPEndPoint>();
            var excludeips = tunnelMessengerAdapter.GetExcludeIps();

            //先尝试内网ipv4
            if (tunnelTransportInfo.Local.Remote.Address.Equals(tunnelTransportInfo.Remote.Remote.Address))
            {
                eps.AddRange(tunnelTransportInfo.Remote.LocalIps.Where(c => c.AddressFamily == AddressFamily.InterNetwork).SelectMany(c => new List<IPEndPoint>
                {
                    new IPEndPoint(c, tunnelTransportInfo.Remote.Local.Port),
                    new IPEndPoint(c, tunnelTransportInfo.Remote.Remote.Port),
                    new IPEndPoint(c, tunnelTransportInfo.Remote.Remote.Port + 1)
                }));
            }
            //在尝试外网
            eps.AddRange(new List<IPEndPoint>{
                new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address,tunnelTransportInfo.Remote.Remote.Port),
                new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address,tunnelTransportInfo.Remote.Remote.Port+1)
            });
            //再尝试IPV6
            eps.AddRange(tunnelTransportInfo.Remote.LocalIps.Where(c => c.AddressFamily == AddressFamily.InterNetworkV6).SelectMany(c => new List<IPEndPoint>
            {
                new IPEndPoint(c, tunnelTransportInfo.Remote.Local.Port),
                new IPEndPoint(c, tunnelTransportInfo.Remote.Remote.Port),
                new IPEndPoint(c, tunnelTransportInfo.Remote.Remote.Port + 1)
             }));
            //本机有V6
            bool hasV6 = tunnelTransportInfo.Local.LocalIps.Any(c => c.AddressFamily == AddressFamily.InterNetworkV6);
            //本机的局域网ip和外网ip
            List<IPAddress> localLocalIps = tunnelTransportInfo.Local.LocalIps.Concat(new List<IPAddress> { tunnelTransportInfo.Local.Remote.Address }).ToList();
            eps = eps
                .Where(c =>
                {
                    if (c.AddressFamily == AddressFamily.InterNetworkV6) return true;
                    return excludeips.Any(d => NetworkHelper.ToNetworkValue(d.IP, d.PrefixLength) == NetworkHelper.ToNetworkValue(c.Address, d.PrefixLength)) == false;
                })
                //对方是V6，本机也得有V6
                .Where(c => (c.AddressFamily == AddressFamily.InterNetworkV6 && hasV6) || c.AddressFamily == AddressFamily.InterNetwork)
                //端口和本机端口一样，那不应该是换回地址
                .Where(c => (c.Port == tunnelTransportInfo.Local.Local.Port && c.Address.Equals(IPAddress.Loopback)) == false)
                //端口和本机端口一样。那不应该是本机的IP
                .Where(c => (c.Port == tunnelTransportInfo.Local.Local.Port && localLocalIps.Any(d => d.Equals(c.Address))) == false)
                .Where(c => c.Address.Equals(IPAddress.Any) == false && c.Port > 0)
                .ToList();

            tunnelTransportInfo.RemoteEndPoints = eps;
        }
        private static string BuildKey(string remoteMachineId, string transactionId)
        {
            return $"{remoteMachineId}@{transactionId}";
        }

        private ConcurrentDictionary<string, bool> backgroundDic = new ConcurrentDictionary<string, bool>();
        /// <summary>
        /// 开始后台打洞
        /// </summary>
        /// <param name="remoteMachineId"></param>
        /// <param name="transactionId"></param>
        public void StartBackground(string remoteMachineId, string transactionId, TunnelProtocolType denyProtocols, Func<bool> stopCallback, Func<ITunnelConnection, Task> resultCallback, int times = 10, int delay = 10000)
        {
            if (AddBackground(remoteMachineId, transactionId) == false)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error($"tunnel background {remoteMachineId}@{transactionId} already exists");
                return;
            }
            TimerHelper.Async(async () =>
            {
                try
                {
                    ITunnelConnection connection = null;

                    await Task.Delay(delay).ConfigureAwait(false);
                    for (int i = 1; i <= times; i++)
                    {
                        if (stopCallback()) break;

                        connection = await ConnectAsync(remoteMachineId, transactionId, denyProtocols).ConfigureAwait(false);
                        if (connection != null)
                        {
                            break;
                        }
                        await Task.Delay(i * 3000).ConfigureAwait(false);
                    }

                    await resultCallback(connection).ConfigureAwait(false);
                }
                catch (Exception)
                {
                }
                finally
                {
                    RemoveBackground(remoteMachineId, transactionId);
                }
            });
        }
        private bool AddBackground(string remoteMachineId, string transactionId)
        {
            return backgroundDic.TryAdd(GetBackgroundKey(remoteMachineId, transactionId), true);
        }
        private void RemoveBackground(string remoteMachineId, string transactionId)
        {
            backgroundDic.TryRemove(GetBackgroundKey(remoteMachineId, transactionId), out _);
        }
        /// <summary>
        /// 是否正在后台打洞
        /// </summary>
        /// <param name="remoteMachineId"></param>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        public bool IsBackground(string remoteMachineId, string transactionId)
        {
            return backgroundDic.ContainsKey(GetBackgroundKey(remoteMachineId, transactionId));
        }
        private static string GetBackgroundKey(string remoteMachineId, string transactionId)
        {
            return $"{remoteMachineId}@{transactionId}";
        }
    }
}
