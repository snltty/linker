using cmonitor.plugins.keyboard.report;
using cmonitor.server;
using MemoryPack;

namespace cmonitor.plugins.keyboard.messenger
{
    public sealed class KeyboardClientMessenger : IMessenger
    {
        private readonly KeyboardReport report;
        public KeyboardClientMessenger(KeyboardReport report)
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
            report.MouseSetInfo mouseSetInfo = MemoryPackSerializer.Deserialize<report.MouseSetInfo>(connection.ReceiveRequestWrap.Payload.Span);
            report.MouseSet(mouseSetInfo);
        }

        [MessengerId((ushort)KeyboardMessengerIds.MouseClick)]
        public void MouseClick(IConnection connection)
        {
            report.MouseClickInfo mouseClickInfo = MemoryPackSerializer.Deserialize<report.MouseClickInfo>(connection.ReceiveRequestWrap.Payload.Span);
            report.MouseClick(mouseClickInfo);
        }
    }
}
