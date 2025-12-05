using linker.libs;
using linker.libs.extends;
using linker.libs.timer;
using linker.messenger.relay.messenger;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.relay.server
{
    public sealed class RelayServerNodeReportTransfer
    {
        public RelayServerConfigInfo Config => relayServerConfigStore.Config;

        private int connectionNum = 0;
        private ulong bytes = 0;
        private ulong lastBytes = 0;
        private string md5 = string.Empty;

        private readonly ICrypto crypto = CryptoFactory.CreateSymmetric(Helper.GlobalString);

        public int ConnectionNum => connectionNum;

        private readonly RelayServerConnectionTransfer relayServerConnectionTransfer;
        private readonly IRelayServerConfigStore relayServerConfigStore;
        private readonly ISerializer serializer;
        private readonly IMessengerSender messengerSender;
        private readonly IRelayServerNodeStore relayServerNodeStore;
        private readonly IRelayServerWhiteListStore relayServerWhiteListStore;
        private readonly IMessengerResolver messengerResolver;

        public RelayServerNodeReportTransfer(RelayServerConnectionTransfer relayServerConnectionTransfer, IRelayServerConfigStore relayServerConfigStore,
            ISerializer serializer, IMessengerSender messengerSender, IRelayServerNodeStore relayServerNodeStore, IRelayServerWhiteListStore relayServerWhiteListStore, IMessengerResolver messengerResolver)
        {
            this.relayServerConnectionTransfer = relayServerConnectionTransfer;
            this.relayServerConfigStore = relayServerConfigStore;
            this.serializer = serializer;
            this.messengerSender = messengerSender;
            this.relayServerNodeStore = relayServerNodeStore;
            this.relayServerWhiteListStore = relayServerWhiteListStore;
            this.messengerResolver = messengerResolver;

            md5 = Config.NodeId.Md5();

            _ = ReportTask();
            SignInTask();

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
            return await relayServerNodeStore.Add(new RelayServerNodeStoreInfo
            {
                NodeId = id,
                Name = name,
                Host = host
            }).ConfigureAwait(false);
        }
        public async Task<bool> Report(RelayServerNodeReportInfo info)
        {
            if (relayServerConnectionTransfer.TryGet(ConnectionSideType.Node, info.NodeId, out _) == false) return false;

            return await relayServerNodeStore.Report(info).ConfigureAwait(false);
        }
        public async Task<bool> SignIn(string serverId, string shareKey, IConnection connection)
        {
            //未被配置，或默认配置的，设它为管理端
            if (string.IsNullOrWhiteSpace(Config.MasterKey) || md5 == Config.MasterKey)
            {
                relayServerConfigStore.SetMasterKey(serverId.Md5());
                relayServerConfigStore.Confirm();
            }

            if (shareKey != Config.ShareKey && serverId.Md5() != Config.MasterKey)
            {
                return false;
            }

            connection.Id = serverId;
            relayServerConnectionTransfer.TryAdd(ConnectionSideType.Master, connection.Id, connection);


            return true;
        }


        public async Task<string> GetShareKeyForward(string nodeId)
        {
            RelayServerNodeStoreInfo store = await relayServerNodeStore.GetByNodeId(nodeId);

            if (store != null && store.Manageable && relayServerConnectionTransfer.TryGet(ConnectionSideType.Node, nodeId, out var connection))
            {
                var resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = (ushort)RelayMessengerIds.Share,
                    Payload = serializer.Serialize(store.MasterKey)
                });
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
                RelayServerNodeShareInfo info = serializer.Deserialize<RelayServerNodeShareInfo>(crypto.Decode(Convert.FromBase64String(shareKey)).Span);

                bool result = await relayServerNodeStore.Add(new RelayServerNodeStoreInfo
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

            return await relayServerNodeStore.Delete(nodeId).ConfigureAwait(false);
        }
        public async Task<bool> UpdateForward(RelayServerNodeStoreInfo info)
        {
            RelayServerNodeStoreInfo store = await relayServerNodeStore.GetByNodeId(info.NodeId);

            if (store != null && store.Manageable && relayServerConnectionTransfer.TryGet(ConnectionSideType.Node, info.NodeId, out var connection))
            {
                info.MasterKey = store.MasterKey;
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = (ushort)RelayMessengerIds.Update,
                    Payload = serializer.Serialize(info)
                });
            }

            return await relayServerNodeStore.Update(info).ConfigureAwait(false);
        }
        public async Task<bool> Update(RelayServerNodeStoreInfo info)
        {
            if (info.MasterKey != Config.MasterKey) return false;

            Config.Connections = info.Connections;
            Config.MasterKey = info.MasterKey;
            Config.Bandwidth = info.Bandwidth;
            Config.Protocol = info.Protocol;
            Config.DataEachMonth = info.DataEachMonth;
            Config.DataRemain = info.DataRemain;
            Config.Logo = info.Logo;
            Config.Name = info.Name;
            Config.Url = info.Url;
            Config.Domain = info.Domain;

            relayServerConfigStore.Confirm();

            return true;
        }
        public async Task<bool> UpgradeForward(string nodeId, string version)
        {
            RelayServerNodeStoreInfo store = await relayServerNodeStore.GetByNodeId(nodeId);

            if (store != null && store.Manageable && relayServerConnectionTransfer.TryGet(ConnectionSideType.Node, nodeId, out var connection))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = (ushort)RelayMessengerIds.Update,
                    Payload = serializer.Serialize(new KeyValuePair<string, string>(store.MasterKey, version))
                });
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
            RelayServerNodeStoreInfo store = await relayServerNodeStore.GetByNodeId(nodeId);

            if (store != null && store.Manageable && relayServerConnectionTransfer.TryGet(ConnectionSideType.Node, nodeId, out var connection))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = (ushort)RelayMessengerIds.Update,
                    Payload = serializer.Serialize(new KeyValuePair<string, string>(store.MasterKey, nodeId))
                });
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

        public async Task<List<RelayServerNodeStoreInfo>> GetNodes(bool validated, string userid, string machineId)
        {
            var nodes = (await relayServerWhiteListStore.GetNodes(userid, machineId)).Where(c => c.Bandwidth >= 0).SelectMany(c => c.Nodes);

            var result = (await relayServerNodeStore.GetAll())
                .Where(c => validated || Environment.TickCount64 - c.LastTicks < 15000)
                .Where(c =>
                {
                    return validated || nodes.Contains(c.NodeId) || nodes.Contains("*")
                    || (c.Public && c.ConnectionsRatio < c.Connections && (c.DataEachMonth == 0 || (c.DataEachMonth > 0 && c.DataRemain > 0)));
                })
                .OrderByDescending(c => c.LastTicks);

            var list = result.OrderByDescending(x => x.Connections == 0 ? int.MaxValue : x.Connections)
                     .ThenBy(x => x.ConnectionsRatio)
                     .ThenBy(x => x.BandwidthRatio)
                     .ThenByDescending(x => x.BandwidthEach == 0 ? int.MaxValue : x.BandwidthEach)
                     .ThenByDescending(x => x.Bandwidth == 0 ? int.MaxValue : x.Bandwidth)
                     .ThenByDescending(x => x.DataEachMonth == 0 ? int.MaxValue : x.DataEachMonth)
                     .ThenByDescending(x => x.DataRemain == 0 ? long.MaxValue : x.DataRemain)
                     .ToList();

            list.ForEach(c =>
            {
                c.MasterKey = string.Empty;
                c.LastTicks = Environment.TickCount64 - c.LastTicks;
            });
            return list;
        }
        public async Task<List<RelayServerNodeStoreInfo>> GetPublicNodes()
        {
            var result = (await relayServerNodeStore.GetAll())
                .Where(c => Environment.TickCount64 - c.LastTicks < 15000)
                .Where(c => c.Public)
                .OrderByDescending(c => c.LastTicks);

            var list = result.OrderByDescending(x => x.Connections == 0 ? int.MaxValue : x.Connections)
                     .ThenBy(x => x.ConnectionsRatio)
                     .ThenBy(x => x.BandwidthRatio)
                     .ThenByDescending(x => x.BandwidthEach == 0 ? int.MaxValue : x.BandwidthEach)
                     .ThenByDescending(x => x.Bandwidth == 0 ? int.MaxValue : x.Bandwidth)
                     .ThenByDescending(x => x.DataEachMonth == 0 ? int.MaxValue : x.DataEachMonth)
                     .ThenByDescending(x => x.DataRemain == 0 ? long.MaxValue : x.DataRemain)
                     .ToList();
            list.ForEach(c =>
            {
                c.MasterKey = string.Empty;
                c.LastTicks = Environment.TickCount64 - c.LastTicks;
            });
            return list;
        }

        private async Task BuildShareKey()
        {
            try
            {
                string host = Config.Domain;
                if (string.IsNullOrWhiteSpace(host))
                {
                    using HttpClient httpClient = new HttpClient();
                    host = await httpClient.GetStringAsync($"https://ifconfig.me/ip").WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);
                }

                RelayServerNodeShareInfo shareKeyInfo = new RelayServerNodeShareInfo
                {
                    NodeId = Config.NodeId,
                    Host = $"{host}:{relayServerConfigStore.ServicePort}",
                    Name = Config.Name,
                    SystemId = SystemIdHelper.GetSystemId().Md5()
                };
                string shareKey = Convert.ToBase64String(crypto.Encode(serializer.Serialize(shareKeyInfo)));
                relayServerConfigStore.SetShareKey(shareKey);
                relayServerConfigStore.Confirm();

                host = $"{IPAddress.Loopback}:{relayServerConfigStore.ServicePort}";
                var node = await relayServerNodeStore.GetByNodeId(relayServerConfigStore.Config.NodeId);
                if(node == null || node.ShareKey != shareKey || node.Name != Config.Name || node.Host != host)
                {
                    await relayServerNodeStore.Delete(relayServerConfigStore.Config.NodeId);
                    await relayServerNodeStore.Add(new RelayServerNodeStoreInfo
                    {
                        NodeId = relayServerConfigStore.Config.NodeId,
                        Name = "default",
                        Host = $"{IPAddress.Loopback}:{relayServerConfigStore.ServicePort}",
                        ShareKey = shareKey
                    }).ConfigureAwait(false);
                }

                LoggerHelper.Instance.Warning($"build relay share key : {shareKey}");
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error($"build relay share key error : {ex}");
            }
        }
        private async Task ReportTask()
        {
            await BuildShareKey().ConfigureAwait(false);

            TimerHelper.SetIntervalLong(async () =>
            {
                try
                {
                    var connections = relayServerConnectionTransfer.Get(ConnectionSideType.Master);
                    if (connections.Any())
                    {
                        double diff = (bytes - lastBytes) * 8 / 1024.0 / 1024.0;
                        lastBytes = bytes;

                        var config = relayServerConfigStore.Config;
                        RelayServerNodeReportInfo info = new RelayServerNodeReportInfo
                        {
                            Bandwidth = config.Bandwidth,
                            Connections = config.Connections,
                            DataEachMonth = config.DataEachMonth,
                            DataRemain = config.DataRemain,
                            Logo = config.Logo,
                            Name = config.Name,
                            NodeId = config.NodeId,
                            Protocol = config.Protocol,
                            Url = config.Url,
                            ConnectionsRatio = connectionNum,
                            BandwidthRatio = Math.Round(diff / 5, 2),
                            Version = VersionHelper.Version,
                            Masters = connections.Select(c => c.Address).ToArray(),
                            MasterKey = config.MasterKey,
                        };
                        byte[] memory = serializer.Serialize(info);
                        var tasks = connections.Select(c => messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = c,
                            MessengerId = (ushort)RelayMessengerIds.Report,
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
                        LoggerHelper.Instance.Error($"relay report : {ex}");
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
                    var nodes = (await relayServerNodeStore.GetAll()).Where(c =>
                    {
                        return relayServerConnectionTransfer.TryGet(ConnectionSideType.Node, c.NodeId, out IConnection connection) == false || connection == null || connection.Connected == false;
                    }).ToList();
                    if (nodes.Count != 0)
                    {
                        var tasks = nodes.Select(async c =>
                        {
                            IPEndPoint remote = await NetworkHelper.GetEndPointAsync(c.Host, 1802).ConfigureAwait(false);
                            Socket socket = new Socket(remote.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                            socket.KeepAlive();
                            await socket.ConnectAsync(remote).WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);
                            var connection = await messengerResolver.BeginReceiveClient(socket, true, (byte)ResolverType.RelayConnection, Helper.EmptyArray).ConfigureAwait(false);

                            var resp = await messengerSender.SendReply(new MessageRequestWrap
                            {
                                Connection = connection,
                                MessengerId = (ushort)RelayMessengerIds.SignIn,
                                Payload = serializer.Serialize(new KeyValuePair<string, string>(Config.NodeId, c.ShareKey)),
                                Timeout = 5000
                            }).ConfigureAwait(false);
                            if (resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray))
                            {
                                LoggerHelper.Instance.Debug($"relay sign in to node {c.NodeId} success");
                                relayServerConnectionTransfer.TryAdd(ConnectionSideType.Node, c.NodeId, connection);
                            }
                            else
                            {
                                connection.Disponse();
                            }

                        });
                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error($"relay sign in : {ex}");
                    }
                }
            }, 10000);
        }
    }
}
