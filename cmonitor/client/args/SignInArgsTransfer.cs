using cmonitor.config;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace cmonitor.client.args
{
    public sealed class SignInArgsTransfer
    {
        private List<ISignInArgs> startups;

        public SignInArgsTransfer(ServiceProvider serviceProvider, Config config)
        {
            var types = ReflectionHelper.GetInterfaceSchieves(typeof(ISignInArgs));
            types = config.Data.Common.PluginContains(types);
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
