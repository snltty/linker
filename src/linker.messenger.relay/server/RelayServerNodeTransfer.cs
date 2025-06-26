using linker.libs;
using linker.libs.extends;
using linker.libs.timer;
using linker.messenger.cdkey;
using linker.messenger.relay.messenger;
using linker.tunnel.connection;
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
        /// <summary>
        /// 配置了就用配置的，每配置就用一个默认的
        /// </summary>
        public RelayServerNodeInfo node => relayServerNodeStore.Node;

        private uint connectionNum = 0;
        private IConnection connection;

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

            if (string.IsNullOrWhiteSpace(relayServerNodeStore.Node.MasterHost))
            {
                relayServerNodeStore.Node.Host = new IPEndPoint(IPAddress.Any, relayServerNodeStore.ServicePort).ToString();
                relayServerNodeStore.Node.MasterHost = new IPEndPoint(IPAddress.Loopback, relayServerNodeStore.ServicePort).ToString();
                relayServerNodeStore.Node.MasterSecretKey = relayServerMasterStore.Master.SecretKey;
                relayServerNodeStore.Node.Name = "default";
                relayServerNodeStore.Node.Public = false;
            }

            limitTotal.SetLimit((uint)Math.Ceiling((node.MaxBandwidthTotal * 1024 * 1024) / 8.0));

            TrafficTask();
            ReportTask();
            SignInTask();

        }

        public async Task<RelayCacheInfo> TryGetRelayCache(string key)
        {
            try
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = (ushort)RelayMessengerIds.NodeGetCache186,
                    Payload = serializer.Serialize(new ValueTuple<string, string>(key, node.Id))
                }).ConfigureAwait(false);
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
            if (info.Id == node.Id)
            {
                relayServerNodeStore.UpdateInfo(info);
                relayServerNodeStore.Confirm();
            }
        }

        public bool Validate(TunnelProtocolType tunnelProtocolType)
        {
            if (tunnelProtocolType == TunnelProtocolType.Udp && node.AllowUdp == false) return false;
            if (tunnelProtocolType == TunnelProtocolType.Tcp && node.AllowTcp == false) return false;

            return true;
        }
        /// <summary>
        /// 无效请求
        /// </summary>
        /// <returns></returns>
        public bool Validate(RelayCacheInfo relayCache)
        {
            //已认证的没有流量限制
            if (relayCache.Validated) return true;
            if (node.Public == false) return false;
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
            bool res = node.MaxConnection == 0 || node.MaxConnection * 2 > connectionNum;
            if (res == false && LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Debug($"relay  ValidateConnection false,{connectionNum}/{node.MaxConnection * 2}");

            return res;
        }
        /// <summary>
        /// 流量是否够
        /// </summary>
        /// <returns></returns>
        private bool ValidateBytes(RelayCacheInfo relayCache)
        {
            bool res = node.MaxGbTotal == 0
                || (node.MaxGbTotal > 0 && node.MaxGbTotalLastBytes > 0);

            if (res == false && LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Debug($"relay  ValidateBytes false,{node.MaxGbTotalLastBytes}bytes/{node.MaxGbTotal}gb");

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
        /// 总限速
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public bool TryLimitPacket(int length)
        {
            return limitTotal.TryLimitPacket(length);
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
            if (node.MaxGbTotal == 0) return true;

            Interlocked.Add(ref cache.Sendt, length);

            var current = cache.CurrentCdkey;
            if (current != null) return current.LastBytes > 0;

            return node.MaxGbTotalLastBytes > 0;
        }

        /// <summary>
        /// 设置限速
        /// </summary>
        /// <param name="relayCache"></param>
        private void SetLimit(RelayTrafficCacheInfo relayCache)
        {
            //无限制
            if (relayCache.Cache.Validated || node.MaxBandwidth == 0)
            {
                relayCache.Limit.SetLimit(0);
                return;
            }

            CdkeyInfo currentCdkey = relayCache.Cache.Cdkey.Where(c => c.LastBytes > 0).OrderByDescending(c => c.Bandwidth).FirstOrDefault();
            //有cdkey，且带宽大于节点带宽，就用cdkey的带宽
            if (currentCdkey != null && (currentCdkey.Bandwidth == 0 || currentCdkey.Bandwidth >= node.MaxBandwidth || node.MaxGbTotalLastBytes == 0))
            {
                relayCache.CurrentCdkey = currentCdkey;
                relayCache.Limit.SetLimit((uint)Math.Ceiling((relayCache.CurrentCdkey.Bandwidth * 1024 * 1024) / 8.0));
                return;
            }
            relayCache.CurrentCdkey = null;
            relayCache.Limit.SetLimit((uint)Math.Ceiling((node.MaxBandwidth * 1024 * 1024) / 8.0));
        }

        /// <summary>
        /// 更新剩余流量
        /// </summary>
        /// <param name="dic"></param>
        public void UpdateLastBytes(Dictionary<int, long> dic)
        {
            if (dic.Count == 0) return;

            Dictionary<int, CdkeyInfo> cdkeys = trafficDict.Values.SelectMany(c => c.Cache.Cdkey).ToDictionary(c => c.Id, c => c);
            //更新剩余流量
            foreach (KeyValuePair<int, long> item in dic)
            {
                if (cdkeys.TryGetValue(item.Key, out CdkeyInfo info))
                {
                    info.LastBytes = item.Value;
                }
            }
        }
        private void ResetNodeBytes()
        {
            if (node.MaxGbTotal == 0) return;

            foreach (var cache in trafficDict.Values.Where(c => c.CurrentCdkey == null))
            {
                long length = Interlocked.Exchange(ref cache.Sendt, 0);

                if (node.MaxGbTotalLastBytes >= length)
                    relayServerNodeStore.SetMaxGbTotalLastBytes(node.MaxGbTotalLastBytes - length);
                else relayServerNodeStore.SetMaxGbTotalLastBytes(0);
            }
            if (node.MaxGbTotalMonth != DateTime.Now.Month)
            {
                relayServerNodeStore.SetMaxGbTotalMonth(DateTime.Now.Month);
                relayServerNodeStore.SetMaxGbTotalLastBytes((long)(node.MaxGbTotal * 1024 * 1024 * 1024));
            }
            relayServerNodeStore.Confirm();
        }
        private async Task UploadBytes()
        {
            var cdkeys = trafficDict.Values.Where(c => c.CurrentCdkey != null && c.Sendt > 0).ToList();
            Dictionary<int, long> id2sent = cdkeys.GroupBy(c => c.CurrentCdkey.Id).ToDictionary(c => c.Key, d => d.Sum(d => { d.SendtCache = d.Sendt; return d.SendtCache; }));
            if (id2sent.Count == 0) return;

            bool result = await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = connection,
                MessengerId = (ushort)RelayMessengerIds.TrafficReport,
                Payload = serializer.Serialize(new RelayTrafficUpdateInfo
                {
                    Dic = id2sent,
                    SecretKey = node.MasterSecretKey
                }),
                Timeout = 4000
            }).ConfigureAwait(false);

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
                    await UploadBytes().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }, 3000);
        }

        private void ReportTask()
        {
            TimerHelper.SetIntervalLong(async () =>
            {
                double diff = (bytes - lastBytes) * 8 / 1024.0 / 1024.0;
                lastBytes = bytes;

                try
                {
                    IPEndPoint endPoint = await NetworkHelper.GetEndPointAsync(node.Host, relayServerNodeStore.ServicePort).ConfigureAwait(false) ?? new IPEndPoint(IPAddress.Any, relayServerNodeStore.ServicePort);
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
                        ConnectionRatio = connectionNum,
                        EndPoint = endPoint,
                        Url = node.Url,
                        AllowProtocol = (node.AllowTcp ? TunnelProtocolType.Tcp : TunnelProtocolType.None)
                         | (node.AllowUdp ? TunnelProtocolType.Udp : TunnelProtocolType.None)
                    };
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = connection,
                        MessengerId = (ushort)RelayMessengerIds.NodeReport,
                        Payload = serializer.Serialize(relayNodeReportInfo)
                    }).ConfigureAwait(false);
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
                if (connection == null || connection.Connected == false)
                {
                    connection = await SignIn(node.MasterHost, node.MasterSecretKey).ConfigureAwait(false);
                }
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


                IPEndPoint remote = await NetworkHelper.GetEndPointAsync(host, 1802).ConfigureAwait(false);
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Warning($"relay node sign in to {remote}");
                }

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
        public Dictionary<int, long> Dic { get; set; }
        public string SecretKey { get; set; }
    }

}
