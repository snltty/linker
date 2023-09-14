using cmonitor.server.client.reports.volume;
using MemoryPack;

namespace cmonitor.server.service.messengers.volume
{
    public sealed class VolumeMessenger : IMessenger
    {
        private readonly VolumeReport volumeReport;
        public VolumeMessenger(VolumeReport volumeReport)
        {
            this.volumeReport = volumeReport;
        }

        [MessengerId((ushort)VolumeMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            float value = MemoryPackSerializer.Deserialize<float>(connection.ReceiveRequestWrap.Payload.Span);
            volumeReport.SetVolume(value);
        }

        [MessengerId((ushort)VolumeMessengerIds.Mute)]
        public void Mute(IConnection connection)
        {
            bool value = MemoryPackSerializer.Deserialize<bool>(connection.ReceiveRequestWrap.Payload.Span);
            volumeReport.SetVolumeMute(value);
        }
    }

}
