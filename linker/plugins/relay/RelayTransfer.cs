using linker.client.config;
using linker.config;
using linker.plugins.relay.transport;
using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Net;
using System.Reflection;

namespace linker.plugins.relay
{
    /// <summary>
    /// 中继
    /// </summary>
    public sealed class RelayTransfer
    {
        private List<ITransport> transports;

        private readonly RunningConfig running;
        private readonly ServiceProvider serviceProvider;

        private ConcurrentDictionary<string, bool> connectingDic = new ConcurrentDictionary<string, bool>();
        private Dictionary<string, List<Action<ITunnelConnection>>> OnConnected { get; } = new Dictionary<string, List<Action<ITunnelConnection>>>();

        public RelayTransfer(RunningConfig running, ServiceProvider serviceProvider, ConfigWrap config)
        {
            this.running = running;
            this.serviceProvider = serviceProvider;

            if (running.Data.Relay.Servers.Length == 0)
            {
                running.Data.Relay.Servers = new RelayServerInfo[]
                {
                     new RelayServerInfo{
                         Name="默认",
                         RelayType= RelayType.Linker,
                         Disabled = false,
                         Host = running.Data.Client.Servers.FirstOrDefault().Host
                     }
                };
            }
        }

        /// <summary>
        /// 加载中继协议
        /// </summary>
        /// <param name="assembs"></param>
        public void Load(Assembly[] assembs)
        {
            IEnumerable<Type> types = ReflectionHelper.GetInterfaceSchieves(assembs, typeof(ITransport));
            transports = types.Select(c => (ITransport)serviceProvider.GetService(c)).Where(c => c != null).Where(c => string.IsNullOrWhiteSpace(c.Name) == false).ToList();

            LoggerHelper.Instance.Warning($"load relay transport:{string.Join(",", transports.Select(c => c.Name))}");
        }
        /// <summary>
        /// 获取所有中继协议
        /// </summary>
        /// <returns></returns>
        public List<RelayTypeInfo> GetTypes()
        {
            return transports.Select(c => new RelayTypeInfo { Value = c.Type, Name = c.Type.ToString() }).Distinct(new RelayCompactTypeInfoEqualityComparer()).ToList();
        }
        /// <summary>
        /// 收到中继协议列表
        /// </summary>
        /// <param name="servers"></param>
        public void OnServers(RelayServerInfo[] servers)
        {
            running.Data.Relay.Servers = servers;
            running.Data.Update();
        }
        /// <summary>
        /// 设置中继成功回调
        /// </summary>
        /// <param name="transactionId">事务</param>
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
        /// 一处中继成功回调
        /// </summary>
        /// <param name="transactionId">事务</param>
        /// <param name="callback"></param>
        public void RemoveConnectedCallback(string transactionId, Action<ITunnelConnection> callback)
        {
            if (OnConnected.TryGetValue(transactionId, out List<Action<ITunnelConnection>> callbacks))
            {
                callbacks.Remove(callback);
            }
        }
        /// <summary>
        /// 中继连接对方
        /// </summary>
        /// <param name="fromMachineId">自己的id</param>
        /// <param name="remoteMachineId">对方id</param>
        /// <param name="transactionId">事务</param>
        /// <returns></returns>
        public async Task<ITunnelConnection> ConnectAsync(string fromMachineId, string remoteMachineId, string transactionId)
        {
            if (connectingDic.TryAdd(remoteMachineId, true) == false)
            {
                return null;
            }
            try
            {
                IEnumerable<ITransport> _transports = transports.OrderBy(c => c.Type);
                foreach (RelayServerInfo item in running.Data.Relay.Servers.Where(c => c.Disabled == false && string.IsNullOrWhiteSpace(c.Host) == false))
                {
                    ITransport transport = _transports.FirstOrDefault(c => c.Type == item.RelayType);
                    if (transport == null)
                    {
                        continue;
                    }

                    IPEndPoint server = NetworkHelper.GetEndPoint(item.Host, 3478);
                    RelayInfo relayInfo = new RelayInfo
                    {
                        FlowingId = 0,
                        FromMachineId = fromMachineId,
                        FromMachineName = string.Empty,
                        RemoteMachineId = remoteMachineId,
                        RemoteMachineName = string.Empty,
                        SecretKey = item.SecretKey,
                        Server = server,
                        TransactionId = transactionId,
                        TransportName = transport.Name,
                        SSL = item.SSL
                    };

                    LoggerHelper.Instance.Info($"relay to {relayInfo.RemoteMachineId}->{relayInfo.RemoteMachineName} {relayInfo.ToJson()}");
                    ITunnelConnection connection = await transport.RelayAsync(relayInfo).ConfigureAwait(false);
                    if (connection != null)
                    {
                        LoggerHelper.Instance.Debug($"relay to {relayInfo.RemoteMachineId}->{relayInfo.RemoteMachineName} success,{relayInfo.ToJson()}");
                        ConnectedCallback(relayInfo, connection);
                        return connection;
                    }
                    else
                    {
                        LoggerHelper.Instance.Error($"relay to {relayInfo.RemoteMachineId}->{relayInfo.RemoteMachineName} fail,{relayInfo.ToJson()}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            finally
            {
                connectingDic.TryRemove(remoteMachineId, out _);
            }

            return null;
        }
        /// <summary>
        /// 收到对方的中继请求
        /// </summary>
        /// <param name="relayInfo"></param>
        /// <returns></returns>
        public async Task<bool> OnBeginAsync(RelayInfo relayInfo)
        {
            if (connectingDic.TryAdd(relayInfo.FromMachineId, true) == false)
            {
                return false;
            }

            try
            {
                ITransport _transports = transports.FirstOrDefault(c => c.Name == relayInfo.TransportName);
                if (_transports != null)
                {
                    ITunnelConnection connection = await _transports.OnBeginAsync(relayInfo).ConfigureAwait(false);
                    if (connection != null)
                    {
                        LoggerHelper.Instance.Debug($"relay from {relayInfo.RemoteMachineId}->{relayInfo.RemoteMachineName} success,{relayInfo.ToJson()}");
                        ConnectedCallback(relayInfo, connection);
                        return true;
                    }
                }

            }
            catch (Exception ex)
            {
                if(LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            finally
            {
                connectingDic.TryRemove(relayInfo.FromMachineId, out _);
            }
            return false;
        }
        /// <summary>
        /// 回调
        /// </summary>
        /// <param name="relayInfo"></param>
        /// <param name="connection"></param>
        private void ConnectedCallback(RelayInfo relayInfo, ITunnelConnection connection)
        {
            if (OnConnected.TryGetValue(Helper.GlobalString, out List<Action<ITunnelConnection>> callbacks))
            {
                foreach (var item in callbacks)
                {
                    item(connection);
                }
            }
            if (OnConnected.TryGetValue(connection.TransactionId, out callbacks))
            {
                foreach (var callabck in callbacks)
                {
                    callabck(connection);
                }
            }
        }
    }
}
