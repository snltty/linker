namespace linker.messenger.updater
{
    public interface IUpdaterCommonStore
    {

        public string UpdateUrl { get; }
        public int UpdateIntervalSeconds { get; }
        public bool CheckUpdate { get; }

        public void SetUrl(string url);
        public void SetInterval(int sec);
        public void SetCheck(bool checke);
        public void Confirm();
    }
}
