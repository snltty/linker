using System;

namespace cmonitor.libs
{
    internal static class ShareMemoryFactory
    {
        public static IShareMemory Create(string key, int length, int itemSize)
        {
            if (OperatingSystem.IsWindows())
            {
                return new ShareMemoryWindows(key, length, itemSize);
            }
            else if (OperatingSystem.IsLinux())
            {
                return new ShareMemoryLinux(key, length, itemSize);
            }
            else if (OperatingSystem.IsMacOS())
            {
                return new ShareMemoryMacOS(key, length, itemSize);
            }

            return new ShareMemoryWindows(key, length, itemSize);
        }
    }
}
