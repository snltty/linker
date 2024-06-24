using Linker.Config;
using Linker.Libs;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Linker.Client.Args
{
    public sealed class SignInArgsTransfer
    {
        private List<ISignInArgs> startups;

        public SignInArgsTransfer(ServiceProvider serviceProvider, ConfigWrap config)
        {
            var types = ReflectionHelper.GetInterfaceSchieves(typeof(ISignInArgs));
            startups = types.Select(c => serviceProvider.GetService(c) as ISignInArgs).Where(c=>c != null).ToList();
        }

        public void Invoke(Dictionary<string, string> args)
        {
            foreach (var item in startups)
            {
                item.Invoke(args);
            }
        }
    }
}
