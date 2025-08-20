using linker.libs;
using linker.libs.extends;
using linker.libs.timer;
using linker.plugins.sforward.messenger;
using linker.plugins.sforward.proxy;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;

namespace linker.messenger.sforward.server
{
    /// <summary>
    /// 穿透节点操作
    /// </summary>
    public class SForwardServerNodeTransfer
    {
        /// <summary>
        /// 配置了就用配置的，每配置就用一个默认的
        /// </summary>
        public SForwardServerNodeInfo Node => sForwardServerNodeStore.Node;

        private IConnection connection;
        public IConnection Connection => connection;

        private readonly NumberSpace ns = new NumberSpace(65537);
        private long bytes = 0;
        private long lastBytes = 0;
        private SForwardSpeedLimit limitTotal = new SForwardSpeedLimit();
        private readonly ConcurrentDictionary<ulong, SForwardTrafficCacheInfo> trafficDict = new ConcurrentDictionary<ulong, SForwardTrafficCacheInfo>();

        private readonly ISerializer serializer;
        private readonly ISForwardServerNodeStore sForwardServerNodeStore;
        private readonly IMessengerResolver messengerResolver;
        private readonly IMessengerSender messengerSender;
        private readonly ISForwardServerStore sForwardServerStore;

        public SForwardServerNodeTransfer(ISerializer serializer, ISForwardServerNodeStore sForwardServerNodeStore, ISForwardServerMasterStore sForwardServerMasterStore, IMessengerResolver messengerResolver, IMessengerSender messengerSender, ISForwardServerStore sForwardServerStore)
        {
            this.serializer = serializer;
            this.sForwardServerNodeStore = sForwardServerNodeStore;
            this.messengerResolver = messengerResolver;
            this.messengerSender = messengerSender;
            this.sForwardServerStore = sForwardServerStore;

            if (string.IsNullOrWhiteSpace(sForwardServerNodeStore.Node.MasterHost))
            {
                sForwardServerNodeStore.Node.Domain = IPAddress.Any.ToString();
                sForwardServerNodeStore.Node.MasterHost = new IPEndPoint(IPAddress.Loopback, sForwardServerNodeStore.ServicePort).ToString();
                sForwardServerNodeStore.Node.MasterSecretKey = sForwardServerMasterStore.Master.SecretKey;
                sForwardServerNodeStore.Node.Name = "default";
                sForwardServerNodeStore.Node.Public = false;
            }

            TrafficTask();
            ReportTask();
            SignInTask();
        }

        public void Edit(SForwardServerNodeUpdateInfo info)
        {
            if (info.Id == Node.Id)
            {
                sForwardServerNodeStore.UpdateInfo(info);
                sForwardServerNodeStore.Confirm();

                _ = Report();
            }
        }
        public void Exit()
        {
            Helper.AppExit(1);
        }
        public void Update(string version)
        {
            Helper.AppUpdate(version);
        }

