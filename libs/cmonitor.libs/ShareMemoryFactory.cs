using System;
using System.Runtime.InteropServices;

namespace cmonitor.libs
{
    internal static class ShareMemoryFactory
    {
        public static IShareMemory Create(string key, int length, int itemSize)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new ShareMemoryWindows(key, length, itemSize);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new ShareMemoryLinux(key, length, itemSize);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new ShareMemoryMacOS(key, length, itemSize);
            }

            return new ShareMemoryWindows(key, length, itemSize);
        }
    }
}
