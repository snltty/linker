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
        private readonly ICrypto crypto;
        private readonly ConcurrentDictionary<string, RelayServerNodeReportInfo> reports = new ConcurrentDictionary<string, RelayServerNodeReportInfo>();


        private readonly IRelayServerCaching relayCaching;
        private readonly ISerializer serializer;
        public RelayServerMasterTransfer(IRelayServerCaching relayCaching, ISerializer serializer, IRelayServerMasterStore relayServerMasterStore)
        {
            this.relayCaching = relayCaching;
            this.serializer = serializer;
            crypto = CryptoFactory.CreateSymmetric(relayServerMasterStore.Master.SecretKey);
        }


        public ulong AddRelay(string fromid, string fromName, string toid, string toName, string groupid, List<RelayServerCdkeyInfo> cdkeys)
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
                Cdkey = cdkeys
            };
            bool added = relayCaching.TryAdd($"{fromid}->{toid}->{flowingId}", cache, 15000);
            if (added == false) return 0;

            return flowingId;
        }

        public Memory<byte> TryGetRelayCache(string key)
        {
            if (relayCaching.TryGetValue(key, out RelayCacheInfo value))
            {
                byte[] bytes = crypto.Encode(serializer.Serialize(value));
                return bytes;
            }
            return Helper.EmptyArray;
        }

        /// <summary>
        /// 设置节点
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="data"></param>
        public void SetNodeReport(IPEndPoint ep, Memory<byte> data)
        {
            try
            {
                if (crypto == null) return;
                data = crypto.Decode(data.ToArray());
                RelayServerNodeReportInfo relayNodeReportInfo = serializer.Deserialize<RelayServerNodeReportInfo>(data.Span);

                if (relayNodeReportInfo.Id == RelayServerNodeInfo.MASTER_NODE_ID)
                {
                    relayNodeReportInfo.EndPoint = new IPEndPoint(IPAddress.Any, 0);
                }
                else if (relayNodeReportInfo.EndPoint.Address.Equals(IPAddress.Any))
                {
                    relayNodeReportInfo.EndPoint.Address = ep.Address;
                }
                relayNodeReportInfo.LastTicks = Environment.TickCount64;
                reports.AddOrUpdate(relayNodeReportInfo.Id, relayNodeReportInfo, (a, b) => relayNodeReportInfo);
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
                .Where(c => c.Public || (c.Public == false && validated))
                .Where(c => Environment.TickCount64 - c.LastTicks < 15000)
                .Where(c => c.ConnectionRatio < 100 && (c.MaxGbTotal == 0 || (c.MaxGbTotal > 0 && c.MaxGbTotalLastBytes > 0)))
                .OrderByDescending(c => c.LastTicks);

            return result.OrderByDescending(x => x.MaxConnection == 0 ? int.MaxValue : x.MaxConnection)
                     .ThenBy(x => x.ConnectionRatio)
                     .ThenBy(x => x.BandwidthRatio)
                     .ThenByDescending(x => x.MaxBandwidth == 0 ? double.MaxValue : x.MaxBandwidth)
                     .ThenByDescending(x => x.MaxBandwidthTotal == 0 ? double.MaxValue : x.MaxBandwidthTotal)
                     .ThenByDescending(x => x.MaxGbTotal == 0 ? double.MaxValue : x.MaxGbTotal)
                     .ThenByDescending(x => x.MaxGbTotalLastBytes == 0 ? ulong.MaxValue : x.MaxGbTotalLastBytes)
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
    }


}
