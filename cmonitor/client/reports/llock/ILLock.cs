namespace cmonitor.client.reports.llock
{
    public interface ILLock
    {
        public void Set(bool value);
        public void LockSystem();
    }
}
