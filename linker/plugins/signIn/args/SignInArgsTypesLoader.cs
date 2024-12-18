
using linker.libs;
using linker.messenger.signin;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.signIn.args
{
    public sealed partial class SignInArgsTypesLoader
    {
        public SignInArgsTypesLoader(SignInArgsTransfer signInArgsTransfer, ServiceProvider serviceProvider)
        {
            var types = GetSourceGeneratorTypes();
            var args = types.Select(c => (ISignInArgs)serviceProvider.GetService(c)).Where(c => c != null).ToList();
            signInArgsTransfer.LoadArgs(args);

            LoggerHelper.Instance.Info($"load sign in args:{string.Join(",", args.Select(c => c.GetType().Name))}");
        }
    }
}
