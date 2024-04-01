using cmonitor.api;
using cmonitor.client.report;
using cmonitor.server;
using common.libs;
using MemoryPack;

namespace cmonitor.plugins.report.messenger
{
    public sealed class ReportClientMessenger : IMessenger
    {
        private readonly IApiServer clientServer;
        private readonly ClientReportTransfer reportTransfer;

        public ReportClientMessenger(IApiServer clientServer, ClientReportTransfer reportTransfer)
        {
            this.clientServer = clientServer;
            this.reportTransfer = reportTransfer;
        }

        [MessengerId((ushort)ReportMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            ReportType reportType = ReportType.Trim;
            if (connection.ReceiveRequestWrap.Payload.Length > 0)
            {
                reportType = (ReportType)connection.ReceiveRequestWrap.Payload.Span[0];
            }

            reportTransfer.Update(reportType);
            connection.Write(Helper.TrueArray);
        }

        [MessengerId((ushort)ReportMessengerIds.Report)]
        public void Report(IConnection connection)
        {
            string report = MemoryPackSerializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            clientServer.Notify("/notify/report/report", new { connection.Name, Report = report });
        }


        [MessengerId((ushort)ReportMessengerIds.Ping)]
        public void Ping(IConnection connection)
        {
            connection.Write(Helper.TrueArray);
        }

    }

}
