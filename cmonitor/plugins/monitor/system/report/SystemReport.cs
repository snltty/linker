using cmonitor.client.report;
using cmonitor.config;
using MemoryPack;

namespace cmonitor.plugins.system.report
{
    public sealed class SystemReport : IClientReport
    {
        public string Name => "System";

        private readonly SystemReportInfo systemReportInfo = new SystemReportInfo();

        private readonly ISystem system;

        private ReportDriveInfo[] drives;
        private Dictionary<string, SystemOptionKeyInfo> registryKeys;

        public SystemReport(ISystem system, Config config)
        {
            this.system = system;
            drives = system.GetAllDrives();
            registryKeys = system.OptionKeys();
            ReportTask();
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
            if (reportType == ReportType.Full || systemReportInfo.Updated())
            {
                return systemReportInfo;
            }
            return null;
        }

        public void OptionUpdate(SystemOptionUpdateInfo[] registryUpdateInfo)
        {
            system.OptionUpdate(registryUpdateInfo);
        }

        public bool Password(PasswordInputInfo info)
        {
            return system.Password(info);
        }


        private void ReportTask()
        {
            Task.Run(async () =>
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
                    await Task.Delay(1000);
                }
            });

        }
    }

    public sealed class SystemReportInfo:ReportInfo
    {
        public double Cpu { get; set; }
        public double Memory { get; set; }
        public float Disk { get; set; }
        public ReportDriveInfo[] Drives { get; set; }

        public string OptionValues { get; set; } = string.Empty;
        public Dictionary<string, SystemOptionKeyInfo> OptionKeys { get; set; }

        public override int HashCode()
        {
            return Cpu.GetHashCode() ^ Memory.GetHashCode() ^ OptionValues.GetHashCode();
        }
    }

    [MemoryPackable]
    public sealed partial class SystemOptionUpdateInfo
    {
        public string Key { get; set; }
        public bool Value { get; set; }
    }



}
