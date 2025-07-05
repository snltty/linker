using linker.libs;
using System.Text.Json.Serialization;

namespace linker.messenger.flow
{
    public interface IFlow
    {
        public long ReceiveBytes { get; }
        public long SendtBytes { get; }
        public string FlowName { get; }

        public VersionManager Version { get; }
        public string GetItems();
        public void SetItems(string json);
        public void SetBytes(long receiveBytes,long sendtBytes);
        public void Clear();

        public (long, long) GetDiffBytes(long recv, long sent);
    }

    public partial class FlowItemInfo
    {
        public long ReceiveBytes { get; set; }
        public long SendtBytes { get; set; }

        [JsonIgnore]
        public string FlowName { get; set; }
    }

    public sealed partial class FlowInfo
    {
        public Dictionary<string, FlowItemInfo> Items { get; set; }
        public DateTime Start { get; set; }
        public DateTime Now { get; set; }
    }
    public sealed partial class FlowReportNetInfo
    {
        public string City { get; set; }

        public double Lat { get; set; }
        public double Lon { get; set; }
        public int Count { get; set; }
    }

}

