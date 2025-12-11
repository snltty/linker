using linker.libs;
using linker.libs.extends;
using linker.libs.timer;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.node
{
    public class NodeReportTransfer<TConfig, TStore, TReport>
        where TConfig : class, INodeConfigBase, new()
        where TStore : class, INodeStoreBase, new()
        where TReport : class, INodeReportBase, new()
    {
        public TConfig Config => nodeConfigStore.Config;

        public int ConnectionNum => connectionNum;

        public virtual ushort MessengerIdSahre { get; }
        public virtual ushort MessengerIdUpdateForward { get; }
        public virtual ushort MessengerIdUpgradeForward { get; }
        public virtual ushort MessengerIdExitForward { get; }
        public virtual ushort MessengerIdReport { get; }
        public virtual ushort MessengerIdSignIn { get; }
        public virtual ushort MessengerIdMastersForward { get; }
        public virtual ushort MessengerIdDenysForward { get; }
        public virtual ushort MessengerIdDenysAddForward { get; }
        public virtual ushort MessengerIdDenysDelForward { get; }


        private int connectionNum = 0;
        private ulong bytes = 0;
        private ulong lastBytes = 0;
        private readonly string md5 = string.Empty;

        private readonly ICrypto crypto = CryptoFactory.CreateSymmetric(Helper.GlobalString);

        private readonly NodeConnectionTransfer nodeConnectionTransfer;
        private readonly INodeConfigStore<TConfig> nodeConfigStore;
        private readonly ISerializer serializer;
        private readonly IMessengerSender messengerSender;
        private readonly INodeStore<TStore, TReport> nodeStore;
        private readonly IMessengerResolver messengerResolver;
        private readonly INodeMasterDenyStore nodeMasterDenyStore;

        public NodeReportTransfer(NodeConnectionTransfer nodeConnectionTransfer, INodeConfigStore<TConfig> nodeConfigStore,
            ISerializer serializer, IMessengerSender messengerSender, INodeStore<TStore, TReport> nodeStore,
            IMessengerResolver messengerResolver, ICommonStore commonStore, INodeMasterDenyStore nodeMasterDenyStore)
        {
            this.nodeConnectionTransfer = nodeConnectionTransfer;
            this.nodeConfigStore = nodeConfigStore;
            this.serializer = serializer;
            this.messengerSender = messengerSender;
            this.nodeStore = nodeStore;
            this.messengerResolver = messengerResolver;
            this.nodeMasterDenyStore = nodeMasterDenyStore;

            md5 = Config.NodeId.Md5();

            if (commonStore.Modes.HasFlag(CommonModes.Server))
            {
                _ = ReportTask();
                SignInTask();
            }
        }

        public void IncrementConnectionNum()
        {
            Interlocked.Increment(ref connectionNum);
        }
        public void DecrementConnectionNum()
        {
            Interlocked.Decrement(ref connectionNum);
        }
        public void AddBytes(long length)
        {
            Interlocked.Add(ref bytes, (ulong)length);
        }

        public async Task<bool> Report(string id, string name, string host)
        {
            return await nodeStore.Add(new TStore
            {
                NodeId = id,
                Name = name,
                Host = host
            }).ConfigureAwait(false);
        }
        public async Task<bool> Report(TReport info)
        {
            if (nodeConnectionTransfer.TryGet(ConnectionSideType.Node, info.NodeId, out _) == false) return false;

            return await nodeStore.Report(info).ConfigureAwait(false);
        }
        public async Task<bool> SignIn(string serverId, string shareKey, IConnection connection)
        {
            //未被配置，或默认配置的，设它为管理端
            if (string.IsNullOrWhiteSpace(Config.MasterKey) || md5 == Config.MasterKey)
            {
                nodeConfigStore.SetMasterKey(serverId.Md5());
                nodeConfigStore.Confirm();
            }
            if (shareKey != Config.ShareKey && serverId.Md5() != Config.MasterKey)
            {
                return false;
            }
            if (await nodeMasterDenyStore.Get(NetworkHelper.ToValue(connection.Address.Address), 0).ConfigureAwait(false))
            {
                return false;
            }

            connection.Id = serverId;
            nodeConnectionTransfer.TryAdd(ConnectionSideType.Master, connection.Id, connection);

            return true;
        }


        public async Task<string> GetShareKeyForward(string nodeId)
        {
            TStore store = await nodeStore.GetByNodeId(nodeId);

            if (store != null && store.Manageable && nodeConnectionTransfer.TryGet(ConnectionSideType.Node, nodeId, out var connection))
            {
                var resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = MessengerIdSahre,
                    Payload = serializer.Serialize(store.MasterKey)
                }).ConfigureAwait(false);
                if (resp.Code == MessageResponeCodes.OK)
                {
                    return serializer.Deserialize<string>(resp.Data.Span);
                }
            }
            return string.Empty;
        }
        public async Task<string> GetShareKey(string masterKey)
        {
            if (masterKey != Config.MasterKey) return string.Empty;
            return Config.ShareKey;
        }
        public async Task<string> Import(string shareKey)
        {
            try
            {
                NodeShareInfo info = serializer.Deserialize<NodeShareInfo>(crypto.Decode(Convert.FromBase64String(shareKey)).Span);

                bool result = await nodeStore.Add(new TStore
                {
                    NodeId = info.NodeId,
                    Host = info.Host,
                    Name = info.Name,
                    LastTicks = Environment.TickCount64,
                    ShareKey = shareKey
                }).ConfigureAwait(false);
                if (result == false)
                {
                    return "node already exists";
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return string.Empty;
        }
        public async Task<bool> Remove(string nodeId)
        {
            if (nodeId == Config.NodeId) return false;

            return await nodeStore.Delete(nodeId).ConfigureAwait(false);
        }
        public async Task<bool> UpdateForward(TStore info)
        {
            TStore store = await nodeStore.GetByNodeId(info.NodeId);

            if (store != null && store.Manageable && nodeConnectionTransfer.TryGet(ConnectionSideType.Node, info.NodeId, out var connection))
            {
                info.MasterKey = store.MasterKey;
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = MessengerIdUpdateForward,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
            }

            return await nodeStore.Update(info).ConfigureAwait(false);
        }
        public virtual async Task<bool> Update(TStore info)
        {
            return false;
        }
        public async Task<bool> UpgradeForward(string nodeId, string version)
        {
            TStore store = await nodeStore.GetByNodeId(nodeId);

            if (store != null && store.Manageable && nodeConnectionTransfer.TryGet(ConnectionSideType.Node, nodeId, out var connection))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = MessengerIdUpgradeForward,
                    Payload = serializer.Serialize(new KeyValuePair<string, string>(store.MasterKey, version))
                }).ConfigureAwait(false);
                return true;
            }

            return false;
        }
        public async Task<bool> Upgrade(string masterKey, string version)
        {
            if (masterKey != Config.MasterKey) return false;

            Helper.AppUpdate(version);

            return true;
        }
        public async Task<bool> ExitForward(string nodeId)
        {
            TStore store = await nodeStore.GetByNodeId(nodeId);
            if (store != null && store.Manageable && nodeConnectionTransfer.TryGet(ConnectionSideType.Node, nodeId, out var connection))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = MessengerIdExitForward,
                    Payload = serializer.Serialize(new KeyValuePair<string, string>(store.MasterKey, nodeId))
                }).ConfigureAwait(false);
                return true;
            }

            return false;
        }
        public async Task<bool> Exit(string masterKey)
        {
            if (masterKey != Config.MasterKey) return false;

            Helper.AppExit(-1);

            return true;
        }


        public async Task<MastersResponseInfo> MastersForward(MastersRequestInfo info)
        {
            TStore store = await nodeStore.GetByNodeId(info.NodeId);
            if (store != null && store.Manageable && nodeConnectionTransfer.TryGet(ConnectionSideType.Node, info.NodeId, out var connection))
            {
                info.NodeId = store.MasterKey;
                var resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = MessengerIdMastersForward,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
                if (resp.Code == MessageResponeCodes.OK)
                {
                    return serializer.Deserialize<MastersResponseInfo>(resp.Data.Span);
                }
            }

            return new MastersResponseInfo();
        }
        public async Task<MastersResponseInfo> Masters(MastersRequestInfo info)
        {
            if (info.NodeId != Config.MasterKey)
            {
                return new MastersResponseInfo();
            }

            var connections = nodeConnectionTransfer.Get(ConnectionSideType.Master);
            return new MastersResponseInfo
            {
                Page = info.Page,
                Sise = info.Sise,
                Count = connections.Count,
                List = connections.Skip((info.Page - 1) * info.Sise).Take(info.Sise).Select(c => new MasterConnInfo { Addr = c.Address, NodeId = c.Id }).ToList()
            };
        }
        public async Task<MasterDenyStoreResponseInfo> DenysForward(MasterDenyStoreRequestInfo info)
        {
            TStore store = await nodeStore.GetByNodeId(info.NodeId);
            if (store != null && store.Manageable && nodeConnectionTransfer.TryGet(ConnectionSideType.Node, info.NodeId, out var connection))
            {
                info.NodeId = store.MasterKey;
                var resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = MessengerIdDenysForward,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
                if (resp.Code == MessageResponeCodes.OK)
                {
                    return serializer.Deserialize<MasterDenyStoreResponseInfo>(resp.Data.Span);
                }
            }

            return new MasterDenyStoreResponseInfo();
        }
        public async Task<MasterDenyStoreResponseInfo> Denys(MasterDenyStoreRequestInfo info)
        {
            if (info.NodeId != Config.MasterKey)
            {
                return new MasterDenyStoreResponseInfo();
            }
            return await nodeMasterDenyStore.Get(info).ConfigureAwait(false);
        }
        public async Task<bool> DenysAddForward(MasterDenyAddInfo info)
        {
            TStore store = await nodeStore.GetByNodeId(info.NodeId);
            if (store != null && store.Manageable && nodeConnectionTransfer.TryGet(ConnectionSideType.Node, info.NodeId, out var connection))
            {
                info.NodeId = store.MasterKey;
                var resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = MessengerIdDenysAddForward,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
                return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
            }

            return false;
        }
        public async Task<bool> DenysAdd(MasterDenyAddInfo info)
        {
            if (info.NodeId != Config.MasterKey)
            {
                return false;
            }
            return await nodeMasterDenyStore.Add(info).ConfigureAwait(false);
        }
        public async Task<bool> DenysDelForward(MasterDenyDelInfo info)
        {
            TStore store = await nodeStore.GetByNodeId(info.NodeId);
            if (store != null && store.Manageable && nodeConnectionTransfer.TryGet(ConnectionSideType.Node, info.NodeId, out var connection))
            {
                info.NodeId = store.MasterKey;
                var resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = MessengerIdDenysDelForward,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
                return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
            }

            return false;
        }
        public async Task<bool> DenysDel(MasterDenyDelInfo info)
        {
            if (info.NodeId != Config.MasterKey)
            {
                return false;
            }
            return await nodeMasterDenyStore.Delete(info).ConfigureAwait(false);
        }


        public virtual async Task<List<TStore>> GetNodes(bool super, string userid, string machineId)
        {
            return [];
        }
        public async Task<TStore> GetNode(string nodeId)
        {
            TStore node = await nodeStore.GetByNodeId(nodeId).ConfigureAwait(false);
            if (node == null || Environment.TickCount64 - node.LastTicks > 15000)
            {
                return null;
            }
            return node;
        }


        protected virtual void BuildReport(TReport info)
        {

        }
        private async Task BuildShareKey()
        {
            try
            {
                string host = Config.Host;
                if (string.IsNullOrWhiteSpace(host))
                {
                    using HttpClient httpClient = new HttpClient();
                    host = await httpClient.GetStringAsync($"https://ifconfig.me/ip").WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);
                }

                NodeShareInfo shareKeyInfo = new NodeShareInfo
                {
                    NodeId = Config.NodeId,
                    Host = $"{host}:{nodeConfigStore.ServicePort}",
                    Name = Config.Name,
                    SystemId = SystemIdHelper.GetSystemId().Md5()
                };
                string shareKey = Convert.ToBase64String(crypto.Encode(serializer.Serialize(shareKeyInfo)));
                nodeConfigStore.SetShareKey(shareKey);
                nodeConfigStore.Confirm();

                host = $"{IPAddress.Loopback}:{nodeConfigStore.ServicePort}";
                var node = await nodeStore.GetByNodeId(nodeConfigStore.Config.NodeId);
                if (node == null || node.ShareKey != shareKey || node.Name != Config.Name || node.Host != host)
                {
                    await nodeStore.Delete(nodeConfigStore.Config.NodeId);
                    await nodeStore.Add(new TStore
                    {
                        NodeId = nodeConfigStore.Config.NodeId,
                        Name = "default",
                        Host = $"{IPAddress.Loopback}:{nodeConfigStore.ServicePort}",
                        ShareKey = shareKey
                    }).ConfigureAwait(false);
                }

                LoggerHelper.Instance.Warning($"build SForward share key : {shareKey}");
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error($"build SForward share key error : {ex}");
            }
        }
        private async Task ReportTask()
        {
            await BuildShareKey().ConfigureAwait(false);

            TimerHelper.SetIntervalLong(async () =>
            {
                try
                {
                    var connections = nodeConnectionTransfer.Get(ConnectionSideType.Master);
                    if (connections.Count != 0)
                    {
                        double diff = (bytes - lastBytes) * 8 / 1024.0 / 1024.0;
                        lastBytes = bytes;

                        var config = nodeConfigStore.Config;
                        TReport info = new TReport
                        {
                            Bandwidth = config.Bandwidth,
                            Connections = config.Connections,
                            DataEachMonth = config.DataEachMonth,
                            DataRemain = config.DataRemain,
                            Logo = config.Logo,
                            Name = config.Name,
                            NodeId = config.NodeId,
                            Url = config.Url,
                            ConnectionsRatio = connectionNum,
                            BandwidthRatio = Math.Round(diff / 5, 2),
                            Version = VersionHelper.Version,
                            MasterCount = connections.Count,
                            MasterKey = config.MasterKey
                        };
                        BuildReport(info);
                        byte[] memory = serializer.Serialize(info);
                        var tasks = connections.Select(c => messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = c,
                            MessengerId = MessengerIdReport,
                            Payload = memory,
                            Timeout = 5000
                        })).ToList();
                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error($"SForward report : {ex}");
                    }
                }
            }, 5000);
        }

        private void SignInTask()
        {
            TimerHelper.SetIntervalLong(async () =>
            {
                try
                {
                    var nodes = (await nodeStore.GetAll()).Where(c =>
                    {
                        return nodeConnectionTransfer.TryGet(ConnectionSideType.Node, c.NodeId, out IConnection connection) == false || connection == null || connection.Connected == false;
                    }).ToList();
                    if (nodes.Count != 0)
                    {
                        var tasks = nodes.Select(async c =>
                        {
                            IPEndPoint remote = await NetworkHelper.GetEndPointAsync(c.Host, 1802).ConfigureAwait(false);
                            Socket socket = new Socket(remote.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                            socket.KeepAlive();
                            await socket.ConnectAsync(remote).WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);
                            var connection = await messengerResolver.BeginReceiveClient(socket, true, (byte)ResolverType.NodeConnection, Helper.EmptyArray).ConfigureAwait(false);

                            var resp = await messengerSender.SendReply(new MessageRequestWrap
                            {
                                Connection = connection,
                                MessengerId = MessengerIdSignIn,
                                Payload = serializer.Serialize(new KeyValuePair<string, string>(Config.NodeId, c.ShareKey)),
                                Timeout = 5000
                            }).ConfigureAwait(false);
                            if (resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray))
                            {
                                LoggerHelper.Instance.Debug($"SForward sign in to node {c.NodeId} success");
                                nodeConnectionTransfer.TryAdd(ConnectionSideType.Node, c.NodeId, connection);
                            }
                            else
                            {
                                connection?.Disponse();
                            }

                        });
                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error($"SForward sign in : {ex}");
                    }
                }
            }, 10000);
        }
    }
}
