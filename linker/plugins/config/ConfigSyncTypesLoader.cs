using linker.libs;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.config
{
    public sealed partial class ConfigSyncTypesLoader
    {
        public ConfigSyncTypesLoader(ConfigSyncTreansfer configSyncTreansfer, ServiceProvider serviceProvider)
        {
            var types = GetSourceGeneratorTypes();
            var syncs = types.Select(c => (IConfigSync)serviceProvider.GetService(c)).Where(c => c != null).ToList();
            configSyncTreansfer.LoadConfigSyncs(syncs);

            LoggerHelper.Instance.Info($"load config sync transport:{string.Join(",", syncs.Select(c => c.GetType().Name))}");
        }
    }
}
