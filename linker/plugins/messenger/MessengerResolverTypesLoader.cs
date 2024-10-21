using linker.libs;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.messenger
{
    public sealed partial class MessengerResolverTypesLoader
    {
        public MessengerResolverTypesLoader(IMessengerResolver messengerResolver, ServiceProvider serviceProvider)
        {
            var types = GetSourceGeneratorTypes();
            var messengers = types.Select(c => (IMessenger)serviceProvider.GetService(c)).Where(c => c != null).ToList();
            messengerResolver.LoadMessenger(messengers);

            LoggerHelper.Instance.Info($"load messengers :{string.Join(",", messengers.Select(c => c.GetType().Name))}");
        }
    }
}
