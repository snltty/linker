using linker.libs.extends;
using linker.plugins.resolver;
using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace linker.plugins.relay.server
{
    public class RelayReportResolver : IResolver
    {
        public ResolverType Type => ResolverType.RelayReport;

        private readonly RelayServerMasterTransfer relayServerTransfer;
        public RelayReportResolver(RelayServerMasterTransfer relayServerTransfer)
        {
            this.relayServerTransfer = relayServerTransfer;
        }

        public virtual void AddReceive(ulong bytes)
        {
        }
        public virtual void AddSendt(ulong bytes)
        {
        }

        public async Task Resolve(Socket socket, Memory<byte> memory)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
            try
            {
                AddReceive((ulong)memory.Length);
                int length = await socket.ReceiveAsync(buffer.AsMemory(), SocketFlags.None).ConfigureAwait(false);
                AddReceive((ulong)length);

                string key = buffer.AsMemory(0,length).GetString();
                Memory<byte> bytes = relayServerTransfer.TryGetRelayCache(key);
                if (bytes.Length > 0)
                {
                    AddSendt((ulong)bytes.Length);
                    await socket.SendAsync(bytes);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
           
        }

        public async Task Resolve(Socket socket, IPEndPoint ep, Memory<byte> memory)
        {
            AddReceive((ulong)memory.Length);
            relayServerTransfer.SetNodeReport(ep, memory);
            await Task.CompletedTask;
        }
    }
}
