namespace cmonitor.client.reports.active
{
    public sealed class ActiveWindowLinux : IActiveWindow
    {
        public void DisallowRun(string[] names)
        {

        }
        public void Kill(int pid)
        {

        }

        public ActiveWindowInfo GetActiveWindow()
        {
            return new ActiveWindowInfo();
        }

        public int GetWindowCount()
        {
            return 0;
        }

        public Dictionary<uint, string> GetWindows()
        {
            return new Dictionary<uint, string>();
        }

    }
}
