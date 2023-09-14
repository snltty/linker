using cmonitor.server.service.messengers.command;
using common.libs;
using MemoryPack;

namespace cmonitor.server.service.messengers.report
{
    public sealed class CommandMessenger : IMessenger
    {
        public CommandMessenger()
        {
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


    }

}
