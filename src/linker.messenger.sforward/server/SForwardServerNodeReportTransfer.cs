using linker.libs;
using linker.libs.extends;
using linker.libs.timer;
using linker.messenger.sforward.messenger;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.sforward.server
{
    public sealed class SForwardServerNodeReportTransfer
    {
        public SForwardServerConfigInfo Config => SForwardServerConfigStore.Config;

        private int connectionNum = 0;
        private ulong bytes = 0;
        private ulong lastBytes = 0;
        private string md5 = string.Empty;

        private readonly ICrypto crypto = CryptoFactory.CreateSymmetric(Helper.GlobalString);

        public int ConnectionNum => connectionNum;

        private readonly SForwardServerConnectionTransfer SForwardServerConnectionTransfer;
        private readonly ISForwardServerConfigStore SForwardServerConfigStore;
        private readonly ISerializer serializer;
        private readonly IMessengerSender messengerSender;
        private readonly ISForwardServerNodeStore  sForwardServerNodeStore;
        private readonly ISForwardServerWhiteListStore  sForwardServerWhiteListStore;
        private readonly IMessengerResolver messengerResolver;

        public SForwardServerNodeReportTransfer(SForwardServerConnectionTransfer SForwardServerConnectionTransfer, ISForwardServerConfigStore SForwardServerConfigStore,
            ISerializer serializer, IMessengerSender messengerSender, ISForwardServerNodeStore sForwardServerNodeStore, ISForwardServerWhiteListStore sForwardServerWhiteListStore, IMessengerResolver messengerResolver)
        {
            this.SForwardServerConnectionTransfer = SForwardServerConnectionTransfer;
            this.SForwardServerConfigStore = SForwardServerConfigStore;
            this.serializer = serializer;
            this.messengerSender = messengerSender;
            this.sForwardServerNodeStore = sForwardServerNodeStore;
            this.sForwardServerWhiteListStore = sForwardServerWhiteListStore;
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
            return await sForwardServerNodeStore.Add(new SForwardServerNodeStoreInfo
            {
                NodeId = id,
                Name = name,
                Host = host
            }).ConfigureAwait(false);
        }
        public async Task<bool> Report(SForwardServerNodeReportInfo info)
        {
            if (SForwardServerConnectionTransfer.TryGet(ConnectionSideType.Node, info.NodeId, out _) == false) return false;

            return await sForwardServerNodeStore.Report(info).ConfigureAwait(false);
        }
        public async Task<bool> SignIn(string serverId, string shareKey, IConnection connection)
        {
            //未被配置，或默认配置的，设它为管理端
            if (string.IsNullOrWhiteSpace(Config.MasterKey) || md5 == Config.MasterKey)
            {
                SForwardServerConfigStore.SetMasterKey(serverId.Md5());
                SForwardServerConfigStore.Confirm();
            }
            if (shareKey != Config.ShareKey && serverId.Md5() != Config.MasterKey)
            {
                return false;
            }

            connection.Id = serverId;
            SForwardServerConnectionTransfer.TryAdd(ConnectionSideType.Master, connection.Id, connection);


            return true;
        }


        public async Task<string> GetShareKeyForward(string nodeId)
        {
            SForwardServerNodeStoreInfo store = await sForwardServerNodeStore.GetByNodeId(nodeId);

            if (store != null && store.Manageable && SForwardServerConnectionTransfer.TryGet(ConnectionSideType.Node, nodeId, out var connection))
            {
                var resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = (ushort)SForwardMessengerIds.Share,
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
                SForwardServerNodeShareInfo info = serializer.Deserialize<SForwardServerNodeShareInfo>(crypto.Decode(Convert.FromBase64String(shareKey)).Span);

                bool result = await sForwardServerNodeStore.Add(new SForwardServerNodeStoreInfo
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

            return await sForwardServerNodeStore.Delete(nodeId).ConfigureAwait(false);
        }
        public async Task<bool> UpdateForward(SForwardServerNodeStoreInfo info)
        {
            SForwardServerNodeStoreInfo store = await sForwardServerNodeStore.GetByNodeId(info.NodeId);

            if (store != null && store.Manageable && SForwardServerConnectionTransfer.TryGet(ConnectionSideType.Node, info.NodeId, out var connection))
            {
                info.MasterKey = store.MasterKey;
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = (ushort)SForwardMessengerIds.Update,
                    Payload = serializer.Serialize(info)
                });
            }

            return await sForwardServerNodeStore.Update(info).ConfigureAwait(false);
        }
        public async Task<bool> Update(SForwardServerNodeStoreInfo info)
        {
            if (info.MasterKey != Config.MasterKey) return false;

            Config.Connections = info.Connections;
            Config.MasterKey = info.MasterKey;
            Config.Bandwidth = info.Bandwidth;
            Config.DataEachMonth = info.DataEachMonth;
            Config.DataRemain = info.DataRemain;
            Config.Logo = info.Logo;
            Config.Name = info.Name;
            Config.Url = info.Url;
            Config.Host = info.Host;

            SForwardServerConfigStore.Confirm();

            return true;
        }
        public async Task<bool> UpgradeForward(string nodeId, string version)
        {
            SForwardServerNodeStoreInfo store = await sForwardServerNodeStore.GetByNodeId(nodeId);

            if (store != null && store.Manageable && SForwardServerConnectionTransfer.TryGet(ConnectionSideType.Node, nodeId, out var connection))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = (ushort)SForwardMessengerIds.Update,
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
            SForwardServerNodeStoreInfo store = await sForwardServerNodeStore.GetByNodeId(nodeId);

            if (store != null && store.Manageable && SForwardServerConnectionTransfer.TryGet(ConnectionSideType.Node, nodeId, out var connection))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = (ushort)SForwardMessengerIds.Update,
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

        /// <summary>
        /// 获取节点列表
        /// </summary>
        /// <param name="super">是否已认证</param>
        /// <returns></returns>
        public async Task<List<SForwardServerNodeStoreInfo>> GetNodes(bool super, string userid, string machineId)
        {
            List<string> sforward = (await sForwardServerWhiteListStore.GetNodes(userid, machineId)).Where(c => c.Bandwidth >= 0).SelectMany(c => c.Nodes).ToList();

            var result = (await sForwardServerNodeStore.GetAll())
                .Where(c => Environment.TickCount64 - c.LastTicks < 15000)
                .Where(c =>
                {
                    return super || c.Public || sforward.Contains(c.NodeId) || sforward.Contains("*");
                })
                .OrderByDescending(c => c.LastTicks);

            return result.ThenBy(x => x.BandwidthRatio)
                     .ThenByDescending(x => x.BandwidthEach == 0 ? double.MaxValue : x.BandwidthEach)
                     .ThenByDescending(x => x.Bandwidth == 0 ? double.MaxValue : x.Bandwidth)
                     .ThenByDescending(x => x.DataEachMonth == 0 ? double.MaxValue : x.DataEachMonth)
                     .ThenByDescending(x => x.DataRemain == 0 ? long.MaxValue : x.DataRemain).ToList();
        }
        public async Task<SForwardServerNodeStoreInfo> GetNode(string nodeId)
        {
            SForwardServerNodeStoreInfo node = await sForwardServerNodeStore.GetByNodeId(nodeId).ConfigureAwait(false);
            if (node == null || Environment.TickCount64 - node.LastTicks < 15000)
            {
                return null;
            }
            return null;
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

                SForwardServerNodeShareInfo shareKeyInfo = new SForwardServerNodeShareInfo
                {
                    NodeId = Config.NodeId,
                    Host = $"{host}:{SForwardServerConfigStore.ServicePort}",
                    Name = Config.Name,
                    SystemId = SystemIdHelper.GetSystemId().Md5()
                };
                string shareKey = Convert.ToBase64String(crypto.Encode(serializer.Serialize(shareKeyInfo)));
                SForwardServerConfigStore.SetShareKey(shareKey);
                SForwardServerConfigStore.Confirm();

                host = $"{IPAddress.Loopback}:{SForwardServerConfigStore.ServicePort}";
                var node = await sForwardServerNodeStore.GetByNodeId(SForwardServerConfigStore.Config.NodeId);
                if (node == null || node.ShareKey != shareKey || node.Name != Config.Name || node.Host != host)
                {
                    await sForwardServerNodeStore.Delete(SForwardServerConfigStore.Config.NodeId);
                    await sForwardServerNodeStore.Add(new SForwardServerNodeStoreInfo
                    {
                        NodeId = SForwardServerConfigStore.Config.NodeId,
                        Name = "default",
                        Host = $"{IPAddress.Loopback}:{SForwardServerConfigStore.ServicePort}",
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
                    var connections = SForwardServerConnectionTransfer.Get(ConnectionSideType.Master);
                    if (connections.Any())
                    {
                        double diff = (bytes - lastBytes) * 8 / 1024.0 / 1024.0;
                        lastBytes = bytes;

                        var config = SForwardServerConfigStore.Config;
                        SForwardServerNodeReportInfo info = new SForwardServerNodeReportInfo
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
                            Masters = connections.Select(c => c.Address).ToArray(),
                            MasterKey = config.MasterKey,
                        };
                        byte[] memory = serializer.Serialize(info);
                        var tasks = connections.Select(c => messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = c,
                            MessengerId = (ushort)SForwardMessengerIds.Report,
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
                    var nodes = (await sForwardServerNodeStore.GetAll()).Where(c =>
                    {
                        return SForwardServerConnectionTransfer.TryGet(ConnectionSideType.Node, c.NodeId, out IConnection connection) == false || connection == null || connection.Connected == false;
                    }).ToList();
                    if (nodes.Count != 0)
                    {
                        var tasks = nodes.Select(async c =>
                        {
                            IPEndPoint remote = await NetworkHelper.GetEndPointAsync(c.Host, 1802).ConfigureAwait(false);
                            Socket socket = new Socket(remote.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                            socket.KeepAlive();
                            await socket.ConnectAsync(remote).WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);
                            var connection = await messengerResolver.BeginReceiveClient(socket, true, (byte)ResolverType.SForwardConnection, Helper.EmptyArray).ConfigureAwait(false);

                            var resp = await messengerSender.SendReply(new MessageRequestWrap
                            {
                                Connection = connection,
                                MessengerId = (ushort)SForwardMessengerIds.SignIn,
                                Payload = serializer.Serialize(new KeyValuePair<string, string>(Config.NodeId, c.ShareKey)),
                                Timeout = 5000
                            }).ConfigureAwait(false);
                            if (resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray))
                            {
                                LoggerHelper.Instance.Debug($"SForward sign in to node {c.NodeId} success");
                                SForwardServerConnectionTransfer.TryAdd(ConnectionSideType.Node, c.NodeId, connection);
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
                        LoggerHelper.Instance.Error($"SForward sign in : {ex}");
                    }
                }
            }, 10000);
        }
    }
}
