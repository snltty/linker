namespace cmonitor.server.client.reports.system
{
    public static class WindowsDrive
    {
        public static ReportDriveInfo[] GetAllDrives()
        {
            return DriveInfo.GetDrives().Select(c => new ReportDriveInfo
            {
                Name = c.Name,
                Free = c.TotalFreeSpace,
                Total = c.TotalSize
            }).ToArray();
        }
        /*
        static PerformanceCounter diskTimeCounter;
        static DateTime previousTime = DateTime.Now;
        static float previousValue = 0;
        public static float GetDiskUsage()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (diskTimeCounter == null)
                {
                    diskTimeCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
                    previousValue = diskTimeCounter.NextValue();
                    previousTime = DateTime.Now;
                    return 0;
                }

                float currentValue = diskTimeCounter.NextValue();
                DateTime currentTime = DateTime.Now;
                float deltaTimeSeconds = (float)(currentTime - previousTime).TotalSeconds;

                float diskUsage = Math.Min(100f, ((currentValue + previousValue) / 2f) * deltaTimeSeconds);

                previousValue = currentValue;
                previousTime = currentTime;

                return diskUsage;

            }
            return 0;
        }
        */
    }

    public sealed class ReportDriveInfo
    {
        public string Name { get; set; }
        public long Free { get; set; }
        public long Total { get; set; }
    }


}
