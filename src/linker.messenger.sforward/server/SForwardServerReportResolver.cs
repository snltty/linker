using linker.libs;
using linker.libs.extends;
using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.sforward.server
{
    /// <summary>
    /// 穿透节点报告处理器
    /// </summary>
    public class SForwardServerReportResolver : IResolver
    {
        public byte Type => (byte)ResolverType.SForwardReport;

        private readonly ISForwardServerMasterStore sForwardServerMasterStore;
        private readonly IMessengerResolver messengerResolver;

        public SForwardServerReportResolver(ISForwardServerMasterStore sForwardServerMasterStore, IMessengerResolver messengerResolver)
        {
            this.sForwardServerMasterStore = sForwardServerMasterStore;
            this.messengerResolver = messengerResolver;
        }

        public virtual void Add(long receiveBytes, long sendtBytes)
        {
        }

        public async Task Resolve(Socket socket, Memory<byte> memory)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
            try
            {


                await socket.ReceiveAsync(buffer.AsMemory(0, 1), SocketFlags.None).ConfigureAwait(false);
                int length = buffer[0];
                Add(memory.Length, length);
                await socket.ReceiveAsync(buffer.AsMemory(0, length), SocketFlags.None).ConfigureAwait(false);

                string key = buffer.AsMemory(0, length).GetString();

                if (sForwardServerMasterStore.Master.SecretKey.Sha256() == key)
                {
                    await messengerResolver.BeginReceiveServer(socket, Helper.EmptyArray).ConfigureAwait(false);
                }
                else
                {
                    socket.SafeClose();
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                socket.SafeClose();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public async Task Resolve(Socket socket, IPEndPoint ep, Memory<byte> memory)
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
