using cmonitor.libs;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.server.client.reports.share
{
    public sealed class ShareReport : IReport
    {
        public string Name => "Share";
        private readonly Config config;
        private ShareMemory shareMemory;

        Dictionary<string, libs.ShareItemInfo> dic = new Dictionary<string, libs.ShareItemInfo>();

        public ShareReport(Config config)
        {
            this.config = config;
            if (config.IsCLient)
            {
                InitShare();
            }
        }
        public object GetReports(ReportType reportType)
        {
            bool updated = GetShare();
            if ((dic.Count > 0 && updated) || reportType == ReportType.Full)
            {
                return dic;
            }
            return null;
        }

        private void InitShare()
        {
            shareMemory = new ShareMemory(config.ShareMemoryKey, config.ShareMemoryLength, config.ShareMemoryItemSize);
            shareMemory.InitLocal();
            shareMemory.InitGlobal();
            shareMemory.WriteRunning(0, true);
            shareMemory.WriteClosed(0, false);
            shareMemory.StateAction(ShareMemoryStateChanged);
            shareMemory.Loop();
        }
        private bool GetShare()
        {
            bool updated = false;
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    if (shareMemory != null)
                    {
                        dic = shareMemory.GetItems(out updated);
                    }

                }
            }
            catch (Exception)
            {
            }
            return updated;
        }

        public bool GetShare(string key, out cmonitor.libs.ShareItemInfo item)
        {
            return dic.TryGetValue(key, out item);
        }
        public bool GetShareRunning(int index)
        {
            return shareMemory.ReadRunning(index);
        }
        public void UpdateShare(ShareItemInfo shareItemInfo)
        {
            if (shareMemory != null)
                shareMemory.Update(shareItemInfo.Index, shareItemInfo.Key, shareItemInfo.Value);
        }
        public void WriteClose(int index, bool closed = true)
        {
            if (shareMemory != null)
                shareMemory.WriteClosed(index, closed);
        }


        private void ShareMemoryStateChanged(int index, ShareMemoryState state)
        {
            if (index == 0 && (state & ShareMemoryState.Closed) == ShareMemoryState.Closed)
            {
                shareMemory.WriteClosed(0, false);
                shareMemory.WriteRunning(0, false);
                Environment.Exit(0);
            }
        }

    }

    [MemoryPackable]
    public sealed partial class ShareItemInfo
    {
        /// <summary>
        /// 内存下标
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// 内存key，为空则不更新key
        /// </summary>
        public string Key { get; set; } = string.Empty;
        /// <summary>
        /// 内存值
        /// </summary>
        public string Value { get; set; }
    }
}
