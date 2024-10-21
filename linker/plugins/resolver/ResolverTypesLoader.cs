using linker.libs;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.resolver
{
    public sealed partial class ResolverTypesLoader
    {
        public ResolverTypesLoader(ResolverTransfer resolverTransfer, ServiceProvider serviceProvider)
        {
            var types = GetSourceGeneratorTypes();
            var resolvers = types.Select(c => (IResolver)serviceProvider.GetService(c)).Where(c => c != null).ToList();
            resolverTransfer.LoadResolvers(resolvers);

            LoggerHelper.Instance.Info($"load resolvers:{string.Join(",", resolvers.Select(c => c.GetType().Name))}");
        }
    }
}
