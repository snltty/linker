namespace cmonitor.plugins.llock.report
{
    public interface ILLock
    {
        public void Set(bool value);
        public void LockSystem();
    }
}
