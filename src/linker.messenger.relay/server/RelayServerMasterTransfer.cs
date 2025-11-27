
using linker.messenger.signin;

namespace linker.messenger.relay.server
{
    /// <summary>
    /// 中继主机操作
    /// </summary>
    public class RelayServerMasterTransfer
    {
        private readonly IRelayServerCaching relayCaching;
        private readonly IRelayServerWhiteListStore relayServerWhiteListStore;
        private readonly IRelayServerMasterStore relayServerMasterStore;

        public RelayServerMasterTransfer(IRelayServerCaching relayCaching, IRelayServerWhiteListStore relayServerWhiteListStore, IRelayServerMasterStore relayServerMasterStore)
        {
            this.relayCaching = relayCaching;
            this.relayServerWhiteListStore = relayServerWhiteListStore;
            this.relayServerMasterStore = relayServerMasterStore;

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
            if (relayCaching.TryGetValue(key, out RelayCacheInfo cache))
            {
                List<double> bandwidth = await relayServerWhiteListStore.GetBandwidth(cache.UserId, cache.FromId, cache.ToId, nodeid);
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
        public async Task<List<RelayNodeStoreInfo>> GetNodes(bool validated, string userid, string machineId)
        {
            var nodes = (await relayServerWhiteListStore.GetNodes(userid, machineId)).Where(c => c.Bandwidth >= 0).SelectMany(c => c.Nodes);

            var result = (await relayServerMasterStore.GetAll())
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
        public async Task<List<RelayNodeStoreInfo>> GetPublicNodes()
        {
            var result = (await relayServerMasterStore.GetAll())
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
