using linker.libs;
using linker.libs.extends;
using linker.plugins.resolver;
using System.Net;
using System.Net.Sockets;

namespace linker.plugins.relay.server
{
    public class RelayReportResolver : IResolver
    {
        public ResolverType Type => ResolverType.RelayReport;

        private readonly ICrypto cryptoMaster;
        private readonly ICrypto cryptoNode;

        private readonly RelayServerTransfer relayServerTransfer;
        public RelayReportResolver(RelayServerTransfer relayServerTransfer)
        {
            this.relayServerTransfer = relayServerTransfer;
        }

        public async Task Resolve(Socket socket, Memory<byte> memory)
        {
            socket.SafeClose();
            await Task.CompletedTask;
        }

        public async Task Resolve(Socket socket, IPEndPoint ep, Memory<byte> memory)
        {
            relayServerTransfer.SetNodeReport(ep, memory);
            await Task.CompletedTask;
        }
    }
}
