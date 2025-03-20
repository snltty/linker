using linker.libs;
using linker.libs.extends;
using linker.libs.timer;
using linker.messenger.relay.messenger;
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
        private readonly ConcurrentDictionary<string, RelayServerNodeReportInfo170> reports = new ConcurrentDictionary<string, RelayServerNodeReportInfo170>();
        private readonly ConcurrentQueue<Dictionary<int, long>> trafficQueue = new ConcurrentQueue<Dictionary<int, long>>();
        private readonly ConcurrentQueue<List<int>> trafficIdsQueue = new ConcurrentQueue<List<int>>();

        private readonly IRelayServerCaching relayCaching;
        private readonly ISerializer serializer;
        private readonly IRelayServerMasterStore relayServerMasterStore;
        private readonly IRelayServerCdkeyStore relayServerCdkeyStore;
        private readonly IMessengerSender messengerSender;


        public RelayServerMasterTransfer(IRelayServerCaching relayCaching, ISerializer serializer, IRelayServerMasterStore relayServerMasterStore, IRelayServerCdkeyStore relayServerCdkeyStore, IMessengerSender messengerSender)
        {
            this.relayCaching = relayCaching;
            this.serializer = serializer;
            this.relayServerMasterStore = relayServerMasterStore;
            this.relayServerCdkeyStore = relayServerCdkeyStore;
            this.messengerSender = messengerSender;
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
        public void SetNodeReport(IConnection connection, RelayServerNodeReportInfo170 info)
        {
            try
            {
                if (info.EndPoint.Address.Equals(IPAddress.Any))
                {
                    info.EndPoint.Address = connection.Address.Address;
                }
                if (info.EndPoint.Address.Equals(IPAddress.Loopback))
                {
                    info.EndPoint = new IPEndPoint(IPAddress.Any, 0);
                }
                info.LastTicks = Environment.TickCount64;
                info.Connection = connection;
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
        /// 更新节点
        /// </summary>
        /// <param name="info"></param>
        public async Task UpdateNodeReport(RelayServerNodeUpdateInfo info)
        {
            if (reports.TryGetValue(info.Id, out RelayServerNodeReportInfo170 cache))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)RelayMessengerIds.UpdateNode,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 获取节点列表
        /// </summary>
        /// <param name="validated">是否已认证</param>
        /// <returns></returns>
        public List<RelayServerNodeReportInfo170> GetNodes(bool validated)
        {
            var result = reports.Values
                .Where(c => c.Public || validated)
                .Where(c => Environment.TickCount64 - c.LastTicks < 15000)
                .Where(c => validated || (c.ConnectionRatio < c.MaxConnection && (c.MaxGbTotal == 0 || (c.MaxGbTotal > 0 && c.MaxGbTotalLastBytes > 0))))
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
            return reports.TryGetValue(nodeId, out RelayServerNodeReportInfo170 relayNodeReportInfo) && relayNodeReportInfo.Public == false;
        }

        /// <summary>
        /// 消耗流量
        /// </summary>
        /// <param name="relayTrafficUpdateInfo"></param>
        /// <returns></returns>
        public void AddTraffic(RelayTrafficUpdateInfo info)
        {
            if (info.SecretKey != relayServerMasterStore.Master.SecretKey) return;
            if (info.Dic.Count == 0) return;

            trafficQueue.Enqueue(info.Dic);
        }
        private void TrafficTask()
        {
            TimerHelper.SetIntervalLong(async () =>
            {
                while (trafficQueue.TryDequeue(out Dictionary<int, long> dic))
                {
                    try
                    {
                        await relayServerCdkeyStore.Traffic(dic).ConfigureAwait(false);

                        var ids = dic.Keys.ToList();
                        while (ids.Count > 0)
                        {
                            var id = ids.Take(100).ToList();
                            trafficIdsQueue.Enqueue(id);
                            ids.RemoveRange(0, id.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                }
            }, 500);
            TimerHelper.SetIntervalLong(async () =>
            {
                while (trafficIdsQueue.TryDequeue(out List<int> ids))
                {
                    try
                    {
                        Dictionary<int, long> id2last = await relayServerCdkeyStore.GetLastBytes(ids).ConfigureAwait(false);
                        if (id2last.Count == 0) continue;
                        byte[] bytes = serializer.Serialize(id2last);

                        await Task.WhenAll(reports.Values.Select(c => messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = c.Connection,
                            MessengerId = (ushort)RelayMessengerIds.SendLastBytes,
                            Payload = bytes,
                        })).ToList()).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                }
            }, 500);

        }
    }
}
