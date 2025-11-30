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
            if (relayServerConnectionTransfer.TryGet(info.NodeId, out IConnection connection) == false) return false;

            return await relayServerNodeStore.Report(info).ConfigureAwait(false);
        }
        public async Task<bool> SignIn(string serverId, string nodeId, IConnection connection)
        {
            if (nodeId != Config.NodeId)
            {
                return false;
            }

            connection.Id = serverId;
            relayServerConnectionTransfer.TryAdd(connection.Id, connection);
            return true;
        }


        public async Task<List<RelayServerNodeStoreInfo>> GetNodes(bool validated, string userid, string machineId)
        {
            var nodes = (await relayServerWhiteListStore.GetNodes(userid, machineId)).Where(c => c.Bandwidth >= 0).SelectMany(c => c.Nodes);

            var result = (await relayServerNodeStore.GetAll())
                .Where(c => Environment.TickCount64 - c.LastTicks < 15000)
                .Where(c =>
                {
                    return validated || nodes.Contains(c.NodeId) || nodes.Contains("*")
                    || (c.Public && c.ConnectionsRatio < c.Connections && (c.DataEachMonth == 0 || (c.DataEachMonth > 0 && c.DataRemain > 0)));
                })
                .OrderByDescending(c => c.LastTicks);

            return result.OrderByDescending(x => x.Connections == 0 ? int.MaxValue : x.Connections)
                     .ThenBy(x => x.ConnectionsRatio)
                     .ThenBy(x => x.BandwidthRatio)
                     .ThenByDescending(x => x.BandwidthEachConnection == 0 ? int.MaxValue : x.BandwidthEachConnection)
                     .ThenByDescending(x => x.Bandwidth == 0 ? int.MaxValue : x.Bandwidth)
                     .ThenByDescending(x => x.DataEachMonth == 0 ? int.MaxValue : x.DataEachMonth)
                     .ThenByDescending(x => x.DataRemain == 0 ? long.MaxValue : x.DataRemain)
                     .ToList();
        }
        public async Task<List<RelayServerNodeStoreInfo>> GetPublicNodes()
        {
            var result = (await relayServerNodeStore.GetAll())
                .Where(c => Environment.TickCount64 - c.LastTicks < 15000)
                .Where(c => c.Public)
                .OrderByDescending(c => c.LastTicks);

            return result.OrderByDescending(x => x.Connections == 0 ? int.MaxValue : x.Connections)
                     .ThenBy(x => x.ConnectionsRatio)
                     .ThenBy(x => x.BandwidthRatio)
                     .ThenByDescending(x => x.BandwidthEachConnection == 0 ? int.MaxValue : x.BandwidthEachConnection)
                     .ThenByDescending(x => x.Bandwidth == 0 ? int.MaxValue : x.Bandwidth)
                     .ThenByDescending(x => x.DataEachMonth == 0 ? int.MaxValue : x.DataEachMonth)
                     .ThenByDescending(x => x.DataRemain == 0 ? long.MaxValue : x.DataRemain)
                     .ToList();
        }


        private async Task BuildShareKey()
        {
            try
            {
                await relayServerNodeStore.Add(new RelayServerNodeStoreInfo
                {
                    NodeId = Config.NodeId,
                    Name = Config.Name,
                    Host = $"{IPAddress.Loopback}:{relayServerConfigStore.ServicePort}"
                }).ConfigureAwait(false);

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
                    Name = Config.Name
                };
                string shareKey = Convert.ToBase64String(crypto.Encode(serializer.Serialize(shareKeyInfo)));
                relayServerConfigStore.SetShareKey(shareKey);
                relayServerConfigStore.Confirm();

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
                    var connections = relayServerConnectionTransfer.Get();
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
                            Servers = connections.Select(c => c.Address).ToArray()
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
                        return relayServerConnectionTransfer.TryGet(c.NodeId, out IConnection connection) == false || connection == null || connection.Connected == false;
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
                                Payload = serializer.Serialize(new KeyValuePair<string, string>(Config.NodeId, c.NodeId)),
                                Timeout = 5000
                            }).ConfigureAwait(false);
                            if (resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray))
                            {
                                Console.WriteLine($"relay sign in to node {c.NodeId} success");
                                relayServerConnectionTransfer.TryAdd(c.NodeId, connection);
                            }
                            else
                            {
                                socket.SafeClose();
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
