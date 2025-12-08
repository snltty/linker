using linker.libs;
using linker.libs.timer;
using linker.messenger.sforward.messenger;
using System.Collections.Concurrent;

namespace linker.messenger.sforward.server
{
    /// <summary>
    /// 穿透节点操作
    /// </summary>
    public class SForwardServerNodeTransfer
    {
        public SForwardServerConfigInfo Config => sForwardServerConfigStore.Config;


        private readonly NumberSpace ns = new(65537);
        private readonly SForwardSpeedLimit limitTotal = new();
        private readonly ConcurrentDictionary<ulong, SForwardTrafficCacheInfo> trafficDict = new();

        private readonly ISerializer serializer;
        private readonly IMessengerSender messengerSender;

        private readonly ISForwardServerConfigStore sForwardServerConfigStore;
        private readonly SForwardServerConnectionTransfer sForwardServerConnectionTransfer;
        private readonly SForwardServerNodeReportTransfer sForwardServerNodeReportTransfer;

        public SForwardServerNodeTransfer(ISerializer serializer, IMessengerSender messengerSender, ICommonStore commonStore
            , ISForwardServerConfigStore sForwardServerConfigStore, SForwardServerConnectionTransfer sForwardServerConnectionTransfer, SForwardServerNodeReportTransfer sForwardServerNodeReportTransfer)
        {
            this.serializer = serializer;
            this.messengerSender = messengerSender;
            this.sForwardServerConfigStore = sForwardServerConfigStore;
            this.sForwardServerConnectionTransfer = sForwardServerConnectionTransfer;
            this.sForwardServerNodeReportTransfer = sForwardServerNodeReportTransfer;

            if ((commonStore.Modes & CommonModes.Server) == CommonModes.Server)
            {
                limitTotal.SetLimit((uint)Math.Ceiling((Config.Bandwidth * 1024 * 1024) / 8.0));
                TrafficTask();
            }
        }
        public async Task<bool> ProxyNode(SForwardProxyInfo info)
        {
            if (sForwardServerConnectionTransfer.TryGet(ConnectionSideType.Master, info.NodeId, out var connection))
            {
                return await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = (ushort)SForwardMessengerIds.ProxyForward,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
            }
            return false;
        }
        public async Task<List<string>> Heart(List<string> ids, string masterNodeId)
        {
            if (sForwardServerConnectionTransfer.TryGet(ConnectionSideType.Master, masterNodeId, out var connection))
            {
                var resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = (ushort)SForwardMessengerIds.Heart,
                    Payload = serializer.Serialize(ids)
                }).ConfigureAwait(false);

                if (resp.Code == MessageResponeCodes.OK)
                {
                    return serializer.Deserialize<List<string>>(resp.Data.Span);
                }

            }

            return [];
        }

        /// <summary>
        /// 是否需要总限速
        /// </summary>
        /// <returns></returns>
        public bool NeedLimit(SForwardTrafficCacheInfo relayCache)
        {
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
        /// <param name="super"></param>
        /// <returns></returns>
        public SForwardTrafficCacheInfo AddTrafficCache(bool super, double bandwidth)
        {
            SForwardTrafficCacheInfo cache = new SForwardTrafficCacheInfo { Cache = new SForwardCacheInfo { FlowId = ns.Increment(), Super = super, Bandwidth = bandwidth }, Limit = new SForwardSpeedLimit(), Sendt = 0, SendtCache = 0 };
            if (cache.Cache.Bandwidth < 0)
            {
                cache.Cache.Bandwidth = Config.Bandwidth;
            }
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
            sForwardServerNodeReportTransfer.AddBytes(length);

            if (Config.DataEachMonth == 0) return true;

            Interlocked.Add(ref cache.Sendt, length);

            return Config.DataRemain > 0;
        }

        /// <summary>
        /// 设置限速
        /// </summary>
        /// <param name="cache"></param>
        private void SetLimit(SForwardTrafficCacheInfo cache)
        {
            if (cache.Cache.Bandwidth >= 0)
            {
                cache.Limit.SetLimit((uint)Math.Ceiling(cache.Cache.Bandwidth * 1024 * 1024 / 8.0));
                return;
            }

            cache.Limit.SetLimit((uint)Math.Ceiling(Config.Bandwidth * 1024 * 1024 / 8.0));
        }


        private void ResetNodeBytes()
        {
            if (Config.DataEachMonth == 0) return;

            foreach (var cache in trafficDict.Values)
            {
                long length = Interlocked.Exchange(ref cache.Sendt, 0);

                if (Config.DataRemain >= length)
                    sForwardServerConfigStore.SetDataRemain(Config.DataRemain - length);
                else sForwardServerConfigStore.SetDataRemain(0);
            }
            if (Config.DataMonth != DateTime.Now.Month)
            {
                sForwardServerConfigStore.SetDataMonth(DateTime.Now.Month);
                sForwardServerConfigStore.SetDataRemain((long)(Config.DataEachMonth * 1024 * 1024 * 1024));
            }
            sForwardServerConfigStore.Confirm();
        }
        private void TrafficTask()
        {
            TimerHelper.SetIntervalLong(() =>
            {
                try
                {
                    ResetNodeBytes();
                }
                catch (Exception ex)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }, 3000);
        }

    }

    public sealed partial class SForwardCacheInfo
    {
        public ulong FlowId { get; set; }
        public bool Super { get; set; }
        public double Bandwidth { get; set; } = double.MinValue;
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
    }
}
