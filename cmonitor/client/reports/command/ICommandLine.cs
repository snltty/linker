namespace cmonitor.client.reports.command
{
    public interface ICommandLine
    {
        Action<int, string> OnData { get; set; }

        int Start();

        void Write(int id, string command);

        void Alive(int id);

        void Stop(int id);
    }
}
