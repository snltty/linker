using cmonitor.plugins.light.report;
using cmonitor.server;
using MemoryPack;

namespace cmonitor.plugins.light.messenger
{
    public sealed class LightClientMessenger : IMessenger
    {
        private readonly LightReport lightReport;
        public LightClientMessenger(LightReport lightReport)
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
