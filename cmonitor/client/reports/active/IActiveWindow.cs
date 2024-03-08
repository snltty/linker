namespace cmonitor.client.reports.active
{
    public interface IActiveWindow
    {
        public void DisallowRun(string[] names);

        public ActiveWindowInfo GetActiveWindow();

        public int GetWindowCount();
        public Dictionary<uint, string> GetWindows();

        public void Kill(uint pid);
    }

    public sealed class ActiveWindowInfo
    {
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public uint Pid { get; set; }
    }
}
