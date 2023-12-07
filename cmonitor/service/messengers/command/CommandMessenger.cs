using cmonitor.client.reports.command;
using cmonitor.service.messengers.command;
using common.libs;
using MemoryPack;

namespace cmonitor.service.messengers.report
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
                string result = CommandHelper.Windows(string.Empty, commands);
            });

            connection.Write(Helper.TrueArray);
        }
    }

}
