namespace cmonitor.server.client.reports.system
{
    public sealed class SystemReport : IReport
    {
        public string Name => "System";

        private readonly SystemReportInfo systemReportInfo = new SystemReportInfo();
        private double lastCpu;
        private double lastMemory;

        public SystemReport()
        {
            systemReportInfo.Drives = WindowsDrive.GetAllDrives();
            ReportTask();
        }

        public object GetReports(ReportType reportType)
        {
            if(reportType == ReportType.Full || systemReportInfo.Cpu != lastCpu || systemReportInfo.Memory != lastMemory)
            {
                lastCpu = systemReportInfo.Cpu;
                lastMemory = systemReportInfo.Memory;
                return systemReportInfo;
            }
            return null;
        }

        private void ReportTask()
        {
            CPUTime oldTime = WindowsCPU.GetCPUTime();
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    CPUTime newTime = WindowsCPU.GetCPUTime();
                    systemReportInfo.Cpu = CPUHelper.CalculateCPULoad(oldTime, newTime);
                    oldTime = newTime;

                    systemReportInfo.Memory = WindowsMemory.GetMemoryUsage();
                    //systemReportInfo.Disk = WindowsDrive.GetDiskUsage();

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
        public List<ReportDriveInfo> Drives { get; set; }
    }
}
