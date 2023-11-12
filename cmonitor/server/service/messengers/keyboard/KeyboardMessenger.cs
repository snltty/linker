using cmonitor.server.client.reports.command;
using MemoryPack;

namespace cmonitor.server.service.messengers.keyboard
{
    public sealed class KeyboardMessenger : IMessenger
    {
        private Config config;
        private readonly KeyboardReport report;
        public KeyboardMessenger(Config config, KeyboardReport report)
        {
            this.config = config;
            this.report = report;
        }

        [MessengerId((ushort)KeyboardMessengerIds.Keyboard)]
        public void Password(IConnection connection)
        {
            KeyBoardInputInfo keyBoardInputInfo = MemoryPackSerializer.Deserialize<KeyBoardInputInfo>(connection.ReceiveRequestWrap.Payload.Span);
            report.KeyBoard(keyBoardInputInfo);
        }

        [MessengerId((ushort)KeyboardMessengerIds.CtrlAltDelete)]
        public void CtrlAltDelete(IConnection connection)
        {
            report.CtrlAltDelete();
        }

    }
}
