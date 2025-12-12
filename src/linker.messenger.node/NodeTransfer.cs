using linker.libs;
using linker.libs.timer;
using System.Collections.Concurrent;

namespace linker.messenger.node
{
    /// <summary>
    /// 节点操作
    /// </summary>
    public class NodeTransfer<TConfig, TStore, TReport>
         where TConfig : class, INodeConfigBase, new()
        where TStore : class, INodeStoreBase, new()
        where TReport : class, INodeReportBase, new()
    {
        public TConfig Config => nodeConfigStore.Config;

        private readonly NumberSpace ns = new(65537);
        private readonly SpeedLimit limitTotal = new();
        private readonly ConcurrentDictionary<ulong, TrafficCacheInfo> trafficDict = new();

        private readonly INodeConfigStore<TConfig> nodeConfigStore;
        private readonly NodeReportTransfer<TConfig, TStore, TReport> nodeReportTransfer;

        public NodeTransfer(ICommonStore commonStore, INodeConfigStore<TConfig> nodeConfigStore, NodeReportTransfer<TConfig, TStore, TReport> nodeReportTransfer)
        {
            this.nodeConfigStore = nodeConfigStore;
            this.nodeReportTransfer = nodeReportTransfer;

            if ((commonStore.Modes & CommonModes.Server) == CommonModes.Server)
            {
                limitTotal.SetLimit((uint)Math.Ceiling((Config.Bandwidth * 1024 * 1024) / 8.0));
                TrafficTask();
            }
        }
        /// <summary>
        /// 增加连接数
        /// </summary>
        public void IncrementConnectionNum()
        {
            nodeReportTransfer.IncrementConnectionNum();
        }
        /// <summary>
        /// 减少连接数
        /// </summary>
        public void DecrementConnectionNum()
        {
            nodeReportTransfer.DecrementConnectionNum();
        }

        /// <summary>
        /// 是否需要总限速
        /// </summary>
        /// <returns></returns>
        public bool NeedLimit(TrafficCacheInfo cache)
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
        public TrafficCacheInfo AddTrafficCache(bool super, double bandwidth)
        {
            TrafficCacheInfo cache = new TrafficCacheInfo { Cache = new CacheInfo { FlowId = ns.Increment(), Super = super, Bandwidth = bandwidth }, Limit = new SpeedLimit(), Sendt = 0, SendtCache = 0 };
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
        /// 开始计算流量
        /// </summary>
        /// <param name="cache"></param>
        public void AddTrafficCache(TrafficCacheInfo cache)
        {
            SetLimit(cache);
            trafficDict.TryAdd(cache.Cache.FlowId, cache);
        }
        /// <summary>
        /// 取消计算流量
        /// </summary>
        /// <param name="cache"></param>
        public void RemoveTrafficCache(TrafficCacheInfo cache)
        {
            trafficDict.TryRemove(cache.Cache.FlowId, out _);
        }

        /// <summary>
        /// 消耗流量
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public bool AddBytes(TrafficCacheInfo cache, long length)
        {
            nodeReportTransfer.AddBytes(length);

            if (Config.DataEachMonth == 0) return true;

            Interlocked.Add(ref cache.Sendt, length);

            return Config.DataRemain > 0;
        }

        /// <summary>
        /// 设置限速
        /// </summary>
        /// <param name="cache"></param>
        private void SetLimit(TrafficCacheInfo cache)
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
                    nodeConfigStore.SetDataRemain(Config.DataRemain - length);
                else nodeConfigStore.SetDataRemain(0);
            }
            if (Config.DataMonth != DateTime.Now.Month)
            {
                nodeConfigStore.SetDataMonth(DateTime.Now.Month);
                nodeConfigStore.SetDataRemain((long)(Config.DataEachMonth * 1024 * 1024 * 1024));
            }
            nodeConfigStore.Confirm();
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

    public class CacheInfo
    {
        public ulong FlowId { get; set; }
        public bool Super { get; set; }
        public double Bandwidth { get; set; } = double.MinValue;
    }
    public class SpeedLimit
    {
        private uint limit = 0;
        private double limitToken = 0;
        private double limitBucket = 0;
        private long limitTicks = Environment.TickCount64;

        public bool NeedLimit()
        {
            return limit > 0;
        }
        public void SetLimit(uint bytes)
        {
            //每s多少字节
            limit = bytes;
            //每ms多少字节
            limitToken = limit / 1000.0;
            //桶里有多少字节
            limitBucket = limit;
        }
        public bool TryLimit(ref int length)
        {
            //0不限速
            if (limit == 0) return true;

            lock (this)
            {
                long _limitTicks = Environment.TickCount64;
                //距离上次经过了多少ms
                long limitTicksTemp = _limitTicks - limitTicks;
                limitTicks = _limitTicks;
                //桶里增加多少字节
                limitBucket += limitTicksTemp * limitToken;
                //桶溢出了
                if (limitBucket > limit) limitBucket = limit;

                //能全部消耗调
                if (limitBucket >= length)
                {
                    limitBucket -= length;
                    length = 0;
                }
                else
                {
                    //只能消耗一部分
                    length -= (int)limitBucket;
                    limitBucket = 0;
                }
            }
            return true;
        }
        public bool TryLimitPacket(int length)
        {
            if (limit == 0) return true;

            lock (this)
            {
                long _limitTicks = Environment.TickCount64;
                long limitTicksTemp = _limitTicks - limitTicks;
                limitTicks = _limitTicks;
                limitBucket += limitTicksTemp * limitToken;
                if (limitBucket > limit) limitBucket = limit;

                if (limitBucket >= length)
                {
                    limitBucket -= length;
                    return true;
                }
            }
            return false;
        }
    }
    public class TrafficCacheInfo
    {
        public long Sendt;
        public long SendtCache;
        public SpeedLimit Limit { get; set; }
        public CacheInfo Cache { get; set; }
        public string Key { get; set; }
    }
}
