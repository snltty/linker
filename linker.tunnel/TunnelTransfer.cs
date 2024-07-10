using linker.tunnel.adapter;
using linker.tunnel.connection;
using linker.tunnel.transport;
using linker.libs;
using linker.libs.extends;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using linker.tunnel.wanport;

namespace linker.tunnel
{
    public sealed class TunnelTransfer
    {
        private List<ITunnelTransport> transports;
        private TunnelWanPortTransfer compactTransfer;
        private ITunnelAdapter tunnelAdapter;

        private ConcurrentDictionary<string, bool> connectingDic = new ConcurrentDictionary<string, bool>();
        private Dictionary<string, List<Action<ITunnelConnection>>> OnConnected { get; } = new Dictionary<string, List<Action<ITunnelConnection>>>();

        public TunnelTransfer()
        {
        }

        /// <summary>
        /// 加载打洞协议
        /// </summary>
        /// <param name="assembs"></param>
        public void Init(TunnelWanPortTransfer compactTransfer, ITunnelAdapter tunnelAdapter, List<ITunnelTransport> transports)
        {
            this.compactTransfer = compactTransfer;
            this.tunnelAdapter = tunnelAdapter;
            this.transports = transports;

            foreach (var item in transports)
            {
                item.OnSendConnectBegin = tunnelAdapter.SendConnectBegin;
                item.OnSendConnectFail = tunnelAdapter.SendConnectFail;
                item.OnSendConnectSuccess = tunnelAdapter.SendConnectSuccess;
                item.OnConnected = _OnConnected;
            }

            var transportItems = tunnelAdapter.GetTunnelTransports();
            var names = transportItems.Select(c => c.Name);
            transportItems = transportItems.Concat(transports.Select(c => new TunnelTransportItemInfo
            {
                Label = c.Label,
                Name = c.Name,
                ProtocolType = c.ProtocolType.ToString(),
                Reverse = c.Reverse,
                DisableReverse = c.DisableReverse,
                SSL = c.SSL,
                DisableSSL = c.DisableSSL
            }))
                .Distinct(new TunnelTransportItemInfoEqualityComparer())
                .Where(c => transports.Select(c => c.Name).Contains(c.Name))
                .ToList();

            foreach (var item in transportItems)
            {
                var transport = transports.FirstOrDefault(c => c.Name == item.Name);
                if (transport != null)
                {
                    item.DisableReverse = transport.DisableReverse;
                    item.DisableSSL = transport.DisableSSL;
                    if (transport.DisableReverse)
                    {
                        item.Reverse = transport.Reverse;
                    }
                    if (transport.DisableSSL)
                    {
                        item.SSL = transport.SSL;
                    }
                }
            }

            tunnelAdapter.SetTunnelTransports(transportItems, names.SequenceEqual(transportItems.Select(c => c.Name)) == false);

            LoggerHelper.Instance.Warning($"load tunnel transport:{string.Join(",", transports.Select(c => c.Name))}");
            LoggerHelper.Instance.Warning($"used tunnel transport:{string.Join(",", transportItems.Where(c => c.Disabled == false).Select(c => c.Name))}");
        }


