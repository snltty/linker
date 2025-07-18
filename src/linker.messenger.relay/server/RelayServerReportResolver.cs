﻿using linker.libs;
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
                Add(memory.Length,length);
                await socket.ReceiveAsync(buffer.AsMemory(0, length), SocketFlags.None).ConfigureAwait(false);

                string key = buffer.AsMemory(0, length).GetString();

                if (relayServerMasterStore.Master.SecretKey.Sha256() == key)
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
