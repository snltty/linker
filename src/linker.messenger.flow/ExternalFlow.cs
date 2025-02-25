using linker.libs;
using linker.messenger.tunnel;
namespace linker.messenger.flow
{
    public sealed class ExternalFlow : IFlow
    {
        public ulong ReceiveBytes { get; private set; }
        public ulong SendtBytes { get; private set; }
        public string FlowName => "External";
        public VersionManager Version { get; } = new VersionManager();

        public ExternalFlow()
        {
        }


        public string GetItems() => string.Empty;
        public void SetItems(string json) { }
        public void SetBytes(ulong receiveBytes, ulong sendtBytes) { ReceiveBytes = receiveBytes; SendtBytes = sendtBytes; }
        public void Clear() { ReceiveBytes = 0; SendtBytes = 0;}

        public void AddReceive(ulong bytes) { ReceiveBytes += bytes; Version.Add(); }
        public void AddSendt(ulong bytes) { SendtBytes += bytes; Version.Add(); }

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

        public override void AddReceive(ulong bytes) { externalFlow.AddReceive(bytes); }
        public override void AddSendt(ulong bytes) { externalFlow.AddSendt(bytes); }

    }

}