        /// <summary>
        /// 设置成功打洞回调
        /// </summary>
        /// <param name="transactionId"></param>
        /// <param name="callback"></param>
        public void SetConnectedCallback(string transactionId, Action<ITunnelConnection> callback)
        {
            if (OnConnected.TryGetValue(transactionId, out List<Action<ITunnelConnection>> callbacks) == false)
            {
                callbacks = new List<Action<ITunnelConnection>>();
                OnConnected[transactionId] = callbacks;
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
            if (OnConnected.TryGetValue(transactionId, out List<Action<ITunnelConnection>> callbacks))
            {
                callbacks.Remove(callback);
            }
        }


        /// <summary>
        /// 开始连接对方
        /// </summary>
        /// <param name="remoteMachineId">对方id</param>
        /// <param name="transactionId">事务id，随便起，你喜欢就好</param>
        /// <returns></returns>
        public async Task<ITunnelConnection> ConnectAsync(string remoteMachineId, string transactionId)
        {
            if (connectingDic.TryAdd(remoteMachineId, true) == false) return null;
            if (IsBackground(remoteMachineId, transactionId)) return null;

            try
            {
                foreach (TunnelTransportItemInfo transportItem in tunnelAdapter.GetTunnelTransports().Where(c => c.Disabled == false))
                {
                    ITunnelTransport transport = transports.FirstOrDefault(c => c.Name == transportItem.Name);
                    //找不到这个打洞协议
                    if (transport == null)
                    {
                        continue;
                    }

                    foreach (var wanPortProtocol in tunnelAdapter.GetTunnelWanPortProtocols().Where(c => c.Disabled == false && string.IsNullOrWhiteSpace(c.Host) == false))
                    {

                        //这个打洞协议不支持这个外网端口协议
                        if ((transport.AllowWanPortProtocolType & wanPortProtocol.ProtocolType) != wanPortProtocol.ProtocolType)
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
                                Task<TunnelTransportWanPortInfo> remoteInfo = tunnelAdapter.GetRemoteWanPort(new TunnelWanPortProtocolInfo
                                {
                                    MachineId = remoteMachineId,
                                    ProtocolType = wanPortProtocol.ProtocolType,
                                    Type = wanPortProtocol.Type,
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
                                LoggerHelper.Instance.Info($"tunnel {transport.Name} got local external ip {localInfo.Result.ToJson()}");
                                LoggerHelper.Instance.Info($"tunnel {transport.Name} got remote external ip {remoteInfo.Result.ToJson()}");


                                tunnelTransportInfo = new TunnelTransportInfo
                                {
                                    Direction = (TunnelDirection)i,
                                    TransactionId = transactionId,
                                    TransportName = transport.Name,
                                    TransportType = transport.ProtocolType,
                                    Local = localInfo.Result,
                                    Remote = remoteInfo.Result,
                                    SSL = transportItem.SSL
                                };
                                OnConnecting(tunnelTransportInfo);
                                ParseRemoteEndPoint(tunnelTransportInfo);
                                ITunnelConnection connection = await transport.ConnectAsync(tunnelTransportInfo).ConfigureAwait(false);
                                if (connection != null)
                                {
                                    _OnConnected(connection);
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
                connectingDic.TryRemove(remoteMachineId, out _);
            }
            return null;
        }
        /// <summary>
        /// 收到对方开始连接的消息
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        public void OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            if (connectingDic.TryAdd(tunnelTransportInfo.Remote.MachineId, true) == false)
            {
                return;
            }
            try
            {
                ITunnelTransport _transports = transports.FirstOrDefault(c => c.Name == tunnelTransportInfo.TransportName && c.ProtocolType == tunnelTransportInfo.TransportType);
                if (_transports != null)
                {
                    OnConnectBegin(tunnelTransportInfo);
                    ParseRemoteEndPoint(tunnelTransportInfo);
                    _transports.OnBegin(tunnelTransportInfo).ContinueWith((result) =>
                    {
                        connectingDic.TryRemove(tunnelTransportInfo.Remote.MachineId, out _);
                    });
                }
                else
                {
                    connectingDic.TryRemove(tunnelTransportInfo.Remote.MachineId, out _);
                }
            }
            catch (Exception ex)
            {
                connectingDic.TryRemove(tunnelTransportInfo.Remote.MachineId, out _);
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
            TunnelWanPortInfo info = tunnelAdapter.GetTunnelWanPortProtocols().FirstOrDefault(c => c.Type == _info.Type && c.ProtocolType == _info.ProtocolType);
            if (info == null) return null;
            return await GetLocalInfo(info).ConfigureAwait(false);
        }

        /// <summary>
        /// 获取自己的外网IP
        /// </summary>
        /// <returns></returns>
        private async Task<TunnelTransportWanPortInfo> GetLocalInfo(TunnelWanPortInfo info)
        {
            TunnelWanPortEndPoint ip = await compactTransfer.GetWanPortAsync(tunnelAdapter.LocalIP, info).ConfigureAwait(false);
            if (ip != null)
            {
                var config = tunnelAdapter.GetLocalConfig();
                return new TunnelTransportWanPortInfo
                {
                    Local = ip.Local,
                    Remote = ip.Remote,
                    LocalIps = config.LocalIps,
                    RouteLevel = config.RouteLevel,
                    MachineId = config.MachineId
                };
            }
            return null;
        }

        private void OnConnecting(TunnelTransportInfo tunnelTransportInfo)
        {
            //if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            LoggerHelper.Instance.Info($"tunnel connecting {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName},{tunnelTransportInfo.ToJson()}");
        }
        private void OnConnectBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            //if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            LoggerHelper.Instance.Info($"tunnel connecting from {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName},{tunnelTransportInfo.ToJson()}");
        }

        /// <summary>
        /// 连接成功
        /// </summary>
        /// <param name="connection"></param>
        private void _OnConnected(ITunnelConnection connection)
        {
            //if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            LoggerHelper.Instance.Debug($"tunnel connect {connection.RemoteMachineId}->{connection.RemoteMachineName} success->{connection.IPEndPoint},{connection.ToJsonFormat()}");

            //调用以下别人注册的回调
            if (OnConnected.TryGetValue(Helper.GlobalString, out List<Action<ITunnelConnection>> callbacks))
            {
                foreach (var item in callbacks)
                {
                    item(connection);
                }
            }
            if (OnConnected.TryGetValue(connection.TransactionId, out callbacks))
            {
                foreach (var item in callbacks)
                {
                    item(connection);
                }
            }
        }
        private void OnConnectFail(TunnelTransportInfo tunnelTransportInfo)
        {
            LoggerHelper.Instance.Error($"tunnel connect {tunnelTransportInfo.Remote.MachineId} fail->{tunnelTransportInfo.ToJsonFormat()}");
        }

        private void ParseRemoteEndPoint(TunnelTransportInfo tunnelTransportInfo)
        {
            //要连接哪些IP
            IPAddress[] localIps = tunnelTransportInfo.Remote.LocalIps.Where(c => c.Equals(tunnelTransportInfo.Remote.Local.Address) == false).ToArray();
            List<IPEndPoint> eps = new List<IPEndPoint>();

            //先尝试内网ipv4
            foreach (IPAddress item in localIps.Where(c => c.AddressFamily == AddressFamily.InterNetwork))
            {
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Local.Port));
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Remote.Port));
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Remote.Port + 1));
            }
            //在尝试外网
            eps.AddRange(new List<IPEndPoint>{
                new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address,tunnelTransportInfo.Remote.Remote.Port),
                new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address,tunnelTransportInfo.Remote.Remote.Port+1),
            });
            //再尝试IPV6
            foreach (IPAddress item in localIps.Where(c => c.AddressFamily == AddressFamily.InterNetworkV6))
            {
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Local.Port));
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Remote.Port));
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Remote.Port + 1));
            }
            //本机有V6
            bool hasV6 = tunnelTransportInfo.Local.LocalIps.Any(c => c.AddressFamily == AddressFamily.InterNetworkV6);
            //本机的局域网ip和外网ip
            List<IPAddress> localLocalIps = tunnelTransportInfo.Local.LocalIps.Concat(new List<IPAddress> { tunnelTransportInfo.Local.Remote.Address }).ToList();
            eps = eps
                //对方是V6，本机也得有V6
                .Where(c => (c.AddressFamily == AddressFamily.InterNetworkV6 && hasV6) || c.AddressFamily == AddressFamily.InterNetwork)
                //端口和本机端口一样，那不应该是换回地址
                .Where(c => (c.Port == tunnelTransportInfo.Local.Local.Port && c.Address.Equals(IPAddress.Loopback)) == false)
                //端口和本机端口一样。那不应该是本机的IP
                .Where(c => (c.Port == tunnelTransportInfo.Local.Local.Port && localLocalIps.Any(d => d.Equals(c.Address))) == false)
                .ToList();

            tunnelTransportInfo.RemoteEndPoints = eps;
        }



        private ConcurrentDictionary<string, bool> backgroundDic = new ConcurrentDictionary<string, bool>();
        public void StartBackground(string remoteMachineId, string transactionId)
        {
            if (IsBackground(remoteMachineId, transactionId)) return;
            AddBackground(remoteMachineId, transactionId);
            Task.Run(async () =>
            {
                try
                {
                    for (int i = 0; i < 10; i++)
                    {
                        await Task.Delay(3000);

                        ITunnelConnection connection = await ConnectAsync(remoteMachineId, transactionId);
                        if (connection != null)
                        {
                            break;
                        }
                    }
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
        private void AddBackground(string remoteMachineId, string transactionId)
        {
            backgroundDic.TryAdd(GetBackgroundKey(remoteMachineId, transactionId), true);
        }
        private void RemoveBackground(string remoteMachineId, string transactionId)
        {
            backgroundDic.TryRemove(GetBackgroundKey(remoteMachineId, transactionId), out _);
        }
        private bool IsBackground(string remoteMachineId, string transactionId)
        {
            return backgroundDic.ContainsKey(GetBackgroundKey(remoteMachineId, transactionId));
        }
        private string GetBackgroundKey(string remoteMachineId, string transactionId)
        {
            return $"{remoteMachineId}@{transactionId}";
        }
    }
}
