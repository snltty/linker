namespace linker.messenger.signin
{
    public interface ISignInArgs
    {
        public Task<string> Invoke(string host, Dictionary<string, string> args);
        public Task<string> Validate(SignInfo signInfo, SignCacheInfo cache);
    }

}
