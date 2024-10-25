using linker.libs;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.decenter
{
    public sealed partial class DecenterTypesLoader
    {
        public DecenterTypesLoader(DecenterTransfer  decenterTransfer, ServiceProvider serviceProvider)
        {
            var types = GetSourceGeneratorTypes();
            var syncs = types.Select(c => (IDecenter)serviceProvider.GetService(c)).Where(c => c != null).ToList();
            decenterTransfer.LoadDecenters(syncs);

            LoggerHelper.Instance.Info($"load decenter transport:{string.Join(",", syncs.Select(c => c.GetType().Name))}");
        }
    }
}
