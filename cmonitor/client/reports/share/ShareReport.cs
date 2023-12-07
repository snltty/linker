using cmonitor.libs;
using MemoryPack;

namespace cmonitor.client.reports.share
{
    public sealed class ShareReport : IReport
    {
        public string Name => "Share";

        private readonly ShareMemory shareMemory;
        private long version = 0;

        Dictionary<string, libs.ShareItemInfo> dic = new Dictionary<string, libs.ShareItemInfo>();

        public ShareReport(Config config, ShareMemory shareMemory)
        {
            this.shareMemory = shareMemory;
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
            shareMemory.AddAttribute(0, ShareMemoryAttribute.Running);
            shareMemory.RemoveAttribute(0, ShareMemoryAttribute.Closed);
            shareMemory.AddAttributeAction(0, ShareMemoryStateChanged);
        }
        private bool GetShare()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    if (shareMemory != null)
                    {
                        dic = shareMemory.ReadItems(out long _version);
                        bool res = _version > version;
                        version = _version;
                        return res;
                    }
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        private void ShareMemoryStateChanged(ShareMemoryAttribute state)
        {
            if ((state & ShareMemoryAttribute.Closed) == ShareMemoryAttribute.Closed)
            {
                shareMemory.RemoveAttribute(0, ShareMemoryAttribute.Closed);
                shareMemory.RemoveAttribute(0, ShareMemoryAttribute.Running);
                Environment.Exit(0);
            }
        }

    }


}
