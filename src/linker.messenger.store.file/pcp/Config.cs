using linker.messenger.pcp;
using LiteDB;
using System.Text.Json.Serialization;

namespace linker.messenger.store.file
{
    public sealed partial class RunningConfigInfo
    {
        /// <summary>
        /// 打洞历史记录
        /// </summary>
        public PcpHistoryInfo PcpHistory { get; set; } = new PcpHistoryInfo();
    }
}
