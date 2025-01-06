using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;
using System.Text.Json;
using linker.libs.extends;
namespace linker.messenger
{
    public static class Entry
    {
        public static ServiceCollection AddMessenger(this ServiceCollection serviceCollection)
        {
            SerialzeExtends.AddJsonConverter(new IConnectionConverter());

            serviceCollection.AddSingleton<IMessengerSender, MessengerSender>();
            serviceCollection.AddSingleton<IMessengerResolver, MessengerResolver>();
            serviceCollection.AddSingleton<MessengerResolverResolver>();
            serviceCollection.AddSingleton<ResolverTransfer>();
            return serviceCollection;
        }
        public static ServiceProvider UseMessenger(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();

            ResolverTransfer resolverTransfer = serviceProvider.GetService<ResolverTransfer>();
            resolverTransfer.AddResolvers(new List<IResolver> { serviceProvider.GetService<MessengerResolverResolver>() });

            return serviceProvider;
        }

    }

    public sealed class IConnectionConverter : JsonConverter<IConnection>
    {
        public override IConnection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return null;
        }

        public override void Write(Utf8JsonWriter writer, IConnection value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(string.Empty);
        }
    }
}
