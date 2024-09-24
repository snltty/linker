using linker.config;
using linker.plugins.messenger;
using linker.plugins.resolver;
using MemoryPack;

namespace linker.plugins.server.messenger
{
    public sealed class ServerMessenger : IMessenger
    {
        private readonly MessengerResolver messengerResolver;
        private readonly ResolverTransfer resolverTransfer;

        private DateTime start = DateTime.Now;

        public ServerMessenger(MessengerResolver messengerResolver, ResolverTransfer resolverTransfer)
        {
            this.messengerResolver = messengerResolver;
            this.resolverTransfer = resolverTransfer;
        }

        [MessengerId((ushort)ServerMessengerIds.Flow)]
        public void Flow(IConnection connection)
        {
            ServerFlowInfo serverFlowInfo = new ServerFlowInfo
            {
                Messangers = messengerResolver.GetFlows(),
                Resolvers = resolverTransfer.GetFlows(),
                Start = start,
                Now = DateTime.Now,
            };
            connection.Write(MemoryPackSerializer.Serialize(serverFlowInfo));
        }

    }

}
