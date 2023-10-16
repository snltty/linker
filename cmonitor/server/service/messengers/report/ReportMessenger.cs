using cmonitor.server.api;
using cmonitor.server.client.reports;
using cmonitor.server.service.messengers.sign;
using common.libs;
using MemoryPack;

namespace cmonitor.server.service.messengers.report
{
    public sealed class ReportMessenger : IMessenger
    {
        private readonly IClientServer clientServer;
        private readonly ReportTransfer  reportTransfer;

        public ReportMessenger(IClientServer clientServer , ReportTransfer reportTransfer)
        {
            this.clientServer = clientServer;
            this.reportTransfer = reportTransfer;
        }

        [MessengerId((ushort)ReportMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            ReportType reportType = ReportType.Trim;
            if(connection.ReceiveRequestWrap.Payload.Length > 0)
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

        [MessengerId((ushort)ReportMessengerIds.Pong)]
        public void Pong(IConnection connection)
        {
            connection.Write(Helper.TrueArray);
        }

    }

}
