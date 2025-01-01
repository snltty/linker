namespace linker.messenger.updater
{
    public interface IUpdaterClientStore
    {
        public string SecretKey { get; }
      
        public void SetSecretKey(string key);
    }
}
