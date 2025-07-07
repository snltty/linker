using linker.libs;
using linker.libs.extends;
using linker.messenger.pcp;
using linker.messenger.relay.client;
using linker.messenger.signin;
using linker.messenger.socks5;
using linker.snat;
using linker.tunnel;
using linker.tunnel.connection;
using System.Collections.Concurrent;
using System.Net;

namespace linker.messenger.flow
{
    public sealed class FlowSocks5Proxy : Socks5Proxy
    {
        private readonly FlowSocks5 flowSocks5;
        private readonly FlowTunnel flowTunnel;

        public FlowSocks5Proxy(FlowSocks5 flowSocks5, FlowTunnel flowTunnel, ISignInClientStore signInClientStore, TunnelTransfer tunnelTransfer, RelayClientTransfer relayTransfer, PcpTransfer pcpTransfer,
            SignInClientTransfer signInClientTransfer, IRelayClientStore relayClientStore, LinkerFirewall linkerFirewall, Socks5CidrDecenterManager socks5CidrDecenterManager) : base(signInClientStore, tunnelTransfer, relayTransfer, pcpTransfer, signInClientTransfer, relayClientStore, linkerFirewall, socks5CidrDecenterManager)
        {
            this.flowSocks5 = flowSocks5;
            this.flowTunnel = flowTunnel;
        }
        public override void Add(string machineId, IPEndPoint target, long recvBytes, long sendtBytes)
        {
            flowSocks5.Add(machineId, target, recvBytes, sendtBytes);
        }
        public override void Add(ITunnelConnection connection)
        {
            flowTunnel.Add(connection);
        }
    }

    public sealed class FlowSocks5 : IFlow
    {
        public long ReceiveBytes { get; private set; }
        public long SendtBytes { get; private set; }
        public string FlowName => "Socks5";
        public VersionManager Version { get; } = new VersionManager();

        private readonly LastTicksManager lastTicksManager = new LastTicksManager();

        private ConcurrentDictionary<(string machineId, IPAddress ip, ushort port), Socks5FlowItemInfo> flows = new ConcurrentDictionary<(string machineId, IPAddress ip, ushort port), Socks5FlowItemInfo>();

        public FlowSocks5()
        {
        }

        public string GetItems() => flows.ToJson();
        public void SetItems(string json) { flows = json.DeJson<ConcurrentDictionary<(string machineId, IPAddress ip, ushort port), Socks5FlowItemInfo>>(); }
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
            if (flows.TryGetValue(_key, out Socks5FlowItemInfo messengerFlowItemInfo) == false)
            {
                messengerFlowItemInfo = new Socks5FlowItemInfo { Key = key, Target = target };
                flows.TryAdd(_key, messengerFlowItemInfo);
            }
            ReceiveBytes += recvBytes;
            messengerFlowItemInfo.ReceiveBytes += recvBytes;
            SendtBytes += sendtBytes;
            messengerFlowItemInfo.SendtBytes += sendtBytes;
            Version.Increment();
        }
        public Socks5FlowResponseInfo GetFlows(Socks5FlowRequestInfo info)
        {
            var items = flows.Values.Where(c => 1 == 1);
            switch (info.Order)
            {
                case Socks5FlowOrder.Sendt:
                    if (info.OrderType == Socks5FlowOrderType.Desc)
                        items = items.OrderByDescending(x => x.SendtBytes);
                    else
                        items = items.OrderBy(x => x.SendtBytes);
                    break;
                case Socks5FlowOrder.Receive:
                    if (info.OrderType == Socks5FlowOrderType.Desc)
                        items = items.OrderByDescending(x => x.ReceiveBytes);
                    else
                        items = items.OrderBy(x => x.ReceiveBytes);
                    break;
                default:
                    break;
            }
            Socks5FlowResponseInfo resp = new Socks5FlowResponseInfo
            {
                Page = info.Page,
                PageSize = info.PageSize,
                Count = flows.Count,
                Data = items.Skip((info.Page - 1) * info.PageSize).Take(info.PageSize).ToList()
            };

            return resp;
        }
    }

    public sealed partial class Socks5FlowItemInfo : FlowItemInfo
    {
        public string Key { get; set; }
        public IPEndPoint Target { get; set; }
    }

    public sealed partial class Socks5FlowRequestInfo
    {
        public string MachineId { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public Socks5FlowOrder Order { get; set; }
        public Socks5FlowOrderType OrderType { get; set; }
    }

    public enum Socks5FlowOrder : byte
    {
        Sendt = 1,
        Receive = 2,
    }
    public enum Socks5FlowOrderType : byte
    {
        Desc = 0,
        Asc = 1,
    }

    public sealed partial class Socks5FlowResponseInfo
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Count { get; set; }
        public List<Socks5FlowItemInfo> Data { get; set; }
    }
}