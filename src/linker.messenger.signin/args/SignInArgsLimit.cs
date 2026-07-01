namespace linker.messenger.signin.args
{
    public sealed class SignInArgsLimitServer : ISignInArgsServer
    {
        public string Name => "limit";
        public SignInArgsLevel Level => SignInArgsLevel.Low;
        public Task<string> Validate(SignInfo signInfo, SignCacheInfo cache)
        {
            return Task.FromResult(string.Empty);
        }
    }
}
