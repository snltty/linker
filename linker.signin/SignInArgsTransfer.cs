namespace linker.messenger.signin
{
    public sealed partial class SignInArgsTransfer
    {
        private List<ISignInArgs> startups;

        public SignInArgsTransfer()
        {
        }

        public void LoadArgs(List<ISignInArgs> list)
        {
            startups = list;
        }

        public async Task<string> Invoke(string host, Dictionary<string, string> args)
        {
            foreach (var item in startups)
            {
                string result = await item.Invoke(host,args);
                if (string.IsNullOrWhiteSpace(result) == false)
                {
                    return result;
                }
            }
            return string.Empty;
        }
        public async Task<string> Verify(SignInfo signInfo, SignCacheInfo cache)
        {
            foreach (var item in startups)
            {
                string result = await item.Validate(signInfo, cache);
                if (string.IsNullOrWhiteSpace(result) == false)
                {
                    return result;
                }
            }
            return string.Empty;
        }
    }
}
