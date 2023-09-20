namespace cmonitor.server.client.reports.system
{
    public static class WindowsDrive
    {
        public static List<ReportDriveInfo> GetAllDrives()
        {
            return DriveInfo.GetDrives().Select(c => new ReportDriveInfo
            {
                Name = c.Name,
                Free = c.TotalFreeSpace,
                Total = c.TotalSize
            }).ToList();
        }
    }

    public sealed class ReportDriveInfo
    {
        public string Name { get; set; }
        public long Free { get; set; }
        public long Total { get; set; }
    }


}
