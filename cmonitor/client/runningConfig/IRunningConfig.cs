namespace cmonitor.client.runningConfig
{
    public interface IRunningConfig
    {
        public T Get<T>(T defaultValue);
        public void Set<T>(T data);
    }
}
