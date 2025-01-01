using linker.libs;
using linker.plugins.sforward.proxy;
using System.Text.Json.Serialization;

namespace linker.messenger.flow
{
    public sealed class SForwardProxyFlow: SForwardProxy
    {
        private readonly SForwardFlow sForwardFlow;
        public SForwardProxyFlow(SForwardFlow sForwardFlow)
        {
            this.sForwardFlow = sForwardFlow;
        }
        public override void AddReceive(string key,string groupid, ulong bytes)
        {
            sForwardFlow.AddReceive(key, groupid, bytes);
        }
        public override void AddSendt(string key, string groupid, ulong bytes)
        {
            sForwardFlow.AddSendt(key, groupid, bytes);
        }
    }

    public sealed class SForwardFlow : IFlow
    {
        public ulong ReceiveBytes { get; private set; }
        public ulong SendtBytes { get; private set; }
        public string FlowName => "SForward";

        private readonly LastTicksManager lastTicksManager = new LastTicksManager();

        private Dictionary<string, SForwardFlowItemInfo> flows { get; } = new Dictionary<string, SForwardFlowItemInfo>();

        public SForwardFlow()
        {
            TimerHelper.SetInterval(() =>
            {
                if (lastTicksManager.DiffLessEqual(5000))
                {
                    foreach (var item in flows.Values)
                    {
                        item.DiffReceiveBytes = item.SendtBytes - item.OldSendtBytes;
                        item.DiffSendtBytes = item.ReceiveBytes - item.OldReceiveBytes;

                        item.OldSendtBytes = item.SendtBytes;
                        item.OldReceiveBytes = item.ReceiveBytes;
                    }
                }
                return true;
            }, () => lastTicksManager.DiffLessEqual(5000) ? 1000 : 30000);
        }


        public void Update()
        {
            lastTicksManager.Update();
        }

        public void AddReceive(string key,string groupid, ulong bytes)
        {
            if (flows.TryGetValue(key, out SForwardFlowItemInfo messengerFlowItemInfo) == false)
            {
                messengerFlowItemInfo = new SForwardFlowItemInfo { Key = key, GroupId= groupid };
                flows.TryAdd(key, messengerFlowItemInfo);
            }
            ReceiveBytes += bytes;
            messengerFlowItemInfo.ReceiveBytes += bytes;
        }
        public void AddSendt(string key, string groupid, ulong bytes)
        {
            if (flows.TryGetValue(key, out SForwardFlowItemInfo messengerFlowItemInfo) == false)
            {
                messengerFlowItemInfo = new SForwardFlowItemInfo { Key = key, GroupId = groupid };
                flows.TryAdd(key, messengerFlowItemInfo);
            }
            SendtBytes += bytes;
            messengerFlowItemInfo.SendtBytes += bytes;
        }
        public SForwardFlowResponseInfo GetFlows(SForwardFlowRequestInfo info)
        {
            var items = flows.Values.Where(c => string.IsNullOrWhiteSpace(info.Key) || c.Key.Contains(info.Key));
            if(string.IsNullOrWhiteSpace(info.GroupId) == false)
            {
                items = items.Where(c => c.GroupId == info.GroupId);
            }
            switch (info.Order)
            {
                case SForwardFlowOrder.Sendt:
                    if (info.OrderType == SForwardFlowOrderType.Desc)
                        items = items.OrderByDescending(x => x.SendtBytes);
                    else
                        items = items.OrderBy(x => x.SendtBytes);
                    break;
                case SForwardFlowOrder.DiffSendt:
                    if (info.OrderType == SForwardFlowOrderType.Desc)
                        items = items.OrderByDescending(x => x.DiffSendtBytes);
                    else
                        items = items.OrderBy(x => x.DiffSendtBytes);
                    break;
                case SForwardFlowOrder.Receive:
                    if (info.OrderType == SForwardFlowOrderType.Desc)
                        items = items.OrderByDescending(x => x.ReceiveBytes);
                    else
                        items = items.OrderBy(x => x.ReceiveBytes);
                    break;
                case SForwardFlowOrder.DiffRecive:
                    if (info.OrderType == SForwardFlowOrderType.Desc)
                        items = items.OrderByDescending(x => x.DiffReceiveBytes);
                    else
                        items = items.OrderBy(x => x.DiffReceiveBytes);
                    break;
                default:
                    break;
            }
            SForwardFlowResponseInfo resp = new SForwardFlowResponseInfo
            {
                Page = info.Page,
                PageSize = info.PageSize,
                Count = flows.Count,
                Data = items.Skip((info.Page - 1) * info.PageSize).Take(info.PageSize).ToList()
            };

            return resp;
        }
    }

    public sealed partial class SForwardFlowItemInfo : FlowItemInfo
    {
        public ulong DiffReceiveBytes { get; set; }
        public ulong DiffSendtBytes { get; set; }
        public string Key { get; set; }

        public string GroupId { get; set; }

        [ JsonIgnore]
        public ulong OldReceiveBytes { get; set; }
        [ JsonIgnore]
        public ulong OldSendtBytes { get; set; }
    }

    public sealed partial class SForwardFlowRequestInfo
    {
        public string Key { get; set; } = string.Empty;

        public string GroupId { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public SForwardFlowOrder Order { get; set; }
        public SForwardFlowOrderType OrderType { get; set; }
    }

    public enum SForwardFlowOrder : byte
    {
        Sendt = 1,
        DiffSendt = 2,
        Receive = 3,
        DiffRecive = 4
    }
    public enum SForwardFlowOrderType : byte
    {
        Desc = 0,
        Asc = 1,
    }

    public sealed partial class SForwardFlowResponseInfo
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Count { get; set; }
        public List<SForwardFlowItemInfo> Data { get; set; }
    }
}