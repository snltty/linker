using linker.plugins.signin.messenger;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.signIn.args
{
    public sealed partial class SignInArgsTransfer
    {
        private List<ISignInArgs> startups;

        public SignInArgsTransfer(ServiceProvider serviceProvider)
        {
            var types = GetSourceGeneratorTypes();
            startups = types.Select(c => serviceProvider.GetService(c) as ISignInArgs).Where(c => c != null).ToList();
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