        /// <summary>
        /// 是否需要总限速
        /// </summary>
        /// <returns></returns>
        public bool NeedLimit(SForwardTrafficCacheInfo relayCache)
        {
            if (relayCache.Cache.Validated) return false;
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
        /// <param name="validated"></param>
        /// <param name="cdkeys"></param>
        /// <returns></returns>
        public SForwardTrafficCacheInfo AddTrafficCache(bool validated, List<SForwardCdkeyInfo> cdkeys)
        {
            SForwardTrafficCacheInfo cache = new SForwardTrafficCacheInfo { Cache = new SForwardCacheInfo { Cdkey = cdkeys, FlowId = ns.Increment(), Validated = validated }, Limit = new SForwardSpeedLimit(), Sendt = 0, SendtCache = 0 };

            SetLimit(cache);
            trafficDict.TryAdd(cache.Cache.FlowId, cache);

            return cache;
        }
        public void RemoveTrafficCache(ulong id)
        {
            trafficDict.TryRemove(id, out _);
        }
        /// <summary>
        /// 消耗流量
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public bool AddBytes(SForwardTrafficCacheInfo cache, long length)
        {
            Interlocked.Add(ref bytes, length);

            //验证过的，不消耗流量
            if (cache.Cache.Validated) return true;
            //节点无流量限制的，不消耗流量
            if (Node.MaxGbTotal == 0) return true;

            Interlocked.Add(ref cache.Sendt, length);

            var current = cache.CurrentCdkey;
            if (current != null) return current.LastBytes > 0;

            return Node.MaxGbTotalLastBytes > 0;
        }

        /// <summary>
        /// 设置限速
        /// </summary>
        /// <param name="relayCache"></param>
        private void SetLimit(SForwardTrafficCacheInfo relayCache)
        {
            //无限制
            if (relayCache.Cache.Validated || Node.MaxBandwidth == 0)
            {
                relayCache.Limit.SetLimit(0);
                return;
            }

            SForwardCdkeyInfo currentCdkey = relayCache.Cache.Cdkey.Where(c => c.LastBytes > 0).OrderByDescending(c => c.Bandwidth).FirstOrDefault();
            //有cdkey，且带宽大于节点带宽，就用cdkey的带宽
            if (currentCdkey != null && (currentCdkey.Bandwidth == 0 || currentCdkey.Bandwidth >= Node.MaxBandwidth || Node.MaxGbTotalLastBytes == 0))
            {
                relayCache.CurrentCdkey = currentCdkey;
                relayCache.Limit.SetLimit((uint)Math.Ceiling((relayCache.CurrentCdkey.Bandwidth * 1024 * 1024) / 8.0));
                return;
            }
            relayCache.CurrentCdkey = null;
            relayCache.Limit.SetLimit((uint)Math.Ceiling((Node.MaxBandwidth * 1024 * 1024) / 8.0));
        }


        /// <summary>
        /// 更新剩余流量
        /// </summary>
        /// <param name="dic"></param>
        public void UpdateLastBytes(Dictionary<int, long> dic)
        {
            if (dic.Count == 0) return;

            Dictionary<int, SForwardCdkeyInfo> cdkeys = trafficDict.Values.SelectMany(c => c.Cache.Cdkey).ToDictionary(c => c.Id, c => c);
            //更新剩余流量
            foreach (KeyValuePair<int, long> item in dic)
            {
                if (cdkeys.TryGetValue(item.Key, out SForwardCdkeyInfo info))
                {
                    info.LastBytes = item.Value;
                }
            }
        }
        private void ResetNodeBytes()
        {
            if (Node.MaxGbTotal == 0) return;

            foreach (var cache in trafficDict.Values.Where(c => c.CurrentCdkey == null))
            {
                long length = Interlocked.Exchange(ref cache.Sendt, 0);

                if (Node.MaxGbTotalLastBytes >= length)
                    sForwardServerNodeStore.SetMaxGbTotalLastBytes(Node.MaxGbTotalLastBytes - length);
                else sForwardServerNodeStore.SetMaxGbTotalLastBytes(0);
            }
            if (Node.MaxGbTotalMonth != DateTime.Now.Month)
            {
                sForwardServerNodeStore.SetMaxGbTotalMonth(DateTime.Now.Month);
                sForwardServerNodeStore.SetMaxGbTotalLastBytes((long)(Node.MaxGbTotal * 1024 * 1024 * 1024));
            }
            sForwardServerNodeStore.Confirm();
        }
        private async Task UploadBytes()
        {
            var cdkeys = trafficDict.Values.Where(c => c.CurrentCdkey != null && c.Sendt > 0).ToList();
            Dictionary<int, long> id2sent = cdkeys.GroupBy(c => c.CurrentCdkey.Id).ToDictionary(c => c.Key, d => d.Sum(d => { d.SendtCache = d.Sendt; return d.SendtCache; }));
            if (id2sent.Count == 0) return;

            bool result = await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = connection,
                MessengerId = (ushort)SForwardMessengerIds.TrafficReport,
                Payload = serializer.Serialize(new SForwardTrafficUpdateInfo
                {
                    Dic = id2sent,
                    SecretKey = Node.MasterSecretKey
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
                await Report();
            }, 5000);
        }
        private async Task Report()
        {
            try
            {
                double diff = (bytes - lastBytes) * 8 / 1024.0 / 1024.0;
                lastBytes = bytes;

                IPAddress address = await NetworkHelper.GetDomainIpAsync(Node.Host).ConfigureAwait(false) ?? IPAddress.Any;
                SForwardServerNodeReportInfo sForwardServerNodeReportInfo = new SForwardServerNodeReportInfo
                {
                    Id = Node.Id,
                    Name = Node.Name,
                    MaxBandwidth = Node.MaxBandwidth,
                    BandwidthRatio = Math.Round(diff / 5, 2),
                    MaxBandwidthTotal = Node.MaxBandwidthTotal,
                    MaxGbTotal = Node.MaxGbTotal,
                    MaxGbTotalLastBytes = Node.MaxGbTotalLastBytes,

                    Public = Node.Public,
                    Domain = Node.Domain,
                    Address = address,
                    Url = Node.Url,
                    Sync2Server = Node.Sync2Server,
                    Version = VersionHelper.Version,
                    WebPort = sForwardServerStore.WebPort,
                    PortRange = sForwardServerStore.TunnelPortRange
                };
                var resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = Connection,
                    MessengerId = (ushort)SForwardMessengerIds.NodeReport,
                    Payload = serializer.Serialize(sForwardServerNodeReportInfo)
                }).ConfigureAwait(false);
                if (Node.Sync2Server && resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
                {
                    string version = serializer.Deserialize<string>(resp.Data.Span);
                    if (version != VersionHelper.Version)
                    {
                        Helper.AppUpdate(version);
                    }
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error($"sforward report : {ex}");
                }
            }
        }

