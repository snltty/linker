namespace cmonitor.server.client.reports.system
{
    public sealed class SystemReport : IReport
    {
        public string Name => "System";

        private readonly SystemReportInfo systemReportInfo = new SystemReportInfo();
        public SystemReport()
        {
            systemReportInfo.Drives = WindowsDrive.GetAllDrives();
            ReportTask();
        }

        public object GetReports()
        {
            return systemReportInfo;
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

                    Thread.Sleep(1000);
                }
            }, TaskCreationOptions.LongRunning);
        }
    }

    public sealed class SystemReportInfo
    {
        public double Cpu { get; set; }
        public double Memory { get; set; }
        public List<ReportDriveInfo> Drives { get; set; }
    }
}
