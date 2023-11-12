using MemoryPack;
using System.Buffers;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace cmonitor.server.client.reports.share
{
    public sealed class ShareReport : IReport
    {
        public string Name => "Share";
        private readonly Config config;

        Dictionary<string, ShareItemInfo> dic = new Dictionary<string, ShareItemInfo>();
        private object lockObj = new object();

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
        public bool GetShare(string key, out ShareItemInfo item)
        {
            return dic.TryGetValue(key, out item);
        }


        MemoryMappedFile mmf3;
        MemoryMappedViewAccessor accessor3;
        MemoryMappedFile mmf33;
        MemoryMappedViewAccessor accessor33;
        byte[] bytes;
        private void InitShare()
        {
            if (OperatingSystem.IsWindows())
            {
                bytes = new byte[config.ShareMemoryLength * Config.ShareMemoryItemLength];
                mmf3 = MemoryMappedFile.CreateOrOpen($"{config.ShareMemoryKey}", bytes.Length);
                accessor3 = mmf3.CreateViewAccessor();

                try
                {
                    mmf33 = MemoryMappedFile.CreateOrOpen($"Global\\{config.ShareMemoryKey}", bytes.Length);
                    accessor33 = mmf33.CreateViewAccessor();
                    ShareTask();
                }
                catch (Exception)
                {
                }
            }
        }
        private bool GetShare()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    
                    accessor3.ReadArray(0, bytes, 0, bytes.Length);
                    //改为未更新
                    UpdatedState3(0);
                    Span<byte> span = bytes.AsSpan();
                    int index = 0;
                    /*
                     * 格式  key长度+key+val长度+val
                     * 
                     * 比如 10项  0-8可用，9保留
                     * 
                     * 保留项 
                     *      0 是否已更新
                     *      其它暂未使用
                     */
                    while (span.Length > Config.ShareMemoryItemLength)
                    {
                        byte keyLen = span[0];
                        if (keyLen > 0)
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
                    return span[0] == 1;
                }
            }
            catch (Exception)
            {
            }
            return true;
        }

        private void ShareTask()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    SyncMemory();
                    Thread.Sleep(10);
                }

            }, TaskCreationOptions.LongRunning);
        }
        private void SyncMemory()
        {

            if (accessor33 != null)
            {
                //检查更新状态
                int updateStatePosition = (config.ShareMemoryLength - 1) * Config.ShareMemoryItemLength;
                if (accessor33.ReadByte(updateStatePosition) == 0)
                {
                    return;
                }
                lock (lockObj)
                {
                    byte[] bytes33 = ArrayPool<byte>.Shared.Rent(bytes.Length);
                    accessor33.ReadArray(0, bytes33, 0, bytes33.Length);

                    Span<byte> span = bytes33.AsSpan();
                    int index = 0;
                    while (span.Length > Config.ShareMemoryItemLength)
                    {
                        byte keyLen = span[0];
                        if (keyLen > 0)
                        {
                            accessor3.WriteArray(index, bytes33, index, Config.ShareMemoryItemLength);
                        }
                        span = span.Slice(Config.ShareMemoryItemLength);
                        index += Config.ShareMemoryItemLength;
                    }

                    ArrayPool<byte>.Shared.Return(bytes33);

                    UpdatedState33(0);
                    UpdatedState3(1);
                }
            }

        }

        public void Update(ShareItemInfo item)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    //最后一项不能写
                    if (item.Index > config.ShareMemoryLength - 2)
                    {
                        return;
                    }

                    lock (lockObj)
                    {
                        int valIndex = item.Index * Config.ShareMemoryItemLength;
                        int startIndex = valIndex;

                        if (!string.IsNullOrWhiteSpace(item.Key))
                        {
                            byte[] keyBytes = Encoding.UTF8.GetBytes(item.Key);
                            accessor3.Write(valIndex, (byte)keyBytes.Length);
                            accessor3.WriteArray(valIndex + 1, keyBytes, 0, keyBytes.Length);
                            if (accessor33 != null)
                            {
                                accessor33.Write(valIndex, (byte)keyBytes.Length);
                                accessor33.WriteArray(valIndex + 1, keyBytes, 0, keyBytes.Length);
                            }
                            valIndex += 1 + keyBytes.Length;
                        }
                        else
                        {
                            accessor3.Read(valIndex, out byte keyLen);
                            if (keyLen == 0 && accessor33 != null)
                            {
                                accessor33.Read(valIndex, out keyLen);
                            }
                            valIndex += 1 + keyLen;
                        }
                        byte[] bytes = Encoding.UTF8.GetBytes(item.Value);
                        if (bytes.Length + valIndex - startIndex < Config.ShareMemoryItemLength)
                        {
                            accessor3.Write(valIndex, (byte)bytes.Length);
                            accessor3.WriteArray(valIndex + 1, bytes, 0, bytes.Length);
                            if (accessor33 != null)
                            {
                                accessor33.Write(valIndex, (byte)bytes.Length);
                                accessor33.WriteArray(valIndex + 1, bytes, 0, bytes.Length);
                            }
                        }
                    }
                    //改为已更新
                    UpdatedState3(1);
                    UpdatedState33(1);
                }
            }
            catch (Exception)
            {
            }
        }

        private void UpdatedState3(byte state)
        {
            accessor3.Write((config.ShareMemoryLength - 1) * Config.ShareMemoryItemLength, state);
        }
        private void UpdatedState33(byte state)
        {
            accessor33.Write((config.ShareMemoryLength - 1) * Config.ShareMemoryItemLength, state);
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
        public string Key { get; set; }
        /// <summary>
        /// 内存值
        /// </summary>
        public string Value { get; set; }

    }

}
