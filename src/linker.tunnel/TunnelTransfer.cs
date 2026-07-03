using linker.libs;
using linker.libs.extends;
using linker.libs.timer;
using linker.tunnel.connection;
using linker.tunnel.transport;
using linker.tunnel.wanport;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace linker.tunnel
{
    public sealed class TunnelTransfer
    {
        private readonly NetworkInfo networkInfo = new NetworkInfo();

        private readonly List<ITunnelTransport> transports;
        private readonly TunnelWanPortTransfer tunnelWanPortTransfer;
        private readonly TunnelUpnpTransfer tunnelUpnpTransfer;
        private readonly RadarTransfer radarTransfer = new RadarTransfer();

        public VersionManager OperatingVersion => operating.DataVersion;
        public ConcurrentDictionary<string, bool> Operating => operating.StringKeyValue;
        private readonly OperatingMultipleManager operating = new OperatingMultipleManager();
        private uint flowid = 1;
        private Dictionary<string, List<Action<ITunnelConnection, TunnelTransportInfo>>> OnConnectedDic { get; } = new();


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
                transportTcpPortMap
            };

            foreach (var item in transports)
            {
                item.OnConnected = OnConnected;
            }

            _ = RefreshRadar();
        }
        private async Task RefreshRadar()
        {
            var samples = await tunnelMessengerAdapter.LoadRadarSamples().ConfigureAwait(false);
            radarTransfer.ImportSamples(samples);
            radarTransfer.SamplesReceived += async (s, e) => { await tunnelMessengerAdapter.SaveRadarSamples(e.ToList()).ConfigureAwait(false); };
            radarTransfer.StartProbe();
        }

        public void RebuildTransports()
        {
            _ = tunnelMessengerAdapter.SetTunnelTransports(Helper.GlobalString, transports).ConfigureAwait(false);
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Info($"load tunnel transport:{string.Join(",", transports.Select(c => c.GetType().Name))}");
        }
        public void AddTransport(ITunnelTransport transport)
        {
            if (transports.Any(c => c.Name == transport.Name) == false)
            {
                transport.OnConnected = OnConnected;
                transports.Add(transport);
            }
        }
        public void AddProtocol(ITunnelWanPortProtocol protocol)
        {
            tunnelWanPortTransfer.AddProtocol(protocol);
        }

        public void Refresh()
        {
            RefreshNetwork();
        }
        private void RefreshNetwork()
        {
            TimerHelper.Async(async () =>
            {
                if (tunnelMessengerAdapter.ServerHost == null) return;

                if (tunnelMessengerAdapter.PortMapPrivate > 0)
                {
                    tunnelUpnpTransfer.SetMap(tunnelMessengerAdapter.PortMapPrivate, tunnelMessengerAdapter.PortMapPublic);
                }
                else
                {
                    IPAddress ip = await GetLocalIP(tunnelMessengerAdapter.ServerHost).ConfigureAwait(false);
                    if (IPAddress.Any.Equals(ip) == false)
                    {
                        tunnelUpnpTransfer.SetMap(18180 + ip.GetAddressBytes()[3]);
                    }
                }


                (int level, List<IPAddress> ips) = await NetworkHelper.GetRouteLevel(tunnelMessengerAdapter.ServerHost.ToString()).ConfigureAwait(false);
                networkInfo.RouteLevel = level;
                networkInfo.LocalIps = NetworkHelper.GetIPV6().Concat(NetworkHelper.GetIPV4()).ToArray();
                networkInfo.MachineId = tunnelMessengerAdapter.MachineId;

                async Task<IPAddress> GetLocalIP(IPEndPoint server)
                {
                    using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(16);
                    buffer.Memory.Span[0] = 255;

                    using CancellationTokenSource cts = new CancellationTokenSource(1000);
                    var socket = new Socket(server.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        await socket.ConnectAsync(server, cts.Token).ConfigureAwait(false);
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

        public void SetConnectedCallback(string transactionId, Action<ITunnelConnection, TunnelTransportInfo> callback)
        {
            if (OnConnectedDic.TryGetValue(transactionId, out List<Action<ITunnelConnection, TunnelTransportInfo>> callbacks) == false)
            {
                callbacks = new List<Action<ITunnelConnection, TunnelTransportInfo>>();
                OnConnectedDic[transactionId] = callbacks;
            }
            callbacks.Add(callback);
        }
        public void RemoveConnectedCallback(string transactionId, Action<ITunnelConnection, TunnelTransportInfo> callback)
        {
            if (OnConnectedDic.TryGetValue(transactionId, out List<Action<ITunnelConnection, TunnelTransportInfo>> callbacks))
            {
                callbacks.Remove(callback);
            }
        }


        public async Task<ITunnelConnection> ConnectAsync(string remoteMachineId, string transactionId,
            Dictionary<string, string> configures, TunnelType[] tunnelTypes = null, TunnelType[] exTunnelTypes = null,
            CancellationToken token = default)
        {
            configures ??= [];
            if (configures.TryGetValue("flag", out string flag) == false)
            {
                flag = "default";
            }

            return await operating.StartOperationAsync(BuildKey(remoteMachineId, transactionId, flag), true,
            async (key) =>
            {
                var transportItems = await GetTransports(remoteMachineId, tunnelTypes, exTunnelTypes).ConfigureAwait(false);
                foreach (TunnelTransportItemInfo transportItem in transportItems)
                {
                    ITunnelTransport transport = transports.FirstOrDefault(c => c.Name == transportItem.Name);
                    transport.SetSSL(tunnelMessengerAdapter.Certificate);

                    foreach (var wanPortProtocol in tunnelWanPortTransfer.Protocols.Where(c => (transport.AllowWanPortProtocolType & c) == c))
                    {
                        ITunnelConnection connection = await GetConnection(remoteMachineId, transactionId, transport, transportItem, wanPortProtocol, configures, token).ConfigureAwait(false);
                        if (connection != null) return connection;
                    }
                }
                return null;
            },
            async (key) => null).ConfigureAwait(false);
        }
        private async Task<List<TunnelTransportItemInfo>> GetTransports(string remoteMachineId, TunnelType[] tunnelTypes = null, TunnelType[] exTunnelTypes = null)
        {
            var query = (await tunnelMessengerAdapter.GetTunnelTransports(remoteMachineId).ConfigureAwait(false)).OrderBy(c => c.Order).Where(c => c.Disabled == false);
            if (tunnelTypes != null && tunnelTypes.Length > 0)
            {
                query = query.Where(c => tunnelTypes.Contains(c.TunnelType));
            }
            if (exTunnelTypes != null && exTunnelTypes.Length > 0)
            {
                query = query.Where(c => exTunnelTypes.Contains(c.TunnelType) == false);

            }
            return query.Where(c => transports.Any(t => t.Name == c.Name)).ToList();
        }
        private async Task<(TunnelTransportWanPortInfo local, TunnelTransportWanPortInfo remote)> GetWanPort(string machineId, TunnelWanPortProtocolType protocolType, ITunnelTransport transport)
        {
            //获取自己的外网ip
            Task<TunnelTransportWanPortInfo> localInfo = GetWanPort(protocolType);
            //获取对方的外网ip
            Task<TunnelTransportWanPortInfo> remoteInfo = tunnelMessengerAdapter.GetRemoteWanPort(new TunnelWanPortProtocolInfo
            {
                MachineId = machineId,
                ProtocolType = protocolType
            });
            await Task.WhenAll(localInfo, remoteInfo).ConfigureAwait(false);

            if (localInfo.Result == null)
            {
                LoggerHelper.Instance.Error($"tunnel {transport.Name} get local wan ip fail ");
                return (null, null);
            }

            if (remoteInfo.Result == null)
            {
                LoggerHelper.Instance.Error($"tunnel {transport.Name} get remote {machineId} wan ip fail ");
                return (null, null);
            }
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Info($"tunnel {transport.Name} got local wan ip {localInfo.Result.ToJson()}");
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Info($"tunnel {transport.Name} got remote wan ip {remoteInfo.Result.ToJson()}");

            return (localInfo.Result, remoteInfo.Result);
        }
        private async Task<ITunnelConnection> GetConnection(string remoteMachineId, string transactionId,
            ITunnelTransport transport, TunnelTransportItemInfo transportItem,
            TunnelWanPortProtocolType protocolType,
            Dictionary<string, string> configures, CancellationToken token)
        {
            TunnelTransportInfo tunnelTransportInfo = null;
            for (int i = 0; i <= Convert.ToInt32(transportItem.Reverse); i++)
            {
                if (token.IsCancellationRequested)
                {
                    return null;
                }
                (TunnelTransportWanPortInfo local, TunnelTransportWanPortInfo remote) = await GetWanPort(remoteMachineId, protocolType, transport).ConfigureAwait(false);

                tunnelTransportInfo = new TunnelTransportInfo
                {
                    Direction = (TunnelDirection)i,
                    TransactionId = transactionId,
                    Configure = configures,
                    TransportName = transport.Name,
                    TransportType = transport.ProtocolType,
                    Local = local,
                    Remote = remote,
                    SSL = transportItem.SSL,
                    FlowId = Interlocked.Increment(ref flowid),
                };
                OnConnecting(tunnelTransportInfo);
                ParseRemoteEndPoint(tunnelTransportInfo, transportItem.Addr);

                ITunnelConnection connection = await transport.ConnectAsync(tunnelTransportInfo).ConfigureAwait(false);
                if (connection != null)
                {
                    OnConnected(connection, tunnelTransportInfo);
                    return connection;
                }
            }
            if (tunnelTransportInfo != null)
            {
                OnConnectFail(tunnelTransportInfo);
            }
            return null;
        }

        public async Task OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            tunnelTransportInfo.Configure.TryGetValue("flag", out string flag);
            string key = BuildKey(tunnelTransportInfo.Remote.MachineId, tunnelTransportInfo.TransactionId, flag);

            await operating.StartOperationAsync(key,
            false,
            async (key) =>
            {
                try
                {
                    var _transports = await tunnelMessengerAdapter.GetTunnelTransports(tunnelTransportInfo.Remote.MachineId).ConfigureAwait(false);

                    ITunnelTransport transport = transports.FirstOrDefault(c => c.Name == tunnelTransportInfo.TransportName && c.ProtocolType == tunnelTransportInfo.TransportType);
                    TunnelTransportItemInfo item = _transports.FirstOrDefault(c => c.Name == tunnelTransportInfo.TransportName && c.Disabled == false);
                    if (transport != null && item != null)
                    {
                        transport.SetSSL(tunnelMessengerAdapter.Certificate);
                        OnConnectBegin(tunnelTransportInfo);
                        ParseRemoteEndPoint(tunnelTransportInfo, item.Addr);
                        _ = transport.OnBegin(tunnelTransportInfo).ContinueWith((result) =>
                        {
                            operating.StopOperation(key);
                        });
                    }
                    else
                    {
                        operating.StopOperation(key);
                        _ = tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo);
                    }
                }
                catch (Exception ex)
                {
                    operating.StopOperation(key);
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                }
            },
            async (key) =>
            {
                operating.StopOperation(key);
                _ = tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo);
            }).ConfigureAwait(false);
        }
        public void OnFail(TunnelTransportInfo tunnelTransportInfo)
        {
            ITunnelTransport _transports = transports.FirstOrDefault(c => c.Name == tunnelTransportInfo.TransportName && c.ProtocolType == tunnelTransportInfo.TransportType);
            _transports?.OnFail(tunnelTransportInfo);
        }
        public void OnSuccess(TunnelTransportInfo tunnelTransportInfo)
        {
            ITunnelTransport _transports = transports.FirstOrDefault(c => c.Name == tunnelTransportInfo.TransportName && c.ProtocolType == tunnelTransportInfo.TransportType);
            _transports?.OnSuccess(tunnelTransportInfo);
        }

        public async Task<TunnelTransportWanPortInfo> GetWanPort(TunnelWanPortProtocolInfo _info)
        {
            return await GetWanPort(_info.ProtocolType).ConfigureAwait(false);
        }
        private async Task<TunnelTransportWanPortInfo> GetWanPort(TunnelWanPortProtocolType tunnelWanPortProtocolType)
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
            if (tunnelMessengerAdapter.InIp != null && IPAddress.Any.Equals(tunnelMessengerAdapter.InIp) == false)
            {
                ip.Remote.Address = tunnelMessengerAdapter.InIp;
            }

            MapInfo portMapInfo = tunnelUpnpTransfer.PortMap ?? new MapInfo { PrivatePort = 0, PublicPort = 0 };
            IPAddress[] lans = (networkInfo.LocalIps ?? new IPAddress[0]).Concat(new[] { tunnelUpnpTransfer.WanIp }).Where(c => IPAddress.Any.Equals(c) == false).ToArray();

            return new TunnelTransportWanPortInfo
            {
                Local = ip.Local,
                Remote = ip.Remote,
                LocalIps = lans,
                RouteLevel = networkInfo.RouteLevel + tunnelMessengerAdapter.RouteLevelPlus,
                MachineId = tunnelMessengerAdapter.MachineId,
                PortMapLan = portMapInfo.PrivatePort,
                PortMapWan = portMapInfo.PublicPort,
                PredictPorts = radarTransfer.Predict(ip.Remote.Port).ToArray()
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
        private void OnConnected(ITunnelConnection connection, TunnelTransportInfo info)
        {
            //调用以下别人注册的回调
            if (OnConnectedDic.TryGetValue(Helper.GlobalString, out List<Action<ITunnelConnection, TunnelTransportInfo>> callbacks))
            {
                foreach (var item in callbacks)
                {
                    item(connection, info);
                }
            }
            if (OnConnectedDic.TryGetValue(connection.TransactionId, out callbacks))
            {
                foreach (var item in callbacks)
                {
                    item(connection, info);
                }
            }
        }
        private static void OnConnectFail(TunnelTransportInfo tunnelTransportInfo)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Error($"tunnel connect {tunnelTransportInfo.Remote.MachineId} fail");
        }

        private void ParseRemoteEndPoint(TunnelTransportInfo tunnelTransportInfo, Addrs addr)
        {
            if (tunnelTransportInfo.Local == null || tunnelTransportInfo.Remote == null) return;

            //要连接哪些IP
            List<IPEndPoint> eps = new List<IPEndPoint>();
            var excludeips = tunnelMessengerAdapter.GetExclusionPolicy();

            //先尝试内网ipv4
            if (tunnelTransportInfo.Local.Remote.Address.Equals(tunnelTransportInfo.Remote.Remote.Address))
            {
                eps.AddRange(tunnelTransportInfo.Remote.LocalIps.Where(c => c.AddressFamily == AddressFamily.InterNetwork).SelectMany(c => new List<IPEndPoint>
                {
                    new IPEndPoint(c, tunnelTransportInfo.Remote.Local.Port),
                }));
            }
            //再尝试外网ip，UDP的话就多试几个端口
            if (tunnelTransportInfo.TransportType == TunnelProtocolType.Udp)
            {
                if (tunnelTransportInfo.Remote.PredictPorts.Length > 0)
                {
                    eps.Add(new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address, tunnelTransportInfo.Remote.Remote.Port));
                    foreach (var item in tunnelTransportInfo.Remote.PredictPorts)
                    {
                        eps.Add(new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address, item));
                    }
                }
                else
                {
                    for (int i = tunnelTransportInfo.Remote.Remote.Port - 200; i < tunnelTransportInfo.Remote.Remote.Port + 200; i++)
                    {
                        eps.Add(new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address, i));
                    }
                }
            }
            else
            {
                eps.AddRange(new List<IPEndPoint>{
                    new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address,tunnelTransportInfo.Remote.Remote.Port),
                    new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address,tunnelTransportInfo.Remote.Remote.Port+1),
                    new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address,tunnelTransportInfo.Remote.Remote.Port+2),
                });
            }

            //再尝试IPV6
            eps.AddRange(tunnelTransportInfo.Remote.LocalIps.Where(c => c.AddressFamily == AddressFamily.InterNetworkV6).SelectMany(c => new List<IPEndPoint>
            {
                new IPEndPoint(c, tunnelTransportInfo.Remote.Local.Port),
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


            if (addr.HasFlag(Addrs.Ipv6) == false)
            {
                eps = eps.Where(c => c.AddressFamily != AddressFamily.InterNetworkV6).ToList();
            }
            if (addr.HasFlag(Addrs.Ipv4) == false)
            {
                eps = eps.Where(c => c.Address.Equals(tunnelTransportInfo.Remote.Remote.Address) == false).ToList();
            }
            if (addr.HasFlag(Addrs.Lan) == false)
            {
                eps = eps.Where(c => tunnelTransportInfo.Remote.LocalIps.Contains(c.Address) == false || c.Address.AddressFamily == AddressFamily.InterNetworkV6).ToList();
            }

            tunnelTransportInfo.RemoteEndPoints = eps;
        }
        private static string BuildKey(string remoteMachineId, string transactionId, string flag)
        {
            if (string.IsNullOrWhiteSpace(flag))
            {
                return $"{remoteMachineId}@{transactionId}";
            }

            return $"{remoteMachineId}@{transactionId}@{flag}";
        }

        private ConcurrentDictionary<string, CancellationTokenSource> backgroundDic = new ConcurrentDictionary<string, CancellationTokenSource>();
        public void StartBackground(string remoteMachineId, string transactionId, Dictionary<string, string> configures, Func<bool> stopCallback, Func<ITunnelConnection, Task> resultCallback, int times = 10, int delay = 10000)
        {
            if (AddBackground(remoteMachineId, transactionId, out CancellationTokenSource cts) == false)
            {
                cts.Cancel();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error($"tunnel background {remoteMachineId}@{transactionId} already exists");
                return;
            }
            TimerHelper.Async(async () =>
            {
                try
                {
                    configures["flag"] = "back";
                    ITunnelConnection connection = null;

                    await Task.Delay(delay).ConfigureAwait(false);
                    for (int i = 1; i <= times; i++)
                    {
                        if (stopCallback()) break;

                        connection = await ConnectAsync(remoteMachineId, transactionId, configures, tunnelTypes: [TunnelType.P2P], token: cts.Token).ConfigureAwait(false);
                        if (connection != null)
                        {
                            break;
                        }
                        await Task.Delay(3000).ConfigureAwait(false);
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
        private bool AddBackground(string remoteMachineId, string transactionId, out CancellationTokenSource cts)
        {
            cts = new CancellationTokenSource();
            return backgroundDic.TryAdd(GetBackgroundKey(remoteMachineId, transactionId), cts);
        }
        public void RemoveBackground(string remoteMachineId, string transactionId)
        {
            if (backgroundDic.TryRemove(GetBackgroundKey(remoteMachineId, transactionId), out CancellationTokenSource cts))
            {
                cts.Cancel();
            }
        }
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
