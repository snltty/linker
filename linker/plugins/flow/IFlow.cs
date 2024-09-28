using MemoryPack;
using System.Text.Json.Serialization;

namespace linker.plugins.flow
{
    public interface IFlow
    {
        public ulong ReceiveBytes { get; }
        public ulong SendtBytes { get; }
        public string FlowName { get; }
    }

    [MemoryPackable]
    public partial class FlowItemInfo
    {
        public ulong ReceiveBytes { get; set; }
        public ulong SendtBytes { get; set; }

        [MemoryPackIgnore, JsonIgnore]
        public string FlowName { get; set; }
    }

    [MemoryPackable]
    public sealed partial class FlowInfo
    {
        public Dictionary<string, FlowItemInfo> Items { get; set; }
        public DateTime Start { get; set; }
        public DateTime Now { get; set; }
    }

}

