using linker.libs;
using linker.plugins.signin.messenger;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.signIn.args
{
    public sealed class SignInArgsTransfer
    {
        private List<ISignInArgs> startups;

        public SignInArgsTransfer(ServiceProvider serviceProvider)
        {
            var types = ReflectionHelper.GetInterfaceSchieves(typeof(ISignInArgs));
            startups = types.Select(c => serviceProvider.GetService(c) as ISignInArgs).Where(c => c != null).ToList();
        }

        public async Task<string> Invoke(Dictionary<string, string> args)
        {
            foreach (var item in startups)
            {
                string result = await item.Invoke(args);
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
                string result = await item.Verify(signInfo, cache);
                if (string.IsNullOrWhiteSpace(result) == false)
                {
                    return result;
                }
            }
            return string.Empty;
        }
    }
}
