
using linker.messenger.signin;
using System.Collections.Concurrent;

namespace linker.messenger.relay.server
{
    /// <summary>
    /// 中继主机操作
    /// </summary>
    public class RelayServerMasterTransfer
    {

        private readonly ConcurrentDictionary<string, RelayServerNodeReportInfo> reports = new ConcurrentDictionary<string, RelayServerNodeReportInfo>();

        private readonly IRelayServerCaching relayCaching;
        private readonly IRelayServerWhiteListStore relayServerWhiteListStore;

        public RelayServerMasterTransfer(IRelayServerCaching relayCaching, IRelayServerWhiteListStore relayServerWhiteListStore)
        {
            this.relayCaching = relayCaching;
            this.relayServerWhiteListStore = relayServerWhiteListStore;

        }

        public bool AddRelay(SignCacheInfo from, SignCacheInfo to, uint flowid)
        {
            RelayCacheInfo cache = new RelayCacheInfo
            {
                FlowId = flowid,
                FromId = from.MachineId,
                FromName = from.MachineName,
                ToId = to.Id,
                ToName = to.MachineName,
                GroupId = to.GroupId,
                Super = from.Super,
                UserId = from.UserId,
            };
            return relayCaching.TryAdd($"{cache.FromId}->{cache.ToId}->{flowid}", cache, 15000);
        }

        public async Task<RelayCacheInfo> TryGetRelayCache(string key, string nodeid)
        {
            if (relayCaching.TryGetValue(key, out RelayCacheInfo cache) && reports.TryGetValue(nodeid, out var node))
            {
                List<double> bandwidth = await relayServerWhiteListStore.GetBandwidth(cache.UserId, cache.FromId, cache.ToId, node.Id);
                if (bandwidth.Any(c => c < 0))
                {
                    return null;
                }

                cache.Bandwidth = bandwidth.Count > 0
                ? bandwidth.Any(c => c == 0) ? 0 : bandwidth.Min()
                : cache.Super ? 0 : -1;

                return cache;
            }
            return null;
        }
        /// <summary>
        /// 获取节点列表
        /// </summary>
        /// <param name="validated">是否已认证</param>
        /// <returns></returns>
        public async Task<List<RelayServerNodeReportInfo>> GetNodes(bool validated, string userid, string machineId)
        {
            var nodes = (await relayServerWhiteListStore.GetNodes(userid, machineId)).Where(c => c.Bandwidth >= 0).SelectMany(c => c.Nodes);

            var result = reports.Values
                .Where(c => Environment.TickCount64 - c.LastTicks < 15000)
                .Where(c =>
                {
                    return validated || nodes.Contains(c.Id) || nodes.Contains("*")
                    || (c.Public && c.ConnectionRatio < c.MaxConnection && (c.MaxGbTotal == 0 || (c.MaxGbTotal > 0 && c.MaxGbTotalLastBytes > 0)));
                })
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
        public List<RelayServerNodeReportInfo> GetPublicNodes()
        {
            var result = reports.Values
                .Where(c => Environment.TickCount64 - c.LastTicks < 15000)
                .Where(c => c.Public)
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

    }

    public sealed partial class RelayCacheInfo
    {
        public ulong FlowId { get; set; }
        public string FromId { get; set; }
        public string FromName { get; set; }
        public string ToId { get; set; }
        public string ToName { get; set; }
        public string GroupId { get; set; }
        public bool Super { get; set; }
        public double Bandwidth { get; set; }

        public string UserId { get; set; } = string.Empty;
    }
}
