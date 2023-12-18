using common.libs.winapis;
using MemoryPack;
using System.Diagnostics;

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
                registryKeys = system.OptionKeys();
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
                systemReportInfo.OptionKeys = registryKeys;
            }
            else
            {
                systemReportInfo.Drives = null;
                systemReportInfo.OptionKeys = null;
            }

            if (reportType == ReportType.Full || systemReportInfo.Cpu != lastCpu || systemReportInfo.Memory != lastMemory)
            {
                lastCpu = systemReportInfo.Cpu;
                lastMemory = systemReportInfo.Memory;
                return systemReportInfo;
            }
            return null;
        }

        public void OptionUpdate(SystemOptionUpdateInfo registryUpdateInfo)
        {
            system.OptionUpdate(registryUpdateInfo);
        }

        public bool Password(PasswordInputInfo info)
        {
            return system.Password(info);
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
                        systemReportInfo.OptionValues = system.OptionValues();
                        //systemReportInfo.Disk = WindowsDrive.GetDiskUsage();
                    }
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

        public string OptionValues { get; set; }
        public Dictionary<string, SystemOptionKeyInfo> OptionKeys { get; set; }
    }

    [MemoryPackable]
    public sealed partial class SystemOptionUpdateInfo
    {
        public string[] Keys { get; set; }
        public bool Value { get; set; }
    }

    

}
