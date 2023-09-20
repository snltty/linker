namespace cmonitor.server.client.reports.system
{
    public static class WindowsDrive
    {
        public static List<string> GetAllDrives()
        {
            List<string> drives = new List<string>();

            foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
            {
                if (driveInfo.IsReady && driveInfo.DriveType == DriveType.Fixed)
                {
                    drives.Add(driveInfo.Name);
                }
            }

            return drives;
        }
    }
}
