using linker.client.config;
using linker.plugins.messenger;
using linker.plugins.signin.messenger;
using LiteDB;
using MemoryPack;
using System.Net;

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
                        }, (ushort)ForwardMessengerIds.TestForward).ConfigureAwait(false);
                    }
                });
            }
        }

        [MessengerId((ushort)ForwardMessengerIds.GetForward)]
        public void GetForward(IConnection connection)
        {
            GetForwardInfo info = MemoryPackSerializer.Deserialize<GetForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(info.MachineId, out SignCacheInfo cache) && signCaching.TryGet(connection.Id, out SignCacheInfo cache1) && cache1.GroupId == cache.GroupId)
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                sender.SendReply(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)ForwardMessengerIds.Get,
                    Payload = connection.ReceiveRequestWrap.Payload
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK)
                    {
                        await sender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Code = MessageResponeCodes.OK,
                            Payload = result.Result.Data,
                            RequestId = requestid
                        }, (ushort)ForwardMessengerIds.GetForward).ConfigureAwait(false);
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
                }, (ushort)ForwardMessengerIds.Test).ConfigureAwait(false);
            });
        }

        [MessengerId((ushort)ForwardMessengerIds.Get)]
        public void Get(IConnection connection)
        {
            GetForwardInfo info = MemoryPackSerializer.Deserialize<GetForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (forwardTransfer.Get().TryGetValue(info.ToMachineId, out List<ForwardInfo> list))
            {
                var result = list.Select(c => new ForwardRemoteInfo { BufferSize = c.BufferSize, Name = c.Name, Port = c.Port, TargetEP = c.TargetEP }).ToList();
                connection.Write(MemoryPackSerializer.Serialize(result));
                return;
            }
            connection.Write(MemoryPackSerializer.Serialize(new List<ForwardRemoteInfo>()));
        }
    }

    [MemoryPackable]
    public sealed partial class ForwardRemoteInfo
    {
        public string Name { get; set; }
        public int Port { get; set; }

        [MemoryPackAllowSerialize]
        public IPEndPoint TargetEP { get; set; }

        public byte BufferSize { get; set; } = 3;
    }

}
