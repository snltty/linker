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

        public bool Invoke(Dictionary<string, string> args)
        {
            foreach (var item in startups)
            {
                if(item.Invoke(args) == false)
                {
                    return false;
                }
            }
            return true;
        }
        public bool Verify(SignInfo signInfo, SignCacheInfo cache, out string msg)
        {
            msg = string.Empty;
            foreach (var item in startups)
            {
                if (item.Verify(signInfo, cache,out msg) == false)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
