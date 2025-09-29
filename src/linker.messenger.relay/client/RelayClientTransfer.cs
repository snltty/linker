using linker.messenger.relay.client.transport;
using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using linker.messenger.signin;
using System.Collections.Concurrent;

namespace linker.messenger.relay.client
{
    /// <summary>
    /// 中继
    /// </summary>
    public sealed class RelayClientTransfer
    {
        public List<IRelayClientTransport> Transports { get; private set; }


        public ConcurrentDictionary<string, bool> Operating => operating.StringKeyValue;
        private OperatingMultipleManager operating = new OperatingMultipleManager();

        private Dictionary<string, List<Action<ITunnelConnection>>> OnConnected { get; } = new Dictionary<string, List<Action<ITunnelConnection>>>();

        private readonly IRelayClientStore relayClientStore;
        private readonly ISignInClientStore signInClientStore;
        public RelayClientTransfer(IMessengerSender messengerSender, ISerializer serializer, IRelayClientStore relayClientStore, SignInClientState signInClientState, IMessengerStore messengerStore, ISignInClientStore signInClientStore)
        {
            this.relayClientStore = relayClientStore;
            this.signInClientStore = signInClientStore;
            Transports = new List<IRelayClientTransport> {
                new RelayClientTransportSelfHost(messengerSender,serializer,relayClientStore,signInClientState,messengerStore),
                new RelayClientTransportSelfHostUdp(messengerSender,serializer,relayClientStore,signInClientState,messengerStore),
            };
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Info($"load relay transport:{string.Join(",", Transports.Select(c => c.GetType().Name))}");
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

        public async Task<ITunnelConnection> ConnectAsync(string fromMachineId, string remoteMachineId, string transactionId, string nodeId, TunnelProtocolType protocol)
        {
            return await ConnectAsync(fromMachineId, remoteMachineId, transactionId, TunnelProtocolType.All & (~protocol), nodeId, protocol).ConfigureAwait(false);
        }
        /// <summary>
        /// 中继连接对方
        /// </summary>
        /// <param name="fromMachineId">自己的id</param>
        /// <param name="remoteMachineId">对方id</param>
        /// <param name="transactionId">事务</param>
        /// <returns></returns>
        public async Task<ITunnelConnection> ConnectAsync(string fromMachineId, string remoteMachineId, string transactionId, TunnelProtocolType denyProtocols, string nodeId = "", TunnelProtocolType protocol = TunnelProtocolType.None)
        {
            if (string.IsNullOrWhiteSpace(nodeId)) nodeId = relayClientStore.DefaultNodeId;
            if(protocol == TunnelProtocolType.None) protocol = relayClientStore.DefaultProtocol;


            if (operating.StartOperation(BuildKey(remoteMachineId, transactionId)) == false)
            {
                return null;
            }
            try
            {
                if (relayClientStore.Server.Disabled)
                {
                    return null;
                }

                IEnumerable<IRelayClientTransport> transports = Transports
                    //优先的
                    .Where(c => c.ProtocolType == protocol)
                    //其次的
                    .Concat(Transports.Where(c => c.ProtocolType != protocol))
                    //不包含在禁用列表里的
                    .Where(c => (denyProtocols & c.ProtocolType)!= c.ProtocolType);

                foreach (IRelayClientTransport transport in transports)
                {
                    if (transport == null)
                    {
                        continue;
                    }

                    transport.RelayInfo170 relayInfo = new transport.RelayInfo170
                    {
                        FlowingId = 0,
                        FromMachineId = fromMachineId,
                        FromMachineName = string.Empty,
                        RemoteMachineId = remoteMachineId,
                        RemoteMachineName = string.Empty,
                        TransactionId = transactionId,
                        TransportName = transport.Name,
                        SSL = relayClientStore.Server.SSL,
                        NodeId = nodeId,
                        UserId = signInClientStore.Server.UserId,
                        UseCdkey = relayClientStore.Server.UseCdkey,
                    };

                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Info($"relay {transport.Name} to {relayInfo.RemoteMachineId}->{relayInfo.RemoteMachineName} {relayInfo.ToJson()}");
                    ITunnelConnection connection = await transport.RelayAsync(relayInfo).ConfigureAwait(false);
                    if (connection != null)
                    {
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            LoggerHelper.Instance.Debug($"relay {transport.Name} to {relayInfo.RemoteMachineId}->{relayInfo.RemoteMachineName} success,{relayInfo.ToJson()}");
                        ConnectedCallback(relayInfo, connection);
                        return connection;
                    }
                    else
                    {
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            LoggerHelper.Instance.Error($"relay {transport.Name} to {relayInfo.RemoteMachineId}->{relayInfo.RemoteMachineName} fail,{relayInfo.ToJson()}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            finally
            {
                operating.StopOperation(BuildKey(remoteMachineId, transactionId));
            }
            return null;
        }
        /// <summary>
        /// 收到对方的中继请求
        /// </summary>
        /// <param name="relayInfo"></param>
        /// <returns></returns>
        public async Task<bool> OnBeginAsync(transport.RelayInfo170 relayInfo)
        {
            if (operating.StartOperation(BuildKey(relayInfo.FromMachineId, relayInfo.TransactionId)) == false)
            {
                return false;
            }

            try
            {
                IRelayClientTransport _transports = Transports.FirstOrDefault(c => c.Name == relayInfo.TransportName);
                if (_transports == null) return false;

                await _transports.OnBeginAsync(relayInfo, (connection) =>
                {
                    if (connection != null)
                    {
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            LoggerHelper.Instance.Debug($"relay from {relayInfo.RemoteMachineId}->{relayInfo.RemoteMachineName} success,{relayInfo.ToJson()}");
                        ConnectedCallback(relayInfo, connection);
                    }
                    else
                    {
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            LoggerHelper.Instance.Error($"relay from {relayInfo.RemoteMachineId}->{relayInfo.RemoteMachineName} error,{relayInfo.ToJson()}");
                    }
                }).ConfigureAwait(false);
                return true;
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
                operating.StopOperation(BuildKey(relayInfo.FromMachineId, relayInfo.TransactionId));
            }
            return false;
        }

        /// <summary>
        /// 回调
        /// </summary>
        /// <param name="relayInfo"></param>
        /// <param name="connection"></param>
        private void ConnectedCallback(transport.RelayInfo170 relayInfo, ITunnelConnection connection)
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



        private string BuildKey(string remoteMachineId, string transactionId)
        {
            return $"{remoteMachineId}@{transactionId}";
        }
    }
}