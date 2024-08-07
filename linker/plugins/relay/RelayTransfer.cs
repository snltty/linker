﻿using linker.client.config;
using linker.config;
using linker.plugins.relay.transport;
using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using MemoryPack;
using linker.plugins.client;

namespace linker.plugins.relay
{
    /// <summary>
    /// 中继
    /// </summary>
    public sealed class RelayTransfer
    {
        private List<ITransport> transports;

        private readonly FileConfig fileConfig;
        private readonly RunningConfig running;
        private readonly ServiceProvider serviceProvider;
        private readonly RunningConfigTransfer runningConfigTransfer;
        private string configKey = "relayServers";

        private ConcurrentDictionary<string, bool> connectingDic = new ConcurrentDictionary<string, bool>();
        private Dictionary<string, List<Action<ITunnelConnection>>> OnConnected { get; } = new Dictionary<string, List<Action<ITunnelConnection>>>();

        public RelayTransfer(FileConfig fileConfig, ClientSignInState clientSignInState, RunningConfig running, ServiceProvider serviceProvider, RunningConfigTransfer runningConfigTransfer)
        {
            this.fileConfig = fileConfig;
            this.running = running;
            this.serviceProvider = serviceProvider;
            this.runningConfigTransfer = runningConfigTransfer;
            InitConfig();
            TestTask();

            runningConfigTransfer.Setter(configKey, SetServers);
            runningConfigTransfer.Getter(configKey, () => MemoryPackSerializer.Serialize(running.Data.Relay.Servers));
            clientSignInState.NetworkFirstEnabledHandle += () =>
            {
                SyncServers();
            };
        }
        private void InitConfig()
        {
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
        public void OnServers(RelayRunningSyncInfo info)
        {
            running.Data.Relay.Servers = info.Servers;
            running.Data.Relay.ByRelay = info.ByRelay;
            running.Data.Update();
            runningConfigTransfer.IncrementVersion(configKey);
            SyncServers();
            _ = TaskRelay();
        }
        private void SetServers(Memory<byte> data)
        {
            RelayRunningSyncInfo relayRunningSyncInfo = MemoryPackSerializer.Deserialize<RelayRunningSyncInfo>(data.Span);
            running.Data.Relay.Servers = relayRunningSyncInfo.Servers;
            running.Data.Relay.ByRelay = relayRunningSyncInfo.ByRelay;
            running.Data.Update();
            _ = TaskRelay();
        }
        private void SyncServers()
        {
            runningConfigTransfer.Sync(configKey, MemoryPackSerializer.Serialize(new RelayRunningSyncInfo
            {
                Servers = running.Data.Relay.Servers,
                ByRelay = running.Data.Relay.ByRelay,
            }));
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
                var servers = running.Data.Relay.Servers
                    .Where(c => c.Disabled == false)
                    .Where(c => string.IsNullOrWhiteSpace(c.Host) == false)
                    .Where(c => c.Delay >= 0).OrderBy(c => c.Delay);
                foreach (RelayServerInfo item in servers)
                {
                    ITransport transport = transports.FirstOrDefault(c => c.Type == item.RelayType);
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
                    await _transports.OnBeginAsync(relayInfo, (ITunnelConnection connection) =>
                    {
                        if (connection != null)
                        {
                            LoggerHelper.Instance.Debug($"relay from {relayInfo.RemoteMachineId}->{relayInfo.RemoteMachineName} success,{relayInfo.ToJson()}");
                            ConnectedCallback(relayInfo, connection);
                        }
                        else
                        {
                            LoggerHelper.Instance.Error($"relay from {relayInfo.RemoteMachineId}->{relayInfo.RemoteMachineName} error,{relayInfo.ToJson()}");
                        }
                    }).ConfigureAwait(false);
                    return true;
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


        private async Task TaskRelay()
        {
            try
            {
                var tasks = running.Data.Relay.Servers.Select(c =>
                {
                    try
                    {
                        ITransport transport = transports.FirstOrDefault(d => d.Type == c.RelayType);
                        if (transport == null) return null;

                        IPEndPoint server = NetworkHelper.GetEndPoint(c.Host, 3478);

                        return new TestInfo
                        {
                            Server = c,
                            Task = transport.RelayTestAsync(new RelayTestInfo
                            {
                                MachineId = fileConfig.Data.Client.Id,
                                SecretKey = c.SecretKey,
                                Server = server,
                            })
                        };
                    }
                    catch (Exception)
                    {
                    }
                    return null;
                });

                try
                {
                    await Task.WhenAll(tasks.Where(c => c != null).Select(c => c.Task)).WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);
                    foreach (var item in tasks.Where(c => c != null))
                    {
                        item.Server.Delay = item.Task.Result.Delay;
                        item.Server.Available = item.Task.Result.Available;
                    }
                }
                catch (Exception)
                {
                }
            }
            catch (Exception)
            {
            }
        }
        private void TestTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await TaskRelay();
                    await Task.Delay(5000);
                }

            });
        }
        sealed class TestInfo
        {
            public RelayServerInfo Server { get; set; }
            public Task<RelayTestResultInfo> Task { get; set; }
        }
    }
}