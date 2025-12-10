using linker.libs;
using linker.libs.extends;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.node
{
    public class NodeConnectionResolver : IResolver
    {
        public virtual byte Type => (byte)ResolverType.NodeConnection;

        private readonly IMessengerResolver messengerResolver;

        public NodeConnectionResolver(IMessengerResolver messengerResolver)
        {
            this.messengerResolver = messengerResolver;
        }

        public virtual void Add(long receiveBytes, long sendtBytes)
        {
        }

        public async Task Resolve(Socket socket, Memory<byte> memory)
        {
            try
            {
                await messengerResolver.BeginReceiveServer(socket, Helper.EmptyArray).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                socket.SafeClose();
            }
        }

        public async Task Resolve(Socket socket, IPEndPoint ep, Memory<byte> memory)
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
