using MemoryPack;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace cmonitor.server.client.reports.share
{
    public sealed class ShareReport : IReport
    {
        public string Name => "Share";
        private readonly Config config;

        Dictionary<string, ShareItemInfo> dic = new Dictionary<string, ShareItemInfo>();
        public ShareReport(Config config)
        {
            this.config = config;
            if (config.IsCLient)
            {
                InitShare();
            }
        }
        public object GetReports()
        {
            GetShare();
            return dic;
        }
        public bool GetShare(string key, out ShareItemInfo item)
        {
            return dic.TryGetValue(key, out item);
        }


        MemoryMappedFile mmf3;
        MemoryMappedViewAccessor accessor3;
        byte[] bytes;
        private void InitShare()
        {
            if (OperatingSystem.IsWindows())
            {
                bytes = new byte[config.ShareMemoryLength];
                mmf3 = MemoryMappedFile.CreateOrOpen(config.ShareMemoryKey, bytes.Length);
                accessor3 = mmf3.CreateViewAccessor();
            }
        }
        private void GetShare()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    accessor3.ReadArray(0, bytes, 0, bytes.Length);
                    Span<byte> span = bytes.AsSpan();
                    int index = 0;
                    /*
                     * 格式  5 00000 2 00   key长度+key+val长度+val
                     */
                    while (span.Length > 0)
                    {
                        byte keyLen = span[0];
                        if (keyLen > 0 && keyLen <= Config.ShareMemoryItemLength - 2)
                        {
                            string key = Encoding.UTF8.GetString(span.Slice(1, keyLen));
                            string val = string.Empty;
                            byte valLen = span[1 + keyLen];
                            if (valLen > 0 && valLen <= Config.ShareMemoryItemLength - 2 - keyLen)
                            {
                                val = Encoding.UTF8.GetString(span.Slice(2 + keyLen, valLen));
                            }
                            dic[key] = new ShareItemInfo
                            {
                                Index = index,
                                Value = val
                            };
                        }
                        span = span.Slice(Config.ShareMemoryItemLength);
                        index++;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public void Update(ShareItemInfo item)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    int valIndex = item.Index * Config.ShareMemoryItemLength;
                    accessor3.Read(valIndex, out byte keyLen);
                    valIndex += 1 + keyLen;

                    byte[] bytes = Encoding.UTF8.GetBytes(item.Value);
                    if (bytes.Length < Config.ShareMemoryItemLength - 2 - keyLen)
                    {
                        accessor3.Write(valIndex, (byte)bytes.Length);
                        accessor3.WriteArray(valIndex + 1, bytes, 0, bytes.Length);
                    }
                }
            }
            catch (Exception)
            {
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
        /// 内存值
        /// </summary>
        public string Value { get; set; }

    }

}
