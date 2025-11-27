using linker.libs;
using linker.messenger.relay.server;

namespace linker.messenger.flow
{
    public sealed class RelayReportFlow : IFlow
    {
        public long ReceiveBytes { get; private set; }
        public long SendtBytes { get; private set; }
        public string FlowName => "RelayReport";
        public VersionManager Version { get; } = new VersionManager();
        public RelayReportFlow()
        {
        }
        public string GetItems() => string.Empty;
        public void SetItems(string json) { }
        public void SetBytes(long receiveBytes, long sendtBytes) { ReceiveBytes = receiveBytes; SendtBytes = sendtBytes; }
        public void Clear() { ReceiveBytes = 0; SendtBytes = 0; }

        public void Add(long receiveBytes, long sendtBytes) { ReceiveBytes += receiveBytes; SendtBytes += sendtBytes; Version.Increment(); }


        public (long, long) GetDiffBytes(long recv, long sent)
        {

            long diffRecv = ReceiveBytes - recv;
            long diffSendt = SendtBytes - sent;
            return (diffRecv, diffSendt);
        }
    }

    public sealed class RelayReportResolverFlow : RelayServerReportResolver
    {
        private readonly RelayReportFlow relayReportFlow;
        public RelayReportResolverFlow(RelayReportFlow relayReportFlow, IMessengerResolver messengerResolver) 
            : base( messengerResolver)
        {
            this.relayReportFlow = relayReportFlow;
        }

        public override void Add(long receiveBytes, long sendtBytes) { relayReportFlow.Add(receiveBytes, sendtBytes); }

    }

}