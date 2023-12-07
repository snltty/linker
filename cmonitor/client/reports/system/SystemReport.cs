using MemoryPack;

namespace cmonitor.client.reports.system
{
    public sealed class SystemReport : IReport
    {
        public string Name => "System";

        private readonly SystemReportInfo systemReportInfo = new SystemReportInfo();
       
        private readonly ISystem system;

        private double lastCpu;
        private double lastMemory;
        private ReportDriveInfo[] drives;
        private Dictionary<string, SystemOptionKeyInfo> registryKeys;

        public SystemReport(ISystem system,Config config)
        {
            this.system = system;
            if (config.IsCLient)
            {
                drives = system.GetAllDrives();
                registryKeys = system.GetOptionKeys();
                ReportTask();
            }
        }


        long ticks = DateTime.UtcNow.Ticks;
        public object GetReports(ReportType reportType)
        {
            ticks = DateTime.UtcNow.Ticks;
            if (reportType == ReportType.Full)
            {
                systemReportInfo.Drives = drives;
                systemReportInfo.RegKeys = registryKeys;
            }
            else
            {
                systemReportInfo.Drives = null;
                systemReportInfo.RegKeys = null;
            }

            if (reportType == ReportType.Full || systemReportInfo.Cpu != lastCpu || systemReportInfo.Memory != lastMemory)
            {
                lastCpu = systemReportInfo.Cpu;
                lastMemory = systemReportInfo.Memory;
                return systemReportInfo;
            }
            return null;
        }

        public bool Password(PasswordInputInfo info)
        {
            return system.Password(info);
        }

        public bool OptionUpdate(SystemOptionUpdateInfo registryUpdateInfo)
        {
            return system.OptionUpdate(registryUpdateInfo);
        }

        private void ReportTask()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if ((DateTime.UtcNow.Ticks - ticks) / TimeSpan.TicksPerMillisecond < 1000)
                    {
                        systemReportInfo.Cpu = system.GetCpu();
                        systemReportInfo.Memory = system.GetMemory();
                        systemReportInfo.RegValues = system.GetOptionValues();
                        //systemReportInfo.Disk = WindowsDrive.GetDiskUsage();
                    }
                    system.OptionRefresh();


                    Thread.Sleep(1000);
                }
            }, TaskCreationOptions.LongRunning);

        }
    }

    public sealed class SystemReportInfo
    {
        public double Cpu { get; set; }
        public double Memory { get; set; }
        public float Disk { get; set; }
        public ReportDriveInfo[] Drives { get; set; }

        public string RegValues { get; set; }
        public Dictionary<string, SystemOptionKeyInfo> RegKeys { get; set; }
    }



    [MemoryPackable]
    public sealed partial class SystemOptionUpdateInfo
    {
        public string Name { get; set; }
        public bool Value { get; set; }
    }

    

}
