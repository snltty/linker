namespace cmonitor.client.runningConfig
{
    public sealed class RunningConfigLinux : IRunningConfig
    {
        public T Get<T>(T defaultValue)
        {
            return defaultValue;
        }

        public void Set<T>(T data)
        {

        }
    }
}
