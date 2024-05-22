using cmonitor.plugins.signin.messenger;
using cmonitor.plugins.tuntap.vea;
using cmonitor.server;
using MemoryPack;
using System.Collections.Concurrent;

namespace cmonitor.plugins.tuntap.messenger
{
    public sealed class TuntapClientMessenger : IMessenger
    {
        private readonly TuntapTransfer tuntapTransfer;
        public TuntapClientMessenger(TuntapTransfer tuntapTransfer)
        {
            this.tuntapTransfer = tuntapTransfer;
        }

        [MessengerId((ushort)TuntapMessengerIds.Run)]
        public void Run(IConnection connection)
        {
            tuntapTransfer.Run();
        }

        [MessengerId((ushort)TuntapMessengerIds.Stop)]
        public void Stop(IConnection connection)
        {
            tuntapTransfer.Stop();
        }

        [MessengerId((ushort)TuntapMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            TuntapInfo info = MemoryPackSerializer.Deserialize<TuntapInfo>(connection.ReceiveRequestWrap.Payload.Span);
            tuntapTransfer.OnUpdate(info);
        }

        [MessengerId((ushort)TuntapMessengerIds.Config)]
        public void Config(IConnection connection)
        {
            TuntapInfo info = MemoryPackSerializer.Deserialize<TuntapInfo>(connection.ReceiveRequestWrap.Payload.Span);
            TuntapInfo _info = tuntapTransfer.OnConfig(info);
            connection.Write(MemoryPackSerializer.Serialize(_info));
        }
    }


    public sealed class TuntapServerMessenger : IMessenger
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;

        public TuntapServerMessenger(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }

        [MessengerId((ushort)TuntapMessengerIds.RunForward)]
        public async Task RunForward(IConnection connection)
        {
            string name = MemoryPackSerializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.Get(name, out SignCacheInfo cache))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    Timeout = 3000,
                    MessengerId = (ushort)TuntapMessengerIds.Run
                });
            }
        }


        [MessengerId((ushort)TuntapMessengerIds.StopForward)]
        public async Task StopForward(IConnection connection)
        {
            string name = MemoryPackSerializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.Get(name, out SignCacheInfo cache))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    Timeout = 3000,
                    MessengerId = (ushort)TuntapMessengerIds.Stop
                });
            }
        }


        [MessengerId((ushort)TuntapMessengerIds.UpdateForward)]
        public async Task UpdateForward(IConnection connection)
        {
            TuntapInfo info = MemoryPackSerializer.Deserialize<TuntapInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.Get(info.MachineName, out SignCacheInfo cache))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    Timeout = 3000,
                    MessengerId = (ushort)TuntapMessengerIds.Update,
                    Payload = connection.ReceiveRequestWrap.Payload
                });
            }
        }


        [MessengerId((ushort)TuntapMessengerIds.ConfigForward)]
        public async Task ConfigForward(IConnection connection)
        {
            if (signCaching.Get(connection.Name, out SignCacheInfo cache))
            {
                List<SignCacheInfo> caches = signCaching.Get(cache.GroupId);

                List<Task<MessageResponeInfo>> tasks = new List<Task<MessageResponeInfo>>();
                foreach (SignCacheInfo item in caches.Where(c => c.MachineName != connection.Name && c.Connected))
                {
                    tasks.Add(messengerSender.SendReply(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        MessengerId = (ushort)TuntapMessengerIds.Config,
                        Payload = connection.ReceiveRequestWrap.Payload
                    }));
                }

                await Task.WhenAll(tasks);

                List<TuntapInfo> results = tasks.Where(c => c.Result.Code == MessageResponeCodes.OK).Select(c => MemoryPackSerializer.Deserialize<TuntapInfo>(c.Result.Data.Span)).ToList();
                connection.Write(MemoryPackSerializer.Serialize(results));
            }
        }
    }
}
