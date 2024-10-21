using linker.plugins.tunnel;
namespace linker.plugins.flow
{
    public sealed class ExternalFlow : IFlow
    {
        public ulong ReceiveBytes { get; private set; }
        public ulong SendtBytes { get; private set; }
        public string FlowName => "External";
        public ExternalFlow()
        {
        }

        public void AddReceive(ulong bytes) { ReceiveBytes += bytes; }
        public void AddSendt(ulong bytes) { SendtBytes += bytes; }

    }

    /// <summary>
    /// 外网端口处理器
    /// </summary>
    public sealed class ExternalResolverFlow : ExternalResolver
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