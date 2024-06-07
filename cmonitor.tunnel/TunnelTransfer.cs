using cmonitor.tunnel.adapter;
using cmonitor.tunnel.compact;
using cmonitor.tunnel.connection;
using cmonitor.tunnel.transport;
using common.libs;
using common.libs.extends;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.tunnel
{
    public sealed class TunnelTransfer
    {
        private List<ITunnelTransport> transports;
        private readonly ServiceProvider serviceProvider;

        private readonly TunnelCompactTransfer compactTransfer;
        private readonly ITunnelAdapter tunnelMessengerAdapter;

        private Dictionary<string, List<Action<ITunnelConnection>>> OnConnected { get; } = new Dictionary<string, List<Action<ITunnelConnection>>>();

        public TunnelTransfer(ServiceProvider serviceProvider, TunnelCompactTransfer compactTransfer, ITunnelAdapter tunnelMessengerAdapter)
        {
            this.serviceProvider = serviceProvider;
            this.compactTransfer = compactTransfer;
            this.tunnelMessengerAdapter = tunnelMessengerAdapter;
        }

        public void Load(Assembly[] assembs)
        {
            IEnumerable<Type> types = ReflectionHelper.GetInterfaceSchieves(assembs, typeof(ITunnelTransport));
            transports = types.Select(c => (ITunnelTransport)serviceProvider.GetService(c)).Where(c => c != null).Where(c => string.IsNullOrWhiteSpace(c.Name) == false).ToList();
            foreach (var item in transports)
            {
                item.OnSendConnectBegin = tunnelMessengerAdapter.SendConnectBegin;
                item.OnSendConnectFail = tunnelMessengerAdapter.SendConnectFail;
                item.OnSendConnectSuccess = tunnelMessengerAdapter.SendConnectSuccess;
                item.OnConnected = _OnConnected;
            }

            var transportItems = tunnelMessengerAdapter.GetTunnelTransports();
            transportItems = transportItems.Concat(transports.Select(c => new TunnelTransportItemInfo { Label = c.Label, Name = c.Name, ProtocolType = c.ProtocolType.ToString() }))
                .Distinct(new TunnelTransportItemInfoEqualityComparer())
                .ToList();
            tunnelMessengerAdapter.SetTunnelTransports(transportItems);

            Logger.Instance.Warning($"load tunnel transport:{string.Join(",", transports.Select(c => c.Name))}");
            Logger.Instance.Warning($"used tunnel transport:{string.Join(",", transportItems.Where(c => c.Disabled == false).Select(c => c.Name))}");
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


        public async Task<ITunnelConnection> ConnectAsync(string remoteMachineName, string transactionId)
        {
            foreach (TunnelTransportItemInfo transportItem in tunnelMessengerAdapter.GetTunnelTransports().Where(c => c.Disabled == false))
            {
                ITunnelTransport transport = transports.FirstOrDefault(c => c.Name == transportItem.Name);
                if (transport == null) continue;
                /*
                 * 我们不能连续获取端口，在正向连接失败后再尝试反向
                 * 
                 * 因为，短时间内，连续进行网络连接，大概率会得到连续的端口
                 * 
                 * 假设，第一次正向连接获取到外网端口为 12345，那我们将会尝试对 12345 12346 进行连接，会对12346有所污染
                 * 
                 * 所以，我们需要在第一次正向连接失败后再尝试反向连接，因为间隔了一定时间，最大程度避免了连续端口污染
                 */
                TunnelTransportInfo tunnelTransportInfo = null;
                int times = transportItem.Reverse ? 1 : 0;
                for (int i = 0; i <= times; i++)
                {
                    try
                    {
                        //获取自己的外网ip
                        TunnelTransportExternalIPInfo localInfo = await GetLocalInfo();
                        if (localInfo == null)
                        {
                            Logger.Instance.Error($"tunnel {transport.Name} get local external ip fail ");
                            goto end;
                        }
                        Logger.Instance.Info($"tunnel {transport.Name} got local external ip {localInfo.ToJson()}");
                        //获取对方的外网ip
                        TunnelTransportExternalIPInfo remoteInfo = await tunnelMessengerAdapter.GetRemoteExternalIP(remoteMachineName);
                        if (remoteInfo == null)
                        {
                            Logger.Instance.Error($"tunnel {transport.Name} get remote {remoteMachineName} external ip fail ");
                            goto end;
                        }
                        Logger.Instance.Info($"tunnel {transport.Name} got remote external ip {remoteInfo.ToJson()}");

                        tunnelTransportInfo = new TunnelTransportInfo
                        {
                            Direction = (TunnelDirection)i,
                            TransactionId = transactionId,
                            TransportName = transport.Name,
                            TransportType = transport.ProtocolType,
                            Local = localInfo,
                            Remote = remoteInfo,
                            SSL = transportItem.SSL
                        };
                        OnConnecting(tunnelTransportInfo);
                        ITunnelConnection connection = await transport.ConnectAsync(tunnelTransportInfo);
                        if (connection != null)
                        {
                            _OnConnected(connection);
                            return connection;
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
            end:
                if (tunnelTransportInfo != null)
                {
                    OnConnectFail(tunnelTransportInfo);
                }
            }
            return null;
        }
        public void OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            ITunnelTransport _transports = transports.FirstOrDefault(c => c.Name == tunnelTransportInfo.TransportName && c.ProtocolType == tunnelTransportInfo.TransportType);
            if (_transports != null)
            {
                _transports.OnBegin(tunnelTransportInfo);
                OnConnectBegin(tunnelTransportInfo);
            }
        }
        public void OnFail(TunnelTransportInfo tunnelTransportInfo)
        {
            ITunnelTransport _transports = transports.FirstOrDefault(c => c.Name == tunnelTransportInfo.TransportName && c.ProtocolType == tunnelTransportInfo.TransportType);
            if (_transports != null)
            {
                _transports.OnFail(tunnelTransportInfo);
            }
        }
        public void OnSuccess(TunnelTransportInfo tunnelTransportInfo)
        {
            ITunnelTransport _transports = transports.FirstOrDefault(c => c.Name == tunnelTransportInfo.TransportName && c.ProtocolType == tunnelTransportInfo.TransportType);
            if (_transports != null)
            {
                _transports.OnSuccess(tunnelTransportInfo);
            }
        }
        public async Task<TunnelTransportExternalIPInfo> GetExternalIP()
        {
            return await GetLocalInfo();
        }

        private async Task<TunnelTransportExternalIPInfo> GetLocalInfo()
        {
            TunnelCompactIPEndPoint ip = await compactTransfer.GetExternalIPAsync(tunnelMessengerAdapter.LocalIP);
            if (ip != null)
            {
                var config = tunnelMessengerAdapter.GetLocalConfig();
                return new TunnelTransportExternalIPInfo
                {
                    Local = ip.Local,
                    Remote = ip.Remote,
                    LocalIps = config.LocalIps,
                    RouteLevel = config.RouteLevel,
                    MachineName = config.MachineName
                };
            }
            return null;
        }

        private void OnConnecting(TunnelTransportInfo tunnelTransportInfo)
        {
            //if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            Logger.Instance.Info($"tunnel connecting {tunnelTransportInfo.Remote.MachineName},{tunnelTransportInfo.ToJson()}");
        }
        private void OnConnectBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            //if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            Logger.Instance.Info($"tunnel connecting from {tunnelTransportInfo.Remote.MachineName},{tunnelTransportInfo.ToJson()}");
        }

        private void _OnConnected(ITunnelConnection connection)
        {
            //if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            Logger.Instance.Debug($"tunnel connect {connection.RemoteMachineName} success->{connection.IPEndPoint},{connection.ToJsonFormat()}");

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
            Logger.Instance.Error($"tunnel connect {tunnelTransportInfo.Remote.MachineName} fail->{tunnelTransportInfo.ToJsonFormat()}");
        }
    }
}
