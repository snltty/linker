using linker.libs;
using linker.libs.timer;
using linker.libs.winapis;
using linker.messenger.cdkey;
using linker.messenger.relay.messenger;
using linker.messenger.relay.server.caching;
using linker.messenger.wlist;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;

namespace linker.messenger.relay.server
{
    /// <summary>
    /// 中继主机操作
    /// </summary>
    public class RelayServerMasterTransfer
    {

        private ulong relayFlowingId = 0;
        private readonly ConcurrentDictionary<string, RelayServerNodeReportInfo188> reports = new ConcurrentDictionary<string, RelayServerNodeReportInfo188>();
        private readonly ConcurrentQueue<Dictionary<int, long>> trafficQueue = new ConcurrentQueue<Dictionary<int, long>>();
        private readonly ConcurrentQueue<List<int>> trafficIdsQueue = new ConcurrentQueue<List<int>>();

        private readonly IRelayServerCaching relayCaching;
        private readonly ISerializer serializer;
        private readonly IRelayServerMasterStore relayServerMasterStore;
        private readonly ICdkeyServerStore relayServerCdkeyStore;
        private readonly IMessengerSender messengerSender;
        private readonly IWhiteListServerStore whiteListServerStore;
        private readonly ICdkeyServerStore cdkeyServerStore;

        public RelayServerMasterTransfer(IRelayServerCaching relayCaching, ISerializer serializer, IRelayServerMasterStore relayServerMasterStore, ICdkeyServerStore relayServerCdkeyStore, IMessengerSender messengerSender, IWhiteListServerStore whiteListServerStore, ICdkeyServerStore cdkeyServerStore)
        {
            this.relayCaching = relayCaching;
            this.serializer = serializer;
            this.relayServerMasterStore = relayServerMasterStore;
            this.relayServerCdkeyStore = relayServerCdkeyStore;
            this.messengerSender = messengerSender;
            this.whiteListServerStore = whiteListServerStore;
            this.cdkeyServerStore = cdkeyServerStore;
            TrafficTask();
        }


        public ulong AddRelay(string fromid, string fromName, string toid, string toName, string groupid, string userid, bool validated, bool useCdkey)
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
                UserId = userid,
                Validated = validated,
                UseCdkey = useCdkey,
            };
            bool added = relayCaching.TryAdd($"{fromid}->{toid}->{flowingId}", cache, 15000);
            if (added == false) return 0;

            return flowingId;
        }

        public async Task<RelayCacheInfo> TryGetRelayCache(string key, string nodeid)
        {
            if (relayCaching.TryGetValue(key, out RelayCacheInfo value))
            {
                var nodes = await whiteListServerStore.Get("Relay", value.UserId).ConfigureAwait(false);
                value.Validated = value.Validated || nodes.Contains(nodeid);
                value.Cdkey = value.UseCdkey
                    ? (await cdkeyServerStore.GetAvailable(value.UserId, "Relay").ConfigureAwait(false)).Select(c => new CdkeyInfo { Bandwidth = c.Bandwidth, Id = c.Id, LastBytes = c.LastBytes }).ToList()
                    : [];
            }
            return value;
        }
        public void SetNodeReport(IConnection connection, RelayServerNodeReportInfo170 info)
        {
            SetNodeReport(connection, new RelayServerNodeReportInfo188
            {
                Version = string.Empty,
                Sync2Server = false,
                Id = info.Id,
                Name = info.Name,
                MaxConnection = info.MaxConnection,
                MaxBandwidth = info.MaxBandwidth,
                MaxBandwidthTotal = info.MaxBandwidthTotal,
                MaxGbTotal = info.MaxGbTotal,
                MaxGbTotalLastBytes = info.MaxGbTotalLastBytes,
                ConnectionRatio = info.ConnectionRatio,
                BandwidthRatio = info.BandwidthRatio,
                Public = info.Public,
                EndPoint = info.EndPoint,
                LastTicks = info.LastTicks,
                Url = info.Url,
                Connection = connection

            });
        }
        public void SetNodeReport(IConnection connection, RelayServerNodeReportInfo188 info)
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
        public async Task Edit(RelayServerNodeUpdateInfo info)
        {
            if (reports.TryGetValue(info.Id, out RelayServerNodeReportInfo188 cache))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)RelayMessengerIds.Edit,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
            }
        }
        public async Task Edit(RelayServerNodeUpdateInfo188 info)
        {
            if (reports.TryGetValue(info.Id, out RelayServerNodeReportInfo188 cache))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)RelayMessengerIds.Edit188,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
            }
        }
        public async Task Exit(string id)
        {
            if (reports.TryGetValue(id, out RelayServerNodeReportInfo188 cache))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)RelayMessengerIds.Exit,
                }).ConfigureAwait(false);
            }
        }
        public async Task Update(string id, string version)
        {
            if (reports.TryGetValue(id, out RelayServerNodeReportInfo188 cache))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)RelayMessengerIds.Update,
                    Payload = serializer.Serialize(version)
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 获取节点列表
        /// </summary>
        /// <param name="validated">是否已认证</param>
        /// <returns></returns>
        public async Task<List<RelayServerNodeReportInfo188>> GetNodes(bool validated, string userid)
        {
            var nodes = await whiteListServerStore.Get("Relay", userid);

            var result = reports.Values
                .Where(c => Environment.TickCount64 - c.LastTicks < 15000)
                .Where(c =>
                {
                    return validated || nodes.Contains(c.Id)
                    || (c.Public && (c.ConnectionRatio < c.MaxConnection && (c.MaxGbTotal == 0 || (c.MaxGbTotal > 0 && c.MaxGbTotalLastBytes > 0))));
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

        /// <summary>
        /// 消耗流量
        /// </summary>
        /// <param name="relayTrafficUpdateInfo"></param>
        /// <returns></returns>
        public void AddTraffic(RelayTrafficUpdateInfo info)
        {
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
