using linker.libs;
using linker.libs.extends;
using linker.messenger.forward.proxy;
using linker.messenger.pcp;
using linker.messenger.relay.client;
using linker.messenger.signin;
using linker.snat;
using linker.tunnel;
using linker.tunnel.connection;
using System.Collections.Concurrent;
using System.Net;

namespace linker.messenger.flow
{
    public sealed class FlowForwardProxy : ForwardProxy
    {
        private readonly FlowForward forwardFlow;
        private readonly FlowTunnel flowTunnel;
        public FlowForwardProxy(FlowForward forwardFlow, FlowTunnel flowTunnel, ISignInClientStore signInClientStore, TunnelTransfer tunnelTransfer, RelayClientTransfer relayTransfer, PcpTransfer pcpTransfer,
            SignInClientTransfer signInClientTransfer, IRelayClientStore relayClientStore, LinkerFirewall linkerFirewall) : base(signInClientStore, tunnelTransfer, relayTransfer, pcpTransfer, signInClientTransfer, relayClientStore, linkerFirewall)
        {
            this.forwardFlow = forwardFlow;
            this.flowTunnel = flowTunnel;
        }
        public override void Add(string machineId, IPEndPoint target, long recvBytes, long sendtBytes)
        {
            forwardFlow.Add(machineId, target, recvBytes, sendtBytes);
        }
        public override void Add(ITunnelConnection connection)
        {
            flowTunnel.Add(connection);
        }
    }

    public sealed class FlowForward : IFlow
    {
        public long ReceiveBytes { get; private set; }
        public long SendtBytes { get; private set; }
        public string FlowName => "Forward";
        public VersionManager Version { get; } = new VersionManager();

        private readonly LastTicksManager lastTicksManager = new LastTicksManager();

        private ConcurrentDictionary<(string machineId, IPAddress ip, ushort port), ForwardFlowItemInfo> flows = new ConcurrentDictionary<(string machineId, IPAddress ip, ushort port), ForwardFlowItemInfo>();

        public FlowForward()
        {
        }

        public string GetItems() => flows.ToJson();
        public void SetItems(string json) { flows = json.DeJson<ConcurrentDictionary<(string machineId, IPAddress ip, ushort port), ForwardFlowItemInfo>>(); }
        public void SetBytes(long receiveBytes, long sendtBytes) { ReceiveBytes = receiveBytes; SendtBytes = sendtBytes; }
        public void Clear() { ReceiveBytes = 0; SendtBytes = 0; flows.Clear(); }

        public void Update()
        {
            lastTicksManager.Update();
        }
        public (long, long) GetDiffBytes(long recv, long sent)
        {
            long diffRecv = ReceiveBytes - recv;
            long diffSendt = SendtBytes - sent;
            return (diffRecv, diffSendt);
        }

        public void Add(string key, IPEndPoint target, long recvBytes, long sendtBytes)
        {
            (string machineId, IPAddress ip, ushort port) _key = (key, target.Address, (ushort)target.Port);
            if (flows.TryGetValue(_key, out ForwardFlowItemInfo messengerFlowItemInfo) == false)
            {
                messengerFlowItemInfo = new ForwardFlowItemInfo { Key = key, Target = target };
                flows.TryAdd(_key, messengerFlowItemInfo);
            }
            ReceiveBytes += recvBytes;
            messengerFlowItemInfo.ReceiveBytes += recvBytes;
            SendtBytes += sendtBytes;
            messengerFlowItemInfo.SendtBytes += sendtBytes;
            Version.Increment();
        }
        public ForwardFlowResponseInfo GetFlows(ForwardFlowRequestInfo info)
        {
            var items = flows.Values.Where(c => 1 == 1);
            switch (info.Order)
            {
                case ForwardFlowOrder.Sendt:
                    if (info.OrderType == ForwardFlowOrderType.Desc)
                        items = items.OrderByDescending(x => x.SendtBytes);
                    else
                        items = items.OrderBy(x => x.SendtBytes);
                    break;
                case ForwardFlowOrder.Receive:
                    if (info.OrderType == ForwardFlowOrderType.Desc)
                        items = items.OrderByDescending(x => x.ReceiveBytes);
                    else
                        items = items.OrderBy(x => x.ReceiveBytes);
                    break;
                default:
                    break;
            }
            ForwardFlowResponseInfo resp = new ForwardFlowResponseInfo
            {
                Page = info.Page,
                PageSize = info.PageSize,
                Count = flows.Count,
                Data = items.Skip((info.Page - 1) * info.PageSize).Take(info.PageSize).ToList()
            };

            return resp;
        }
    }

    public sealed partial class ForwardFlowItemInfo : FlowItemInfo
    {
        public string Key { get; set; }
        public IPEndPoint Target { get; set; }
    }

    public sealed partial class ForwardFlowRequestInfo
    {
        public string MachineId { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public ForwardFlowOrder Order { get; set; }
        public ForwardFlowOrderType OrderType { get; set; }
    }

    public enum ForwardFlowOrder : byte
    {
        Sendt = 1,
        Receive = 2,
    }
    public enum ForwardFlowOrderType : byte
    {
        Desc = 0,
        Asc = 1,
    }

    public sealed partial class ForwardFlowResponseInfo
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Count { get; set; }
        public List<ForwardFlowItemInfo> Data { get; set; }
    }
}