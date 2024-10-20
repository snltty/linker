﻿using linker.client.config;
using linker.config;
using linker.plugins.relay.transport;
using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Net;
using linker.plugins.client;

namespace linker.plugins.relay
{
    /// <summary>
    /// 中继
    /// </summary>
    public sealed partial class RelayTransfer
    {
        private List<ITransport> transports;

        private readonly FileConfig fileConfig;
        private readonly RunningConfig running;
        private readonly ServiceProvider serviceProvider;

        private ConcurrentDictionary<string, bool> connectingDic = new ConcurrentDictionary<string, bool>();
        private Dictionary<string, List<Action<ITunnelConnection>>> OnConnected { get; } = new Dictionary<string, List<Action<ITunnelConnection>>>();

        public RelayTransfer(FileConfig fileConfig, ClientSignInState clientSignInState, RunningConfig running, ServiceProvider serviceProvider)
        {
            this.fileConfig = fileConfig;
            this.running = running;
            this.serviceProvider = serviceProvider;
            InitConfig();
            TestTask();

            IEnumerable<Type> types = GetSourceGeneratorTypes();
            transports = types.Select(c => (ITransport)serviceProvider.GetService(c)).Where(c => c != null).Where(c => string.IsNullOrWhiteSpace(c.Name) == false).ToList();
            LoggerHelper.Instance.Info($"load relay transport:{string.Join(",", transports.Select(c => c.Name))}");
        }
        private void InitConfig()
        {
            if (fileConfig.Data.Client.Relay.Servers.Length == 0)
            {
                fileConfig.Data.Client.Relay.Servers = new RelayServerInfo[]
                {
                     new RelayServerInfo{
                         Name="Linker",
                         RelayType= RelayType.Linker,
                         Disabled = false,
                         Host = fileConfig.Data.Client.ServerInfo.Host,
                     }
                };
                fileConfig.Data.Update();
            }
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
                var servers = GetServers();
                foreach (RelayServerInfo item in servers)
                {
                    ITransport transport = transports.FirstOrDefault(c => c.Type == item.RelayType);
                    if (transport == null)
                    {
                        continue;
                    }

                    IPEndPoint server = NetworkHelper.GetEndPoint(item.Host, 3478);
                    transport.RelayInfo relayInfo = new transport.RelayInfo
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
                        SSL = item.SSL,
                        ServerName = item.Name
                    };

                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Info($"relay to {relayInfo.RemoteMachineId}->{relayInfo.RemoteMachineName} {relayInfo.ToJson()}");
                    ITunnelConnection connection = await transport.RelayAsync(relayInfo).ConfigureAwait(false);
                    if (connection != null)
                    {
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            LoggerHelper.Instance.Debug($"relay to {relayInfo.RemoteMachineId}->{relayInfo.RemoteMachineName} success,{relayInfo.ToJson()}");
                        ConnectedCallback(relayInfo, connection);
                        return connection;
                    }
                    else
                    {
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
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
        public async Task<bool> OnBeginAsync(transport.RelayInfo relayInfo)
        {
            if (connectingDic.TryAdd(relayInfo.FromMachineId, true) == false)
            {
                return false;
            }

            try
            {
                RelayServerInfo server = GetServer(relayInfo.ServerName);
                if (server == null) return false;

                relayInfo.Server = NetworkHelper.GetEndPoint(server.Host, 3478);

                ITransport _transports = transports.FirstOrDefault(c => c.Name == relayInfo.TransportName);
                if (_transports == null) return false;

                await _transports.OnBeginAsync(relayInfo, (ITunnelConnection connection) =>
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
                connectingDic.TryRemove(relayInfo.FromMachineId, out _);
            }
            return false;
        }

        /// <summary>
        /// 回调
        /// </summary>
        /// <param name="relayInfo"></param>
        /// <param name="connection"></param>
        private void ConnectedCallback(transport.RelayInfo relayInfo, ITunnelConnection connection)
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


        private IEnumerable<RelayServerInfo> GetServers()
        {
            return fileConfig.Data.Client.Relay.Servers
                    .Where(c => c.Disabled == false)
                    .Where(c => string.IsNullOrWhiteSpace(c.Host) == false);
        }
        private RelayServerInfo GetServer(string name)
        {
            if (fileConfig.Data.Client.HasAccess(ClientApiAccess.Config))
            {
                return fileConfig.Data.Client.Relay.Servers.FirstOrDefault(c => c.Name == name && c.Disabled == false && string.IsNullOrWhiteSpace(c.Host) == false)
                    ?? fileConfig.Data.Client.Relay.Servers.FirstOrDefault(c => c.Disabled == false && string.IsNullOrWhiteSpace(c.Host) == false);
            }
            else
            {
                return fileConfig.Data.Client.Relay.Servers.FirstOrDefault(c => c.Name == name && string.IsNullOrWhiteSpace(c.Host) == false)
                    ?? fileConfig.Data.Client.Relay.Servers.FirstOrDefault(c => string.IsNullOrWhiteSpace(c.Host) == false);
            }
        }



        private readonly LastTicksManager lastTicksManager = new LastTicksManager();
        public void SubscribeDelayTest()
        {
            lastTicksManager.Update();
        }
        private async Task TaskRelay()
        {
            try
            {
                foreach (var server in fileConfig.Data.Client.Relay.Servers)
                {
                    ITransport transport = transports.FirstOrDefault(d => d.Type == server.RelayType);
                    if (transport == null) continue;

                    IPEndPoint serverEP = NetworkHelper.GetEndPoint(server.Host, 3478);
                    RelayTestResultInfo result = await transport.RelayTestAsync(new RelayTestInfo
                    {
                        MachineId = fileConfig.Data.Client.Id,
                        SecretKey = server.SecretKey,
                        Server = serverEP,
                    });
                    server.Delay = result.Delay;
                    server.Available = result.Available;
                }
            }
            catch (Exception)
            {
            }
        }
        private void TestTask()
        {
            TimerHelper.SetInterval(async () =>
            {
                if (lastTicksManager.DiffLessEqual(3000))
                {
                    await TaskRelay();
                }
                return true;
            }, () => lastTicksManager.DiffLessEqual(3000) ? 3000 : 30000);
        }
        sealed class TestInfo
        {
            public RelayServerInfo Server { get; set; }
            public Task<RelayTestResultInfo> Task { get; set; }
        }

        public void SetServers(RelayServerInfo[] servers)
        {
            fileConfig.Data.Client.Relay.Servers = servers;
            fileConfig.Data.Update();
        }
    }
}