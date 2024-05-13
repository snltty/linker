using cmonitor.plugins.signin.messenger;
using cmonitor.plugins.tuntap.vea;
using cmonitor.server;
using common.libs;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.plugins.tuntap.messenger
{
    public sealed class TuntapClientMessenger : IMessenger
    {
        private readonly TuntapTransfer tuntapTransfer;
        public TuntapClientMessenger(TuntapTransfer tuntapTransfer)
        {
            this.tuntapTransfer = tuntapTransfer;
        }

        [MessengerId((ushort)TuntapMessengerIds.Info)]
        public void Info(IConnection connection)
        {
            connection.Write(MemoryPackSerializer.Serialize(tuntapTransfer.GetInfo()));
        }

        [MessengerId((ushort)TuntapMessengerIds.Change)]
        public void Change(IConnection connection)
        {
            tuntapTransfer.OnChange();
        }

        [MessengerId((ushort)TuntapMessengerIds.Run)]
        public void Run(IConnection connection)
        {
            _ = tuntapTransfer.Run();
        }

        [MessengerId((ushort)TuntapMessengerIds.Stop)]
        public void Stop(IConnection connection)
        {
            _ = tuntapTransfer.Stop();
        }

        [MessengerId((ushort)TuntapMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            TuntapInfo info = MemoryPackSerializer.Deserialize<TuntapInfo>(connection.ReceiveRequestWrap.Payload.Span);
            tuntapTransfer.Update(info);
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

        [MessengerId((ushort)TuntapMessengerIds.InfoForward)]
        public async Task InfoForward(IConnection connection)
        {
            if (signCaching.Get(connection.Name, out SignCacheInfo cache))
            {
                List<SignCacheInfo> caches = signCaching.Get(cache.GroupId);
                List<Task<MessageResponeInfo>> tasks = new List<Task<MessageResponeInfo>>();
                foreach (SignCacheInfo item in caches.Where(c => c.MachineName != connection.Name))
                {
                    tasks.Add(messengerSender.SendReply(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        Timeout = 3000,
                        MessengerId = (ushort)TuntapMessengerIds.Info
                    }));
                }
                if(tasks.Count > 0)
                {
                    await Task.WhenAll(tasks);

                    List<TuntapInfo> ips = tasks.Where(c => c.Result.Code == MessageResponeCodes.OK)
                         .Select(c => MemoryPackSerializer.Deserialize<TuntapInfo>(c.Result.Data.Span)).ToList();
                    connection.Write(MemoryPackSerializer.Serialize(ips));
                }
                else
                {
                    connection.Write(MemoryPackSerializer.Serialize(new List<TuntapInfo>()));
                }
            }
        }


        [MessengerId((ushort)TuntapMessengerIds.ChangeForward)]
        public void ChangeForward(IConnection connection)
        {
            if (signCaching.Get(connection.Name, out SignCacheInfo cache))
            {
                List<SignCacheInfo> caches = signCaching.Get(cache.GroupId);

                foreach (SignCacheInfo item in caches.Where(c => c.MachineName != connection.Name))
                {
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        Timeout = 3000,
                        MessengerId = (ushort)TuntapMessengerIds.Change
                    });
                }
            }
        }


        [MessengerId((ushort)TuntapMessengerIds.RunForward)]
        public void RunForward(IConnection connection)
        {
            string name = MemoryPackSerializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.Get(name, out SignCacheInfo cache))
            {
                _ = messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    Timeout = 3000,
                    MessengerId = (ushort)TuntapMessengerIds.Run
                });
            }
        }


        [MessengerId((ushort)TuntapMessengerIds.StopForward)]
        public void StopForward(IConnection connection)
        {
            string name = MemoryPackSerializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.Get(name, out SignCacheInfo cache))
            {
                _ = messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    Timeout = 3000,
                    MessengerId = (ushort)TuntapMessengerIds.Stop
                });
            }
        }


        [MessengerId((ushort)TuntapMessengerIds.UpdateForward)]
        public void UpdateForward(IConnection connection)
        {
            TuntapInfo info = MemoryPackSerializer.Deserialize<TuntapInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.Get(info.MachineName, out SignCacheInfo cache))
            {
                _ = messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    Timeout = 3000,
                    MessengerId = (ushort)TuntapMessengerIds.Update,
                     Payload= connection.ReceiveRequestWrap.Payload
                });
            }
        }
    }
}
