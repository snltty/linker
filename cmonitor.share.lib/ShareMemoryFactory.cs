using System.Runtime.InteropServices;

namespace cmonitor.share.lib
{
    public static class ShareMemoryFactory
    {
        public static IShareMemory Create(string key, int length, int itemSize)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new ShareMemoryWindows(key, length, itemSize);
            }

            return new ShareMemoryWindows(key, length, itemSize);
        }
    }
}
