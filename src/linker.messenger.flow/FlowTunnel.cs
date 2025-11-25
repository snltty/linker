using linker.libs;
using linker.libs.extends;
using linker.messenger.channel;
using linker.messenger.pcp;
using linker.messenger.signin;
using linker.messenger.tuntap;
using linker.messenger.tuntap.cidr;
using linker.tunnel;
using linker.tunnel.connection;
using System.Collections.Concurrent;

namespace linker.messenger.flow
{

    public sealed class FlowTuntapProxy : TuntapProxy
    {
        private readonly FlowTunnel flowTunnel;

        public FlowTuntapProxy(FlowTunnel flowTunnel, ISignInClientStore signInClientStore, TunnelTransfer tunnelTransfer, PcpTransfer pcpTransfer,
            SignInClientTransfer signInClientTransfer, TuntapConfigTransfer tuntapConfigTransfer,
            TuntapCidrConnectionManager tuntapCidrConnectionManager, TuntapCidrDecenterManager tuntapCidrDecenterManager,
            TuntapCidrMapfileManager tuntapCidrMapfileManager,TuntapDecenter tuntapDecenter, ChannelConnectionCaching channelConnectionCaching) 
            : base(signInClientStore, tunnelTransfer, pcpTransfer, signInClientTransfer,
                  tuntapConfigTransfer, tuntapCidrConnectionManager, tuntapCidrDecenterManager, tuntapCidrMapfileManager, tuntapDecenter, channelConnectionCaching)
        {
            this.flowTunnel = flowTunnel;
        }
        public override void Add(ITunnelConnection connection)
        {
            flowTunnel.Add(connection);
        }
    }
    public sealed class FlowTunnel : IFlow
    {
        public long ReceiveBytes { get; private set; }
        public long SendtBytes { get; private set; }
        public string FlowName => "Tunnel";
        public VersionManager Version { get; } = new VersionManager();

        private readonly LastTicksManager lastTicksManager = new LastTicksManager();

        private ConcurrentDictionary<(string machineId, string transitionId, TunnelDirection dir, TunnelType type, TunnelMode mode), TunnelFlowItemInfo> flows = new();

        public FlowTunnel()
        {
        }

        public string GetItems() => flows.ToJson();
        public void SetItems(string json) { flows = json.DeJson<ConcurrentDictionary<(string machineId, string transitionId, TunnelDirection dir, TunnelType type, TunnelMode mode), TunnelFlowItemInfo>>(); }
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

        public void Add(ITunnelConnection connection)
        {
            (string machineId, string transitionId, TunnelDirection dir, TunnelType type, TunnelMode mode) _key = (connection.RemoteMachineId, connection.TransactionId, connection.Direction, connection.Type, connection.Mode);
            if (flows.TryGetValue(_key, out TunnelFlowItemInfo messengerFlowItemInfo) == false)
            {
                messengerFlowItemInfo = new TunnelFlowItemInfo
                {
                    Key = connection.RemoteMachineId,
                    TransitionId = connection.TransactionId,
                    Direction = connection.Direction,
                    Type = connection.Type,
                    Mode = connection.Mode,
                };
                flows.TryAdd(_key, messengerFlowItemInfo);
            }
            ReceiveBytes += 1;
            messengerFlowItemInfo.ReceiveBytes += 1;
            Version.Increment();
        }
        public TunnelFlowResponseInfo GetFlows(TunnelFlowRequestInfo info)
        {
            var items = flows.Values.Where(c => 1 == 1);
            switch (info.Order)
            {
                case TunnelFlowOrder.Sendt:
                    if (info.OrderType == TunnelFlowOrderType.Desc)
                        items = items.OrderByDescending(x => x.SendtBytes);
                    else
                        items = items.OrderBy(x => x.SendtBytes);
                    break;
                case TunnelFlowOrder.Receive:
                    if (info.OrderType == TunnelFlowOrderType.Desc)
                        items = items.OrderByDescending(x => x.ReceiveBytes);
                    else
                        items = items.OrderBy(x => x.ReceiveBytes);
                    break;
                default:
                    break;
            }
            TunnelFlowResponseInfo resp = new TunnelFlowResponseInfo
            {
                Page = info.Page,
                PageSize = info.PageSize,
                Count = flows.Count,
                Data = items.Skip((info.Page - 1) * info.PageSize).Take(info.PageSize).ToList()
            };

            return resp;
        }
    }

    public sealed partial class TunnelFlowItemInfo : FlowItemInfo
    {
        public string Key { get; set; }
        public string TransitionId { get; set; }
        public TunnelDirection Direction { get; set; }
        public TunnelType Type { get; set; }
        public TunnelMode Mode { get; set; }
    }

    public sealed partial class TunnelFlowRequestInfo
    {
        public string MachineId { get; set; } = string.Empty;

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public TunnelFlowOrder Order { get; set; }
        public TunnelFlowOrderType OrderType { get; set; }
    }

    public enum TunnelFlowOrder : byte
    {
        Sendt = 1,
        Receive = 2,
    }
    public enum TunnelFlowOrderType : byte
    {
        Desc = 0,
        Asc = 1,
    }

    public sealed partial class TunnelFlowResponseInfo
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Count { get; set; }
        public List<TunnelFlowItemInfo> Data { get; set; }
    }
}