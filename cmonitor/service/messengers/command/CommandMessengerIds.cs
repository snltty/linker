namespace cmonitor.service.messengers.command
{
    public enum CommandMessengerIds : ushort
    {
        Exec = 200,
        CommandStart = 201,
        CommandWrite = 202,
        CommandStop = 203,
        CommandAlive = 204,
        CommandData = 205,

        None = 299,
    }
}
