using linker.client.config;
using linker.plugins.signin.messenger;
using linker.server;
using MemoryPack;

namespace linker.plugins.forward.messenger
{
    public sealed class ForwardServerMessenger : IMessenger
    {

        private readonly MessengerSender sender;
        private readonly SignCaching signCaching;

        public ForwardServerMessenger(MessengerSender sender, SignCaching signCaching)
        {
            this.sender = sender;
            this.signCaching = signCaching;
        }

        [MessengerId((ushort)ForwardMessengerIds.TestForward)]
        public void TestForward(IConnection connection)
        {
            ForwardTestInfo forwardTestInfo = MemoryPackSerializer.Deserialize<ForwardTestInfo>(connection.ReceiveRequestWrap.Payload.Span);

            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache1) && signCaching.TryGet(forwardTestInfo.MachineId, out SignCacheInfo cache2) && cache1.GroupId == cache2.GroupId)
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                sender.SendReply(new MessageRequestWrap
                {
                    Connection = cache2.Connection,
                    MessengerId = (ushort)ForwardMessengerIds.Test,
                    Payload = connection.ReceiveRequestWrap.Payload
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK)
                    {
                        await sender.ReplyOnly(new MessageResponseWrap
                        {
                            Code = MessageResponeCodes.OK,
                            RequestId = requestid,
                            Connection = connection,
                            Payload = result.Result.Data
                        });
                    }
                });
            }
        }
    }

    public sealed class ForwardClientMessenger : IMessenger
    {
        private readonly ForwardTransfer forwardTransfer;
        private readonly MessengerSender sender;

        public ForwardClientMessenger(ForwardTransfer forwardTransfer, MessengerSender sender)
        {
            this.forwardTransfer = forwardTransfer;
            this.sender = sender;
        }

        [MessengerId((ushort)ForwardMessengerIds.Test)]
        public void Test(IConnection connection)
        {
            ForwardTestInfo forwardTestInfo = MemoryPackSerializer.Deserialize<ForwardTestInfo>(connection.ReceiveRequestWrap.Payload.Span);

            uint requestid = connection.ReceiveRequestWrap.RequestId;
            forwardTransfer.Test(forwardTestInfo).ContinueWith(async (result) =>
            {
                await sender.ReplyOnly(new MessageResponseWrap
                {
                    Code = MessageResponeCodes.OK,
                    RequestId = requestid,
                    Connection = connection,
                    Payload = MemoryPackSerializer.Serialize(result.Result)
                });
            });
        }
    }


}
