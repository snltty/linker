using System.IO.MemoryMappedFiles;
using System.Text;

namespace cmonitor.server.client.reports.screen
{
    public sealed class ShareReport : IReport
    {
        public string Name => "Share";
        private readonly Config config;

        Dictionary<string, object> dic = new Dictionary<string, object> { { "Value", string.Empty}};

        public ShareReport(Config config)
        {
            this.config = config;

            InitShare();

        }
        public Dictionary<string, object> GetReports()
        {
            dic["Value"] = GetShare();
            return dic;
        }


        MemoryMappedFile mmf3;
        MemoryMappedViewAccessor accessor3;
        byte[] bytes;
        private void InitShare()
        {
            bytes = new byte[config.ShareMemoryLength];
            if (OperatingSystem.IsWindows())
            {
                mmf3 = MemoryMappedFile.CreateOrOpen(config.ShareMemoryKey, bytes.Length);
                accessor3 = mmf3.CreateViewAccessor();
            }
        }
        private string GetShare()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    if (accessor3 != null)
                    {
                        int length = accessor3.ReadInt32(0);
                        if (length > 0)
                        {
                            accessor3.ReadArray(4, bytes, 0, length);
                            return Encoding.UTF8.GetString(bytes.AsSpan(0, length));
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return string.Empty;
        }
    }

}
