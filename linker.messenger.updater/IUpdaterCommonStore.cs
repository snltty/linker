namespace linker.messenger.updater
{
    public interface IUpdaterCommonStore
    {

        public string UpdateUrl { get; }
        public int UpdateIntervalSeconds { get; }
        public void SetInterval(int sec);
    }
}
