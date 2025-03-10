using linker.libs;
using linker.libs.extends;
using linker.messenger.relay.messenger;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.relay.server
{
    /// <summary>
    /// 中继节点操作
    /// </summary>
    public class RelayServerNodeTransfer
    {
        public string Id => relayServerNodeStore.Node.Id;

        private uint connectionNum = 0;
        private IConnection localConnection;
        private IConnection remoteConnection;

        private long bytes = 0;
        private long lastBytes = 0;
        private RelaySpeedLimit limitTotal = new RelaySpeedLimit();
        private readonly ConcurrentDictionary<ulong, RelayTrafficCacheInfo> trafficDict = new ConcurrentDictionary<ulong, RelayTrafficCacheInfo>();

        private readonly ISerializer serializer;
        private readonly IRelayServerNodeStore relayServerNodeStore;
        private readonly IRelayServerMasterStore relayServerMasterStore;
        private readonly IMessengerResolver messengerResolver;
        private readonly IMessengerSender messengerSender;

        public RelayServerNodeTransfer(ISerializer serializer, IRelayServerNodeStore relayServerNodeStore, IRelayServerMasterStore relayServerMasterStore, IMessengerResolver messengerResolver, IMessengerSender messengerSender)
        {
            this.serializer = serializer;
            this.relayServerNodeStore = relayServerNodeStore;
            this.relayServerMasterStore = relayServerMasterStore;
            this.messengerResolver = messengerResolver;
            this.messengerSender = messengerSender;

            limitTotal.SetLimit((uint)Math.Ceiling((relayServerNodeStore.Node.MaxBandwidthTotal * 1024 * 1024) / 8.0));

            TrafficTask();
            ReportTask();
            SignInTask();

        }

        public async ValueTask<RelayCacheInfo> TryGetRelayCache(string key, string nodeid)
        {
            try
            {
                IConnection connection = relayServerNodeStore.Node.Id == RelayServerNodeInfo.MASTER_NODE_ID ? localConnection : remoteConnection;

                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = (ushort)RelayMessengerIds.NodeGetCache,
                    Payload = serializer.Serialize(key)
                });
                if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
                {
                    return serializer.Deserialize<RelayCacheInfo>(resp.Data.Span);
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error($"{ex}");
            }
            return null;
        }

        public void UpdateNode(RelayServerNodeUpdateInfo info)
        {
            relayServerNodeStore.UpdateInfo(info);
        }

        /// <summary>
        /// 无效请求
        /// </summary>
        /// <returns></returns>
        public bool Validate(RelayCacheInfo relayCache)
        {
            //已认证的没有流量限制
            if (relayCache.Validated) return true;
            //流量卡有的，就能继续用
            if (relayCache.Cdkey.Any(c => c.LastBytes > 0)) return true;

            return ValidateConnection(relayCache) && ValidateBytes(relayCache);
        }
        /// <summary>
        /// 连接数是否够
        /// </summary>
        /// <returns></returns>
        private bool ValidateConnection(RelayCacheInfo relayCache)
        {
            bool res = relayServerNodeStore.Node.MaxConnection == 0 || relayServerNodeStore.Node.MaxConnection * 2 > connectionNum;
            if (res == false && LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Debug($"relay  ValidateConnection false,{connectionNum}/{relayServerNodeStore.Node.MaxConnection * 2}");

            return res;
        }
        /// <summary>
        /// 流量是否够
        /// </summary>
        /// <returns></returns>
        private bool ValidateBytes(RelayCacheInfo relayCache)
        {
            bool res = relayServerNodeStore.Node.MaxGbTotal == 0
                || (relayServerNodeStore.Node.MaxGbTotal > 0 && relayServerNodeStore.Node.MaxGbTotalLastBytes > 0);

            if (res == false && LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Debug($"relay  ValidateBytes false,{relayServerNodeStore.Node.MaxGbTotalLastBytes}bytes/{relayServerNodeStore.Node.MaxGbTotal}gb");

            return res;
        }

        /// <summary>
        /// 增加连接数
        /// </summary>
        public void IncrementConnectionNum()
        {
            Interlocked.Increment(ref connectionNum);
        }
        /// <summary>
        /// 减少连接数
        /// </summary>
        public void DecrementConnectionNum()
        {
            Interlocked.Decrement(ref connectionNum);
        }

        /// <summary>
        /// 是否需要总限速
        /// </summary>
        /// <returns></returns>
        public bool NeedLimit(RelayTrafficCacheInfo relayCache)
        {
            if (relayCache.Cache.Validated) return false;
            //if (relayCache.CurrentCdkey != null) return false;
            return limitTotal.NeedLimit();
        }
        /// <summary>
        /// 总限速
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public bool TryLimit(ref int length)
        {
            return limitTotal.TryLimit(ref length);
        }


        /// <summary>
        /// 开始计算流量
        /// </summary>
        /// <param name="relayCache"></param>
        public void AddTrafficCache(RelayTrafficCacheInfo relayCache)
        {
            SetLimit(relayCache);
            trafficDict.TryAdd(relayCache.Cache.FlowId, relayCache);
        }
        /// <summary>
        /// 取消计算流量
        /// </summary>
        /// <param name="relayCache"></param>
        public void RemoveTrafficCache(RelayTrafficCacheInfo relayCache)
        {
            trafficDict.TryRemove(relayCache.Cache.FlowId, out _);
        }
        /// <summary>
        /// 消耗流量
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public bool AddBytes(RelayTrafficCacheInfo cache, long length)
        {
            Interlocked.Add(ref bytes, length);

            //验证过的，不消耗流量
            if (cache.Cache.Validated) return true;
            //节点无流量限制的，不消耗流量
            if (relayServerNodeStore.Node.MaxGbTotal == 0) return true;

            Interlocked.Add(ref cache.Sendt, length);

            var current = cache.CurrentCdkey;
            if (current != null) return current.LastBytes > 0;

            return relayServerNodeStore.Node.MaxGbTotalLastBytes > 0;
        }

        /// <summary>
        /// 设置限速
        /// </summary>
        /// <param name="relayCache"></param>
        private void SetLimit(RelayTrafficCacheInfo relayCache)
        {
            //无限制
            if (relayCache.Cache.Validated || relayServerNodeStore.Node.MaxBandwidth == 0)
            {
                relayCache.Limit.SetLimit(0);
                return;
            }

            RelayServerCdkeyInfo currentCdkey = relayCache.Cache.Cdkey.Where(c => c.LastBytes > 0).OrderByDescending(c => c.Bandwidth).FirstOrDefault();
            //有cdkey，且带宽大于节点带宽，就用cdkey的带宽
            if (currentCdkey != null && (currentCdkey.Bandwidth == 0 || currentCdkey.Bandwidth >= relayServerNodeStore.Node.MaxBandwidth || relayServerNodeStore.Node.MaxGbTotalLastBytes == 0))
            {
                relayCache.CurrentCdkey = currentCdkey;
                relayCache.Limit.SetLimit((uint)Math.Ceiling((relayCache.CurrentCdkey.Bandwidth * 1024 * 1024) / 8.0));
                return;
            }
            relayCache.CurrentCdkey = null;
            relayCache.Limit.SetLimit((uint)Math.Ceiling((relayServerNodeStore.Node.MaxBandwidth * 1024 * 1024) / 8.0));
        }

        /// <summary>
        /// 更新剩余流量
        /// </summary>
        /// <param name="dic"></param>
        public void UpdateLastBytes(Dictionary<long, long> dic)
        {
            if (dic.Count == 0) return;

            Dictionary<long, RelayServerCdkeyInfo> cdkeys = trafficDict.Values.SelectMany(c => c.Cache.Cdkey).ToDictionary(c => c.CdkeyId, c => c);
            //更新剩余流量
            foreach (KeyValuePair<long, long> item in dic)
            {
                if (cdkeys.TryGetValue(item.Key, out RelayServerCdkeyInfo info))
                {
                    info.LastBytes = item.Value;
                }
            }
        }
        private void ResetNodeBytes()
        {
            foreach (var cache in trafficDict.Values.Where(c => c.CurrentCdkey == null))
            {
                long length = Interlocked.Exchange(ref cache.Sendt, 0);

                if (relayServerNodeStore.Node.MaxGbTotalLastBytes >= length)
                    relayServerNodeStore.SetMaxGbTotalLastBytes(relayServerNodeStore.Node.MaxGbTotalLastBytes - length);
                else relayServerNodeStore.SetMaxGbTotalLastBytes(0);
            }
            if (relayServerNodeStore.Node.MaxGbTotalMonth != DateTime.Now.Month)
            {
                relayServerNodeStore.SetMaxGbTotalMonth(DateTime.Now.Month);
                relayServerNodeStore.SetMaxGbTotalLastBytes((long)(relayServerNodeStore.Node.MaxGbTotal * 1024 * 1024 * 1024));
            }
            relayServerNodeStore.Confirm();
        }
        private async Task UploadBytes()
        {
            var cdkeys = trafficDict.Values.Where(c => c.CurrentCdkey != null && c.Sendt > 0).ToList();
            Dictionary<long, long> id2sent = cdkeys.GroupBy(c => c.CurrentCdkey.CdkeyId).ToDictionary(c => c.Key, d => d.Sum(d => { d.SendtCache = d.Sendt; return d.SendtCache; }));
            if (id2sent.Count == 0) return;

            bool result = await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = relayServerNodeStore.Node.Id == RelayServerNodeInfo.MASTER_NODE_ID ? localConnection : remoteConnection,
                MessengerId = (ushort)RelayMessengerIds.TrafficReport,
                Payload = serializer.Serialize(new RelayTrafficUpdateInfo
                {
                    Dic = id2sent,
                    SecretKey = relayServerNodeStore.Node.Id == RelayServerNodeInfo.MASTER_NODE_ID
                             ? relayServerMasterStore.Master.SecretKey
                             : relayServerNodeStore.Node.MasterSecretKey
                }),
                Timeout = 4000
            });

            if (result)
            {
                //成功报告了流量，就重新计数
                foreach (var cache in cdkeys)
                {
                    Interlocked.Add(ref cache.Sendt, -cache.SendtCache);
                    Interlocked.Exchange(ref cache.SendtCache, 0);
                    //当前cdkey流量用完了，就重新找找新的cdkey
                    if (cache.CurrentCdkey.LastBytes <= 0)
                    {
                        SetLimit(cache);
                    }
                }
            }
        }
        private void TrafficTask()
        {
            TimerHelper.SetIntervalLong(async () =>
            {
                try
                {
                    ResetNodeBytes();
                    await UploadBytes();
                }
                catch (Exception ex)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                return true;
            }, 3000);
        }

        private void ReportTask()
        {
            TimerHelper.SetIntervalLong(async () =>
            {
                IEnumerable<RelayServerNodeInfo> nodes = new List<RelayServerNodeInfo>
                {
                    //默认报告给自己，作为本服务器的一个默认中继节点
                    new RelayServerNodeInfo{
                        Id = RelayServerNodeInfo.MASTER_NODE_ID,
                        Host = new IPEndPoint(IPAddress.Any, relayServerNodeStore.ServicePort).ToString(),
                        MasterHost =  new IPEndPoint(IPAddress.Loopback, relayServerNodeStore.ServicePort).ToString(),
                        MasterSecretKey = relayServerMasterStore.Master.SecretKey,
                        MaxBandwidth = 0,
                        MaxConnection = 0,
                        MaxBandwidthTotal=0,
                        MaxGbTotal=0,
                        MaxGbTotalLastBytes=0,
                        MaxGbTotalMonth=0,
                        Name = "default",
                        Public = false
                    },
                    //配置的中继节点
                    relayServerNodeStore.Node
                }.Where(c => string.IsNullOrWhiteSpace(c.MasterHost) == false && string.IsNullOrWhiteSpace(c.MasterSecretKey) == false)
                .Where(c => string.IsNullOrWhiteSpace(c.Name) == false && string.IsNullOrWhiteSpace(c.Id) == false);

                double diff = (bytes - lastBytes) * 8 / 1024.0 / 1024.0;
                lastBytes = bytes;

                foreach (var node in nodes)
                {
                    try
                    {
                        IConnection connection = node.Id == RelayServerNodeInfo.MASTER_NODE_ID ? localConnection : remoteConnection;
                        IPEndPoint endPoint = await NetworkHelper.GetEndPointAsync(node.Host, relayServerNodeStore.ServicePort) ?? new IPEndPoint(IPAddress.Any, relayServerNodeStore.ServicePort);

                        RelayServerNodeReportInfo170 relayNodeReportInfo = new RelayServerNodeReportInfo170
                        {
                            Id = node.Id,
                            Name = node.Name,
                            Public = node.Public,
                            MaxBandwidth = node.MaxBandwidth,
                            BandwidthRatio = Math.Round(diff / 5, 2),
                            MaxBandwidthTotal = node.MaxBandwidthTotal,
                            MaxGbTotal = node.MaxGbTotal,
                            MaxGbTotalLastBytes = node.MaxGbTotalLastBytes,
                            MaxConnection = node.MaxConnection,
                            ConnectionRatio = Math.Round(connectionNum / 2.0),
                            EndPoint = endPoint,
                            Url = node.Url
                        };

                        await messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = connection,
                            MessengerId = (ushort)RelayMessengerIds.NodeReport,
                            Payload = serializer.Serialize(relayNodeReportInfo)
                        });
                    }
                    catch (Exception ex)
                    {
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        {
                            LoggerHelper.Instance.Error($"relay report : {ex}");
                        }
                    }
                }
                return true;
            }, 5000);
        }

        private void SignInTask()
        {
            TimerHelper.SetIntervalLong(async () =>
            {
                if ((remoteConnection == null || remoteConnection.Connected == false) && string.IsNullOrWhiteSpace(relayServerNodeStore.Node.MasterHost) == false)
                {
                    remoteConnection = await SignIn(relayServerNodeStore.Node.MasterHost, relayServerNodeStore.Node.MasterSecretKey).ConfigureAwait(false);
                }
                if (localConnection == null || localConnection.Connected == false)
                {
                    localConnection = await SignIn(new IPEndPoint(IPAddress.Loopback, relayServerNodeStore.ServicePort).ToString(), relayServerMasterStore.Master.SecretKey).ConfigureAwait(false);
                }
                return true;
            }, 3000);
        }
        private async Task<IConnection> SignIn(string host, string secretKey)
        {
            byte[] bytes = ArrayPool<byte>.Shared.Rent(1024);
            try
            {
                byte[] secretKeyBytes = secretKey.Md5().ToBytes();

                bytes[0] = (byte)secretKeyBytes.Length;
                secretKeyBytes.AsSpan().CopyTo(bytes.AsSpan(1));


                IPEndPoint remote = await NetworkHelper.GetEndPointAsync(host, 1802);

                Socket socket = new Socket(remote.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.KeepAlive();
                await socket.ConnectAsync(remote).WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);
                return await messengerResolver.BeginReceiveClient(socket, true, (byte)ResolverType.RelayReport, bytes.AsMemory(0, secretKeyBytes.Length + 1)).ConfigureAwait(false);
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
                ArrayPool<byte>.Shared.Return(bytes);
            }
            return null;
        }
    }

    public sealed partial class RelayTrafficUpdateInfo
    {
        /// <summary>
        /// cdkey id  和 流量
        /// </summary>
        public Dictionary<long, long> Dic { get; set; }
        public string SecretKey { get; set; }
    }

}
