namespace linker.messenger.updater
{
    public interface IUpdaterServerStore
    {
        public bool Sync2Server { get; }



        public bool Confirm();
    }
}
