namespace linker.messenger.signin.args
{
    public sealed class SignInArgsLimitServer : ISignInArgsServer
    {
        public string Name => "limit";
        public SignInArgsLevel Level => SignInArgsLevel.Low;
        public async Task<string> Validate(SignInfo signInfo, SignCacheInfo cache)
        {
            return await Task.FromResult(string.Empty);
        }
    }
}
