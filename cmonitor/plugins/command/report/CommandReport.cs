using cmonitor.client;
using cmonitor.client.report;
using cmonitor.plugins.command.messenger;
using cmonitor.server;
using MemoryPack;

namespace cmonitor.plugins.command.report
{
    public sealed class CommandReport : IClientReport
    {
        public string Name => "Command";

        private readonly ICommandLine commandLine;
        private readonly MessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;
        public CommandReport(ICommandLine commandLine, MessengerSender messengerSender, ClientSignInState clientSignInState)
        {
            this.commandLine = commandLine;
            commandLine.OnData += OnCommandData;
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;
        }

        public object GetReports(ReportType reportType)
        {
            return null;
        }

        public int CommandStart()
        {
            return commandLine.Start();
        }
        public void CommandWrite(CommandLineWriteInfo writeInfo)
        {
            commandLine.Write(writeInfo.Id, writeInfo.Command);
        }
        public void CommandStop(int id)
        {
            commandLine.Stop(id);
        }
        public void CommandAlive(int id)
        {
            commandLine.Alive(id);
        }
        private void OnCommandData(int id, string data)
        {
            messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)CommandMessengerIds.CommandData,
                Payload = MemoryPackSerializer.Serialize(new CommandLineDataInfo { Data = data, Id = id })
            }).ConfigureAwait(false);
        }
    }

    [MemoryPackable]
    public sealed partial class CommandLineWriteInfo
    {
        public int Id { get; set; }
        public string Command { get; set; }
    }

    [MemoryPackable]
    public sealed partial class CommandLineDataInfo
    {
        public int Id { get; set; }
        public string Data { get; set; }
    }
}
