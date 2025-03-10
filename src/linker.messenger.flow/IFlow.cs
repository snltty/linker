﻿using linker.libs;
using System.Text.Json.Serialization;

namespace linker.messenger.flow
{
    public interface IFlow
    {
        public ulong ReceiveBytes { get; }
        public ulong SendtBytes { get; }
        public string FlowName { get; }

        public VersionManager Version { get; }
        public string GetItems();
        public void SetItems(string json);
        public void SetBytes(ulong receiveBytes,ulong sendtBytes);
        public void Clear();
    }

    public partial class FlowItemInfo
    {
        public ulong ReceiveBytes { get; set; }
        public ulong SendtBytes { get; set; }

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

