using cmonitor.server.client.reports.command;
using cmonitor.server.service.messengers.command;
using common.libs;
using MemoryPack;

namespace cmonitor.server.service.messengers.report
{
    public sealed class CommandMessenger : IMessenger
    {

        private readonly CommandReport commandReport;
        public CommandMessenger(CommandReport commandReport)
        {
            this.commandReport = commandReport;
        }


        [MessengerId((ushort)CommandMessengerIds.Exec)]
        public void Exec(IConnection connection)
        {
            string[] commands = MemoryPackSerializer.Deserialize<string[]>(connection.ReceiveRequestWrap.Payload.Span);
            Task.Run(() =>
            {
                CommandHelper.Windows(string.Empty, commands);
            });

            connection.Write(Helper.TrueArray);
        }

        [MessengerId((ushort)CommandMessengerIds.KeyBoard)]
        public void KeyBoard(IConnection connection)
        {
            KeyBoardInputInfo command = MemoryPackSerializer.Deserialize<KeyBoardInputInfo>(connection.ReceiveRequestWrap.Payload.Span);
            commandReport.KeyBoard(command);
        }
    }

}
