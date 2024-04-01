namespace cmonitor.plugins.active.report
{
    public sealed class ActiveWindowMacOS : IActiveWindow
    {
        public void DisallowRun(string[] names)
        {

        }
        public void Kill(uint pid)
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
