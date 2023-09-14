using cmonitor.server.client.reports.llock;
using MemoryPack;

namespace cmonitor.server.service.messengers.usb
{
    public sealed class UsbMessenger : IMessenger
    {
        private readonly UsbReport usbReport;

        public UsbMessenger(UsbReport usbReport)
        {
            this.usbReport = usbReport;
        }

        [MessengerId((ushort)UsbMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            usbReport.Update(MemoryPackSerializer.Deserialize<bool>(connection.ReceiveRequestWrap.Payload.Span));
        }
    }

}
