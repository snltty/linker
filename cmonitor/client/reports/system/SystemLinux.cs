namespace cmonitor.client.reports.system
{
    public sealed class SystemLinux : ISystem
    {
        public ReportDriveInfo[] GetAllDrives()
        {
            return Array.Empty<ReportDriveInfo>();
        }

        public double GetCpu()
        {
            return 0;
        }

        public double GetMemory()
        {
            return 0;
        }

        public Dictionary<string, SystemOptionKeyInfo> GetOptionKeys()
        {
            return new Dictionary<string, SystemOptionKeyInfo>();
        }

        public string GetOptionValues()
        {
            return string.Empty;
        }

        public void OptionRefresh()
        {
           
        }

        public bool OptionUpdate(SystemOptionUpdateInfo registryUpdateInfo)
        {
            return true;
        }

        public bool Password(PasswordInputInfo command)
        {
            return true;
        }
    }
}
