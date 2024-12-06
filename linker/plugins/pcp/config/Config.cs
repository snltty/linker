
namespace linker.client.config
{
    public sealed partial class RunningConfigInfo
    {
        /// <summary>
        /// 打洞历史记录
        /// </summary>
        public TunnelHistoryInfo TunnelHistory { get; set; } = new TunnelHistoryInfo();
    }

    public sealed class TunnelHistoryInfo
    {
        public TunnelHistoryInfo() { }

        public List<string> History { get; set; } = new List<string>();
    }
}
