using linker.libs;
using linker.messenger.tunnel;
namespace linker.messenger.flow
{
    public sealed class ExternalFlow : IFlow
    {
        public long ReceiveBytes { get; private set; }
        public long SendtBytes { get; private set; }
        public string FlowName => "External";
        public VersionManager Version { get; } = new VersionManager();

        public ExternalFlow()
        {
        }


        public string GetItems() => string.Empty;
        public void SetItems(string json) { }
        public void SetBytes(long receiveBytes, long sendtBytes) { ReceiveBytes = receiveBytes; SendtBytes = sendtBytes; }
        public void Clear() { ReceiveBytes = 0; SendtBytes = 0;}

        public void AddReceive(long bytes) { ReceiveBytes += bytes; Version.Increment(); }
        public void AddSendt(long bytes) { SendtBytes += bytes; Version.Increment(); }

    }

    /// <summary>
    /// 外网端口处理器
    /// </summary>
    public sealed class ExternalResolverFlow : TunnelServerExternalResolver
    {
        private readonly ExternalFlow externalFlow;
        public ExternalResolverFlow(ExternalFlow externalFlow)
        {
            this.externalFlow = externalFlow;
        }

        public override void AddReceive(long bytes) { externalFlow.AddReceive(bytes); }
        public override void AddSendt(long bytes) { externalFlow.AddSendt(bytes); }

    }

}