        private void SignInTask()
        {
            TimerHelper.SetIntervalLong(async () =>
            {
                if (Connection == null || Connection.Connected == false)
                {
                    connection = await SignIn(Node.MasterHost, Node.MasterSecretKey).ConfigureAwait(false);
                }
            }, 3000);
        }
        private async Task<IConnection> SignIn(string host, string secretKey)
        {
            byte[] bytes = ArrayPool<byte>.Shared.Rent(1024);
            try
            {
                byte[] secretKeyBytes = secretKey.Sha256().ToBytes();

                bytes[0] = (byte)secretKeyBytes.Length;
                secretKeyBytes.AsSpan().CopyTo(bytes.AsSpan(1));


                IPEndPoint remote = await NetworkHelper.GetEndPointAsync(host, 1802).ConfigureAwait(false);
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Warning($"sforward node sign in to {remote}");
                }

                Socket socket = new Socket(remote.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.KeepAlive();
                await socket.ConnectAsync(remote).WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);
                return await messengerResolver.BeginReceiveClient(socket, true, (byte)ResolverType.SForwardReport, bytes.AsMemory(0, secretKeyBytes.Length + 1)).ConfigureAwait(false);
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


    public sealed partial class SForwardCacheInfo
    {
        public ulong FlowId { get; set; }
        public bool Validated { get; set; }
        public List<SForwardCdkeyInfo> Cdkey { get; set; } = [];
    }
    public class SForwardSpeedLimit
    {
        private uint sforwardLimit = 0;
        private double sforwardLimitToken = 0;
        private double sforwardLimitBucket = 0;
        private long sforwardLimitTicks = Environment.TickCount64;

        public bool NeedLimit()
        {
            return sforwardLimit > 0;
        }
        public void SetLimit(uint bytes)
        {
            //每s多少字节
            sforwardLimit = bytes;
            //每ms多少字节
            sforwardLimitToken = sforwardLimit / 1000.0;
            //桶里有多少字节
            sforwardLimitBucket = sforwardLimit;
        }
        public bool TryLimit(ref int length)
        {
            //0不限速
            if (sforwardLimit == 0) return true;

            lock (this)
            {
                long _sforwardLimitTicks = Environment.TickCount64;
                //距离上次经过了多少ms
                long sforwardLimitTicksTemp = _sforwardLimitTicks - sforwardLimitTicks;
                sforwardLimitTicks = _sforwardLimitTicks;
                //桶里增加多少字节
                sforwardLimitBucket += sforwardLimitTicksTemp * sforwardLimitToken;
                //桶溢出了
                if (sforwardLimitBucket > sforwardLimit) sforwardLimitBucket = sforwardLimit;

                //能全部消耗调
                if (sforwardLimitBucket >= length)
                {
                    sforwardLimitBucket -= length;
                    length = 0;
                }
                else
                {
                    //只能消耗一部分
                    length -= (int)sforwardLimitBucket;
                    sforwardLimitBucket = 0;
                }
            }
            return true;
        }
        public bool TryLimitPacket(int length)
        {
            if (sforwardLimit == 0) return true;

            lock (this)
            {
                long _sforwardLimitTicks = Environment.TickCount64;
                long sforwardLimitTicksTemp = _sforwardLimitTicks - sforwardLimitTicks;
                sforwardLimitTicks = _sforwardLimitTicks;
                sforwardLimitBucket += sforwardLimitTicksTemp * sforwardLimitToken;
                if (sforwardLimitBucket > sforwardLimit) sforwardLimitBucket = sforwardLimit;

                if (sforwardLimitBucket >= length)
                {
                    sforwardLimitBucket -= length;
                    return true;
                }
            }
            return false;
        }
    }
    public sealed class SForwardTrafficCacheInfo
    {
        public long Sendt;
        public long SendtCache;
        public SForwardSpeedLimit Limit { get; set; }
        public SForwardCacheInfo Cache { get; set; }
        public SForwardCdkeyInfo CurrentCdkey { get; set; }
    }

    public sealed partial class SForwardTrafficUpdateInfo
    {
        /// <summary>
        /// cdkey id  和 流量
        /// </summary>
        public Dictionary<int, long> Dic { get; set; }
        public string SecretKey { get; set; }
    }

}
