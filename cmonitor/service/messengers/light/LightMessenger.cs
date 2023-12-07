using cmonitor.client.reports.light;
using MemoryPack;

namespace cmonitor.service.messengers.light
{
    public sealed class LightMessenger : IMessenger
    {
        private readonly LightReport  lightReport;
        public LightMessenger(LightReport lightReport)
        {
            this.lightReport = lightReport;
        }

        [MessengerId((ushort)LightMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            int value = MemoryPackSerializer.Deserialize<int>(connection.ReceiveRequestWrap.Payload.Span);
            lightReport.SetLight(value);
        }

    }

}
