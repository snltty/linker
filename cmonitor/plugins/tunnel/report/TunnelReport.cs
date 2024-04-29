using cmonitor.client.report;
using static cmonitor.plugins.tunnel.TunnelTransfer;

namespace cmonitor.plugins.tunnel.report
{
    public sealed class TunnelReport : IClientReport
    {
        public string Name => "Tunnel";

        private readonly TunnelTransfer tunnelTransfer;
        private readonly TunnelReportInfo tunnelReportInfo = new TunnelReportInfo();
        public TunnelReport(TunnelTransfer tunnelTransfer)
        {
            this.tunnelTransfer = tunnelTransfer;
        }
        public object GetReports(ReportType reportType)
        {
            if (tunnelTransfer.ConnectionChanged)
            {
                tunnelReportInfo.Connections = tunnelTransfer.Connections;
                return tunnelReportInfo;
            }
            return null;
        }

        public sealed class TunnelReportInfo
        {
            public Dictionary<string, TunnelConnectInfo> Connections { get; set; } = new Dictionary<string, TunnelConnectInfo>();
        }
    }
}
