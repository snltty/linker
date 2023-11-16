using cmonitor.server.client.reports.command;
using MemoryPack;

namespace cmonitor.server.service.messengers.keyboard
{
    public sealed class KeyboardMessenger : IMessenger
    {
        private readonly KeyboardReport report;
        public KeyboardMessenger(KeyboardReport report)
        {
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


        [MessengerId((ushort)KeyboardMessengerIds.MouseSet)]
        public void MouseSet(IConnection connection)
        {
            MouseSetInfo mouseSetInfo = MemoryPackSerializer.Deserialize<MouseSetInfo>(connection.ReceiveRequestWrap.Payload.Span);
            report.MouseSet(mouseSetInfo);
        }

        [MessengerId((ushort)KeyboardMessengerIds.MouseClick)]
        public void MouseClick(IConnection connection)
        {
            MouseClickInfo mouseClickInfo = MemoryPackSerializer.Deserialize<MouseClickInfo>(connection.ReceiveRequestWrap.Payload.Span);
            report.MouseClick(mouseClickInfo);
        }
    }
}
