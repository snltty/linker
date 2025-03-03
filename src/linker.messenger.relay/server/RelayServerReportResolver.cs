using linker.libs;
using linker.libs.extends;
using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.relay.server
{
    /// <summary>
    /// 中继节点报告处理器
    /// </summary>
    public class RelayServerReportResolver : IResolver
    {
        public byte Type => (byte)ResolverType.RelayReport;

        private readonly RelayServerMasterTransfer relayServerTransfer;
        private readonly IRelayServerMasterStore relayServerMasterStore;
        private readonly IMessengerResolver messengerResolver;

        public RelayServerReportResolver(RelayServerMasterTransfer relayServerTransfer, IRelayServerMasterStore relayServerMasterStore, IMessengerResolver messengerResolver)
        {
            this.relayServerTransfer = relayServerTransfer;
            this.relayServerMasterStore = relayServerMasterStore;
            this.messengerResolver = messengerResolver;
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
                int length = await socket.ReceiveAsync(buffer.AsMemory(0, 1), SocketFlags.None).ConfigureAwait(false);
                AddReceive((ulong)length);
                await socket.ReceiveAsync(buffer.AsMemory(0, length), SocketFlags.None).ConfigureAwait(false);

                string key = buffer.AsMemory(0, length).GetString();
                if (relayServerMasterStore.Master.SecretKey.Md5() == key)
                {
                    await messengerResolver.BeginReceiveServer(socket, Helper.EmptyArray);
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
            await Task.CompletedTask;
        }
    }
}
