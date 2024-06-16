using cmonitor.tunnel.adapter;
using cmonitor.tunnel.compact;
using cmonitor.tunnel.connection;
using cmonitor.tunnel.transport;
using common.libs;
using common.libs.extends;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;

namespace cmonitor.tunnel
{
    public sealed class TunnelTransfer
    {
        private List<ITunnelTransport> transports;
        private readonly ServiceProvider serviceProvider;

        private readonly TunnelCompactTransfer compactTransfer;
        private readonly ITunnelAdapter tunnelMessengerAdapter;

        private ConcurrentDictionary<string, bool> connectingDic = new ConcurrentDictionary<string, bool>();
        private Dictionary<string, List<Action<ITunnelConnection>>> OnConnected { get; } = new Dictionary<string, List<Action<ITunnelConnection>>>();

        public TunnelTransfer(ServiceProvider serviceProvider, TunnelCompactTransfer compactTransfer, ITunnelAdapter tunnelMessengerAdapter)
        {
            this.serviceProvider = serviceProvider;
            this.compactTransfer = compactTransfer;
            this.tunnelMessengerAdapter = tunnelMessengerAdapter;
        }

        public void Load(Assembly[] assembs)
        {
            IEnumerable<Type> types = ReflectionHelper.GetInterfaceSchieves(assembs.Concat(new Assembly[] { typeof(TunnelTransfer).Assembly }).ToArray(), typeof(ITunnelTransport));
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


        public async Task<ITunnelConnection> ConnectAsync(string remoteMachineId, string transactionId)
        {
            connectingDic.AddOrUpdate(remoteMachineId, true, (a, b) => true);

            foreach (TunnelTransportItemInfo transportItem in tunnelMessengerAdapter.GetTunnelTransports().Where(c => c.Disabled == false))
            {
                ITunnelTransport transport = transports.FirstOrDefault(c => c.Name == transportItem.Name);
                if (transport == null) continue;

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
                            break;
                        }
                        Logger.Instance.Info($"tunnel {transport.Name} got local external ip {localInfo.ToJson()}");
                        //获取对方的外网ip
                        TunnelTransportExternalIPInfo remoteInfo = await tunnelMessengerAdapter.GetRemoteExternalIP(remoteMachineId);
                        if (remoteInfo == null)
                        {
                            Logger.Instance.Error($"tunnel {transport.Name} get remote {remoteMachineId} external ip fail ");
                            break;
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
                            connectingDic.TryRemove(remoteMachineId, out _);
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
                if (tunnelTransportInfo != null)
                {
                    OnConnectFail(tunnelTransportInfo);
                }
            }
            connectingDic.TryRemove(remoteMachineId, out _);
            return null;
        }
        public void OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            if (connectingDic.TryGetValue(tunnelTransportInfo.Remote.MachineId, out _))
            {
                return;
            }
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
                    MachineId = config.MachineId
                };
            }
            return null;
        }

        private void OnConnecting(TunnelTransportInfo tunnelTransportInfo)
        {
            //if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            Logger.Instance.Info($"tunnel connecting {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName},{tunnelTransportInfo.ToJson()}");
        }
        private void OnConnectBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            //if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            Logger.Instance.Info($"tunnel connecting from {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName},{tunnelTransportInfo.ToJson()}");
        }

        private void _OnConnected(ITunnelConnection connection)
        {
            //if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            Logger.Instance.Debug($"tunnel connect {connection.RemoteMachineId}->{connection.RemoteMachineName} success->{connection.IPEndPoint},{connection.ToJsonFormat()}");

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
            Logger.Instance.Error($"tunnel connect {tunnelTransportInfo.Remote.MachineId} fail->{tunnelTransportInfo.ToJsonFormat()}");
        }
    }
}
