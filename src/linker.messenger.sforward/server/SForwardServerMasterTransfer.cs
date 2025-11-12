using linker.libs;
using linker.libs.timer;
using linker.messenger.sforward.messenger;
using linker.messenger.signin;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;

namespace linker.messenger.sforward.server
{
    /// <summary>
    /// 穿透主机操作
    /// </summary>
    public class SForwardServerMasterTransfer
    {
        private readonly ConcurrentDictionary<string, SForwardServerNodeReportInfo> reports = new ConcurrentDictionary<string, SForwardServerNodeReportInfo>();

        private readonly ConcurrentQueue<Dictionary<int, long>> trafficQueue = new ConcurrentQueue<Dictionary<int, long>>();
        private readonly ConcurrentQueue<List<int>> trafficIdsQueue = new ConcurrentQueue<List<int>>();

        private readonly ISerializer serializer;
        private readonly IMessengerSender messengerSender;
        private readonly ISForwardServerWhiteListStore sForwardServerWhiteListStore;
        private readonly ISForwardServerNodeStore sForwardServerNodeStore;

        public SForwardServerMasterTransfer(ISerializer serializer, IMessengerSender messengerSender, ISForwardServerWhiteListStore sForwardServerWhiteListStore,
            ISForwardServerNodeStore sForwardServerNodeStore)
        {
            this.serializer = serializer;
            this.messengerSender = messengerSender;
            this.sForwardServerWhiteListStore = sForwardServerWhiteListStore;
            this.sForwardServerNodeStore = sForwardServerNodeStore;

        }

        public void SetNodeReport(IConnection connection, SForwardServerNodeReportInfo info)
        {
            try
            {
                if (info.Address.Equals(IPAddress.Any))
                {
                    info.Address = connection.Address.Address;
                }
                if (info.Address.Equals(IPAddress.Loopback))
                {
                    info.Address = IPAddress.Any;
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
        public async Task Edit(SForwardServerNodeUpdateInfo info)
        {

            if (reports.TryGetValue(info.Id, out SForwardServerNodeReportInfo cache))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)SForwardMessengerIds.Edit,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
            }
        }
        public async Task Exit(string id)
        {
            if (reports.TryGetValue(id, out SForwardServerNodeReportInfo cache))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)SForwardMessengerIds.Exit,
                }).ConfigureAwait(false);
            }
        }
        public async Task Update(string id, string version)
        {
            if (reports.TryGetValue(id, out SForwardServerNodeReportInfo cache))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)SForwardMessengerIds.Update,
                    Payload = serializer.Serialize(version)
                }).ConfigureAwait(false);
            }
        }

        public async Task<SForwardAddResultInfo> Add(SForwardAddInfo191 info, SignCacheInfo from)
        {
            if (string.IsNullOrWhiteSpace(info.NodeId)) info.NodeId = sForwardServerNodeStore.Node.Id;
            if (GetNode(info.NodeId, out var node) == false)
            {
                return new SForwardAddResultInfo
                {
                    BufferSize = 1,
                    Message = "node not found",
                    Success = false
                };
            }

            List<SForwardWhiteListItem> sforward = await sForwardServerWhiteListStore.GetNodes(from.UserId, from.MachineId);
            string target = string.IsNullOrWhiteSpace(info.Domain) ? info.RemotePort.ToString() : info.Domain;
            info.Super = from.Super;

            var bandwidth = sforward.Where(c => (c.Nodes.Contains($"sfp->{target}") || c.Nodes.Contains($"sfp->*")) && (c.Nodes.Contains(info.NodeId) || c.Nodes.Contains($"*"))).ToList();
            if (bandwidth.Any(c => c.Bandwidth < 0))
            {
                return new SForwardAddResultInfo
                {
                    BufferSize = 1,
                    Message = "white list deney",
                    Success = false
                };
            }

            info.Bandwidth = bandwidth.Count > 0
                ? bandwidth.Any(c => c.Bandwidth == 0) ? 0 : bandwidth.Max(c => c.Bandwidth)
                : info.Super ? 0 : node.MaxBandwidth;


            if (reports.TryGetValue(info.NodeId, out SForwardServerNodeReportInfo cache))
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)SForwardMessengerIds.Add191,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
                if (resp.Code == MessageResponeCodes.OK)
                {
                    return serializer.Deserialize<SForwardAddResultInfo>(resp.Data.Span);
                }
            }
            return new SForwardAddResultInfo
            {
                BufferSize = 1,
                Message = "node not found",
                Success = false
            };
        }
        public async Task<SForwardAddResultInfo> Remove(SForwardAddInfo191 info)
        {
            if (string.IsNullOrWhiteSpace(info.NodeId)) info.NodeId = sForwardServerNodeStore.Node.Id;

            if (reports.TryGetValue(info.NodeId, out SForwardServerNodeReportInfo cache))
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)SForwardMessengerIds.Remove191,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
                if (resp.Code == MessageResponeCodes.OK)
                {
                    return serializer.Deserialize<SForwardAddResultInfo>(resp.Data.Span);
                }
            }
            return new SForwardAddResultInfo
            {
                BufferSize = 1,
                Message = "node not found",
                Success = false
            };
        }


        /// <summary>
        /// 获取节点列表
        /// </summary>
        /// <param name="super">是否已认证</param>
        /// <returns></returns>
        public async Task<List<SForwardServerNodeReportInfo>> GetNodes(bool super, string userid, string machineId)
        {
            List<string> sforward = (await sForwardServerWhiteListStore.GetNodes(userid, machineId)).Where(c => c.Bandwidth >= 0).SelectMany(c => c.Nodes).ToList();

            var result = reports.Values
                .Where(c => Environment.TickCount64 - c.LastTicks < 15000)
                .Where(c =>
                {
                    return super || c.Public || sforward.Contains(c.Id) || sforward.Contains("*");
                })
                .OrderByDescending(c => c.LastTicks);

            return result.ThenBy(x => x.BandwidthRatio)
                     .ThenByDescending(x => x.MaxBandwidth == 0 ? double.MaxValue : x.MaxBandwidth)
                     .ThenByDescending(x => x.MaxBandwidthTotal == 0 ? double.MaxValue : x.MaxBandwidthTotal)
                     .ThenByDescending(x => x.MaxGbTotal == 0 ? double.MaxValue : x.MaxGbTotal)
                     .ThenByDescending(x => x.MaxGbTotalLastBytes == 0 ? long.MaxValue : x.MaxGbTotalLastBytes).ToList();
        }
        public bool GetNode(string id, out SForwardServerNodeReportInfo node)
        {
            return reports.TryGetValue(id, out node) && node != null && Environment.TickCount64 - node.LastTicks < 15000;
        }

    }
}
