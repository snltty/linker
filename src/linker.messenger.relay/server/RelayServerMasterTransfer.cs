using linker.libs;
using linker.messenger.relay.server.caching;
using System.Collections.Concurrent;
using System.Net;

namespace linker.messenger.relay.server
{
    /// <summary>
    /// 中继主机操作
    /// </summary>
    public class RelayServerMasterTransfer
    {

        private ulong relayFlowingId = 0;
        private readonly ConcurrentDictionary<string, RelayServerNodeReportInfo> reports = new ConcurrentDictionary<string, RelayServerNodeReportInfo>();
        private readonly ConcurrentQueue<Dictionary<long, long>> trafficQueue = new ConcurrentQueue<Dictionary<long, long>>();

        private readonly IRelayServerCaching relayCaching;
        private readonly ISerializer serializer;
        private readonly IRelayServerCdkeyStore relayServerCdkeyStore;
        public RelayServerMasterTransfer(IRelayServerCaching relayCaching, ISerializer serializer, IRelayServerMasterStore relayServerMasterStore, IRelayServerCdkeyStore relayServerCdkeyStore)
        {
            this.relayCaching = relayCaching;
            this.serializer = serializer;
            this.relayServerCdkeyStore = relayServerCdkeyStore;
            TrafficTask();
        }


        public ulong AddRelay(string fromid, string fromName, string toid, string toName, string groupid, bool validated, List<RelayServerCdkeyInfo> cdkeys)
        {
            ulong flowingId = Interlocked.Increment(ref relayFlowingId);

            RelayCacheInfo cache = new RelayCacheInfo
            {
                FlowId = flowingId,
                FromId = fromid,
                FromName = fromName,
                ToId = toid,
                ToName = toName,
                GroupId = groupid,
                Validated = validated,
                Cdkey = cdkeys
            };
            bool added = relayCaching.TryAdd($"{fromid}->{toid}->{flowingId}", cache, 15000);
            if (added == false) return 0;

            return flowingId;
        }

        public bool TryGetRelayCache(string key, out RelayCacheInfo value)
        {
            return relayCaching.TryGetValue(key, out value);
        }
        /// <summary>
        /// 设置节点
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="data"></param>
        public void SetNodeReport(IConnection connection, RelayServerNodeReportInfo info)
        {
            try
            {
                if (info.Id == RelayServerNodeInfo.MASTER_NODE_ID)
                {
                    info.EndPoint = new IPEndPoint(IPAddress.Any, 0);
                }
                else if (info.EndPoint.Address.Equals(IPAddress.Any))
                {
                    info.EndPoint.Address = connection.Address.Address;
                }
                info.LastTicks = Environment.TickCount64;
                reports.AddOrUpdate(info.Id, info, (a, b) => info);
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
        }
        /// <summary>
        /// 获取节点列表
        /// </summary>
        /// <param name="validated">是否已认证</param>
        /// <returns></returns>
        public List<RelayServerNodeReportInfo> GetNodes(bool validated)
        {
            var result = reports.Values
                .Where(c => c.Public || validated)
                .Where(c => Environment.TickCount64 - c.LastTicks < 15000)
                .Where(c => c.ConnectionRatio < 100 && (c.MaxGbTotal == 0 || (c.MaxGbTotal > 0 && c.MaxGbTotalLastBytes > 0)))
                .OrderByDescending(c => c.LastTicks);

            return result.OrderByDescending(x => x.MaxConnection == 0 ? int.MaxValue : x.MaxConnection)
                     .ThenBy(x => x.ConnectionRatio)
                     .ThenBy(x => x.BandwidthRatio)
                     .ThenByDescending(x => x.MaxBandwidth == 0 ? double.MaxValue : x.MaxBandwidth)
                     .ThenByDescending(x => x.MaxBandwidthTotal == 0 ? double.MaxValue : x.MaxBandwidthTotal)
                     .ThenByDescending(x => x.MaxGbTotal == 0 ? double.MaxValue : x.MaxGbTotal)
                     .ThenByDescending(x => x.MaxGbTotalLastBytes == 0 ? long.MaxValue : x.MaxGbTotalLastBytes)
                     .ToList();
        }

        /// <summary>
        /// 是否需要认证
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public bool NodeValidate(string nodeId)
        {
            return reports.TryGetValue(nodeId, out RelayServerNodeReportInfo relayNodeReportInfo) && relayNodeReportInfo.Public == false;
        }


        public async Task<Dictionary<long, long>> AddTraffic(RelayTrafficUpdateInfo relayTrafficUpdateInfo)
        {
            if (relayTrafficUpdateInfo.Dic.Count > 0)
                trafficQueue.Enqueue(relayTrafficUpdateInfo.Dic);

            if (relayTrafficUpdateInfo.Ids == null || relayTrafficUpdateInfo.Ids.Count == 0)
            {
                return new Dictionary<long, long>();
            }
            return (await relayServerCdkeyStore.Get(relayTrafficUpdateInfo.Ids)).ToDictionary(c => c.CdkeyId, c => c.LastBytes);
        }
        private void TrafficTask()
        {
            TimerHelper.SetIntervalLong(async () =>
            {
                while (trafficQueue.TryDequeue(out Dictionary<long, long> dic))
                {
                    await relayServerCdkeyStore.Traffic(dic).ConfigureAwait(false);
                }
                return true;
            },3000);
        }
    }
}
