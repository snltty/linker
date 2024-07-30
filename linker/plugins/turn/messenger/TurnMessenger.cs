using linker.config;
using linker.plugins.signin.messenger;
using linker.libs;
using MemoryPack;
using linker.plugins.messenger;
using linker.plugins.turn.config;

namespace linker.plugins.turn.messenger
{
    public sealed class TurnClientMessenger : IMessenger
    {
        private readonly MessengerSender messengerSender;

        public TurnClientMessenger(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }



    }

    public sealed class TurnServerMessenger : IMessenger
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        public TurnServerMessenger(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }


        [MessengerId((ushort)TurnMessengerIds.TunnelsForward)]
        public void TunnelsForward(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;

                IEnumerable<Task<MessageResponeInfo>> tasks = signCaching.Get(cache.GroupId)
                    .Where(c => c.MachineId != connection.Id && c.Connected)
                    .Select(c => messengerSender.SendReply(new MessageRequestWrap
                    {
                        Connection = c.Connection,
                        MessengerId = (ushort)TurnMessengerIds.Tunnels,
                        Timeout = 3000
                    }));

                Task.WhenAll(tasks).ContinueWith(async (result) =>
                {
                    List<TunnelsInfo> results = tasks.Where(c => c.Result.Code == MessageResponeCodes.OK)
                    .Select(c => new TunnelsInfo { MachineId = c.Result.Connection.Id, MachineIds = MemoryPackSerializer.Deserialize<List<string>>(c.Result.Data.Span) })
                    .ToList();
                    await messengerSender.ReplyOnly(new MessageResponseWrap
                    {
                        Connection = connection,
                        Payload = MemoryPackSerializer.Serialize(results),
                        RequestId = requestid,
                    }).ConfigureAwait(false);
                });
            }
        }

        [MessengerId((ushort)TurnMessengerIds.PortMapsForward)]
        public void PortMapsForward(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;

                IEnumerable<Task<MessageResponeInfo>> tasks = signCaching.Get(cache.GroupId)
                    .Where(c => c.MachineId != connection.Id && c.Connected)
                    .Select(c => messengerSender.SendReply(new MessageRequestWrap
                    {
                        Connection = c.Connection,
                        MessengerId = (ushort)TurnMessengerIds.PortMaps,
                        Timeout = 3000
                    }));

                Task.WhenAll(tasks).ContinueWith(async (result) =>
                {
                    List<string> results = tasks.Where(c => c.Result.Code == MessageResponeCodes.OK && c.Result.Data.Span.SequenceEqual(Helper.TrueArray))
                    .Select(c => c.Result.Connection.Id)
                    .ToList();
                    await messengerSender.ReplyOnly(new MessageResponseWrap
                    {
                        Connection = connection,
                        Payload = MemoryPackSerializer.Serialize(results),
                        RequestId = requestid,
                    }).ConfigureAwait(false);
                });
            }
        }
    }


}
