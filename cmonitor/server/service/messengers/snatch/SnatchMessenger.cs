using cmonitor.server.client.reports.snatch;
using MemoryPack;

namespace cmonitor.server.service.messengers.snatch
{
    public sealed class SnatchMessenger : IMessenger
    {
        private readonly SnatchReport snatchReport;

        public SnatchMessenger(SnatchReport snatchReport)
        {
            this.snatchReport = snatchReport;
        }

        [MessengerId((ushort)SnatchMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            SnatchQuestionInfo question = MemoryPackSerializer.Deserialize<SnatchQuestionInfo>(connection.ReceiveRequestWrap.Payload.Span);
            snatchReport.Update(question);
        }
    }

}
