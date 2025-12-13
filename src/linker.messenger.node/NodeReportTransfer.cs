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
        public virtual ushort MessengerIdUpdate { get; }
        public virtual ushort MessengerIdUpgrade { get; }
        public virtual ushort MessengerIdExit { get; }
        public virtual ushort MessengerIdReport { get; }
        public virtual ushort MessengerIdSignIn { get; }
        public virtual ushort MessengerIdMasters { get; }
        public virtual ushort MessengerIdDenys { get; }
        public virtual ushort MessengerIdDenysAdd { get; }
        public virtual ushort MessengerIdDenysDel { get; }


        protected virtual string Name { get; }


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
        public async Task<bool> Report(IConnection conn, TReport info)
        {
            if (nodeConnectionTransfer.TryGet(ConnectionSideType.Node, conn.Id, out _) == false || conn.Id != info.NodeId)
            {
                return false;
            }

            if (info.Version != VersionHelper.Version)
            {
                _ = UpgradeForward(info.NodeId, VersionHelper.Version);
            }

            return await nodeStore.Report(info).ConfigureAwait(false);
        }
        public async Task<bool> SignIn(string serverId, string masterKey, string shareKey, IConnection connection)
        {
            bool result = connection.Address.Address.Equals(IPAddress.Loopback)
                || masterKey == Config.MasterKey
                || ((shareKey == Config.ShareKey || shareKey == Config.ShareKeyManager) && await nodeMasterDenyStore.Get(NetworkHelper.ToValue(connection.Address.Address), 0).ConfigureAwait(false) == false);
            if (result == false)
            {
                return false;
            }

            connection.Id = serverId;
            nodeConnectionTransfer.TryAdd(ConnectionSideType.Master, connection.Id, new ConnectionInfo
            {
                Connection = connection,
                Manageable = masterKey == Config.MasterKey
            });

            return true;
        }


        public async Task<string> GetShareKeyForward(string nodeId)
        {
            TStore store = await nodeStore.GetByNodeId(nodeId);
            if (store == null || nodeConnectionTransfer.TryGet(ConnectionSideType.Node, nodeId, out var connection) == false || connection.Manageable == false)
            {
                return string.Empty;
            }

            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection.Connection,
                MessengerId = MessengerIdSahre
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<string>(resp.Data.Span);
            }
            return string.Empty;
        }
        public async Task<string> GetShareKey(IConnection conn)
        {
            if (nodeConnectionTransfer.TryGet(ConnectionSideType.Master, conn.Id, out var connection) == false || connection.Manageable == false) return string.Empty;
            return Config.ShareKey;
        }
        public async Task<string> Import(string shareKey)
        {
            try
            {
                if (shareKey.Contains("\\u"))
                {
                    shareKey = System.Text.RegularExpressions.Regex.Unescape(shareKey);
                }

                NodeShareInfo info = serializer.Deserialize<NodeShareInfo>(crypto.Decode(Convert.FromBase64String(shareKey)).Span);

                bool result = await nodeStore.Add(new TStore
                {
                    NodeId = info.NodeId,
                    Host = info.Host,
                    Name = info.Name,
                    LastTicks = Environment.TickCount64,
                    ShareKey = shareKey,
                    MasterKey = info.MasterKey,
                    Manageable = string.IsNullOrWhiteSpace(info.MasterKey) == false
                }).ConfigureAwait(false);
                if (result == false)
                {
                    return $"{Name} node already exists";
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
            return await nodeStore.Delete(nodeId).ConfigureAwait(false);
        }
        public async Task<bool> UpdateForward(TStore info)
        {
            TStore store = await nodeStore.GetByNodeId(info.NodeId);

            if (store != null && nodeConnectionTransfer.TryGet(ConnectionSideType.Node, info.NodeId, out var connection) && connection.Manageable)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = connection.Connection,
                    MessengerId = MessengerIdUpdate,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
            }
            return await nodeStore.Update(info).ConfigureAwait(false);
        }
        public virtual async Task<bool> Update(IConnection conn, TStore info)
        {
            return false;
        }
        public async Task<bool> UpgradeForward(string nodeId, string version)
        {
            TStore store = await nodeStore.GetByNodeId(nodeId);

            if (store == null || nodeConnectionTransfer.TryGet(ConnectionSideType.Node, nodeId, out var connection) == false || connection.Manageable == false)
            {
                return false;
            }
            return await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = connection.Connection,
                MessengerId = MessengerIdUpgrade,
                Payload = serializer.Serialize(version)
            }).ConfigureAwait(false);
        }
        public async Task<bool> Upgrade(IConnection conn, string version)
        {
            if (nodeConnectionTransfer.TryGet(ConnectionSideType.Master, conn.Id, out var connection) == false || connection.Manageable == false)
            {
                return false;
            }

            Helper.AppUpdate(version);

            return true;
        }
        public async Task<bool> ExitForward(string nodeId)
        {
            TStore store = await nodeStore.GetByNodeId(nodeId);
            if (store == null || nodeConnectionTransfer.TryGet(ConnectionSideType.Node, nodeId, out var connection) == false || connection.Manageable == false)
            {
                return false;
            }

            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = connection.Connection,
                MessengerId = MessengerIdExit
            }).ConfigureAwait(false);
            return true;
        }
        public async Task<bool> Exit(IConnection conn)
        {
            if (nodeConnectionTransfer.TryGet(ConnectionSideType.Master, conn.Id, out var connection) == false || connection.Manageable == false) return false;

            Helper.AppExit(-1);

            return true;
        }


        public async Task<MastersResponseInfo> MastersForward(MastersRequestInfo info)
        {
            TStore store = await nodeStore.GetByNodeId(info.NodeId);
            if (store == null || nodeConnectionTransfer.TryGet(ConnectionSideType.Node, info.NodeId, out var connection) == false || connection.Manageable == false)
            {
                return new MastersResponseInfo();
            }

            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection.Connection,
                MessengerId = MessengerIdMasters,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<MastersResponseInfo>(resp.Data.Span);
            }

            return new MastersResponseInfo();
        }
        public async Task<MastersResponseInfo> Masters(IConnection conn, MastersRequestInfo info)
        {
            if (nodeConnectionTransfer.TryGet(ConnectionSideType.Master, conn.Id, out var connection) == false || connection.Manageable == false)
            {
                return new MastersResponseInfo();
            }

            var connections = nodeConnectionTransfer.Get(ConnectionSideType.Master);
            return new MastersResponseInfo
            {
                Page = info.Page,
                Size = info.Size,
                Count = connections.Count,
                List = connections.Skip((info.Page - 1) * info.Size).Take(info.Size).Select(c => new MasterConnInfo
                {
                    Addr = c.Connection.Address,
                    NodeId = c.Connection.Id
                }).ToList()
            };
        }
        public async Task<MasterDenyStoreResponseInfo> DenysForward(MasterDenyStoreRequestInfo info)
        {
            TStore store = await nodeStore.GetByNodeId(info.NodeId);
            if (store == null || nodeConnectionTransfer.TryGet(ConnectionSideType.Node, info.NodeId, out var connection) == false || connection.Manageable == false)
            {
                return new MasterDenyStoreResponseInfo();
            }

            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection.Connection,
                MessengerId = MessengerIdDenys,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<MasterDenyStoreResponseInfo>(resp.Data.Span);
            }
            return new MasterDenyStoreResponseInfo();
        }
        public async Task<MasterDenyStoreResponseInfo> Denys(IConnection conn, MasterDenyStoreRequestInfo info)
        {
            if (nodeConnectionTransfer.TryGet(ConnectionSideType.Master, conn.Id, out var connection) == false || connection.Manageable == false)
            {
                return new MasterDenyStoreResponseInfo();
            }

            return await nodeMasterDenyStore.Get(info).ConfigureAwait(false);
        }
        public async Task<bool> DenysAddForward(MasterDenyAddInfo info)
        {
            TStore store = await nodeStore.GetByNodeId(info.NodeId);
            if (store == null || nodeConnectionTransfer.TryGet(ConnectionSideType.Node, info.NodeId, out var connection) == false || connection.Manageable == false)
            {
                return false;
            }

            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection.Connection,
                MessengerId = MessengerIdDenysAdd,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
        public async Task<bool> DenysAdd(IConnection conn, MasterDenyAddInfo info)
        {
            if (nodeConnectionTransfer.TryGet(ConnectionSideType.Master, conn.Id, out var connection) == false || connection.Manageable == false)
            {
                return false;
            }

            return await nodeMasterDenyStore.Add(info).ConfigureAwait(false);
        }
        public async Task<bool> DenysDelForward(MasterDenyDelInfo info)
        {
            TStore store = await nodeStore.GetByNodeId(info.NodeId);
            if (store == null || nodeConnectionTransfer.TryGet(ConnectionSideType.Node, info.NodeId, out var connection) == false || connection.Manageable == false)
            {
                return false;

            }
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection.Connection,
                MessengerId = MessengerIdDenysDel,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);

        }
        public async Task<bool> DenysDel(IConnection conn, MasterDenyDelInfo info)
        {
            if (nodeConnectionTransfer.TryGet(ConnectionSideType.Master, conn.Id, out var connection) == false || connection.Manageable == false)
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
                    host = await httpClient.GetStringAsync($"https://linker.snltty.com/ip").WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);
                }

                NodeShareInfo shareKeyInfo = new NodeShareInfo
                {
                    NodeId = Config.NodeId,
                    Host = $"{host}:{nodeConfigStore.ServicePort}",
                    Name = Config.Name
                };
                string shareKey = Convert.ToBase64String(crypto.Encode(serializer.Serialize(shareKeyInfo)));
                nodeConfigStore.SetShareKey(shareKey);

                shareKeyInfo.MasterKey = Config.MasterKey;
                string shareKeyManager = Convert.ToBase64String(crypto.Encode(serializer.Serialize(shareKeyInfo)));
                nodeConfigStore.SetShareKeyManager(shareKeyManager);
                nodeConfigStore.Confirm();

                host = $"{IPAddress.Loopback}:{nodeConfigStore.ServicePort}";
                var node = await nodeStore.GetByNodeId(nodeConfigStore.Config.NodeId);
                if (node == null || node.ShareKey != shareKeyManager || node.Name != Config.Name || node.Host != host)
                {
                    await nodeStore.Delete(nodeConfigStore.Config.NodeId);
                    await nodeStore.Add(new TStore
                    {
                        NodeId = nodeConfigStore.Config.NodeId,
                        Name = "default",
                        Host = $"{IPAddress.Loopback}:{nodeConfigStore.ServicePort}",
                        ShareKey = shareKeyManager,
                        Manageable = true,
                    }).ConfigureAwait(false);
                }

                LoggerHelper.Instance.Warning($"build {Name} share key : {shareKey}");
                LoggerHelper.Instance.Warning($"build {Name} share key manager: {shareKeyManager}");
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error($"build {Name} share key error : {ex}");
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
                            MasterKey = string.Empty
                        };
                        BuildReport(info);
                        byte[] memory = serializer.Serialize(info);
                        var tasks = connections.Select(c => messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = c.Connection,
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
                        LoggerHelper.Instance.Error($"{Name} report : {ex}");
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
                        return nodeConnectionTransfer.TryGet(ConnectionSideType.Node, c.NodeId, out ConnectionInfo connection) == false || connection == null || connection.Connection == null || connection.Connection.Connected == false;
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

                            connection.Id = c.NodeId;
                            var resp = await messengerSender.SendReply(new MessageRequestWrap
                            {
                                Connection = connection,
                                MessengerId = MessengerIdSignIn,
                                Payload = serializer.Serialize(new ValueTuple<string, string, string>(Config.NodeId, c.MasterKey, c.ShareKey)),
                                Timeout = 5000
                            }).ConfigureAwait(false);
                            if (resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray))
                            {
                                LoggerHelper.Instance.Debug($"{Name} sign in to node {c.NodeId} success");
                                nodeConnectionTransfer.TryAdd(ConnectionSideType.Node, c.NodeId, new ConnectionInfo
                                {
                                    Connection = connection,
                                    Manageable = c.Manageable
                                });
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
                        LoggerHelper.Instance.Error($"{Name} sign in : {ex}");
                    }
                }
            }, 10000);
        }
    }
}
