using cmonitor.client.report;

namespace cmonitor.plugins.display.report
{
    public sealed class DisplayReport : IClientReport
    {

        public string Name => "Display";
        private readonly IDisplay display;

        public DisplayReport(IDisplay display)
        {
            this.display = display;
        }

        public void SetDisplayState(bool onState)
        {
            display.SetDisplayState(onState);
        }

        public object GetReports(ReportType reportType)
        {
            return null;
        }
    }

}
