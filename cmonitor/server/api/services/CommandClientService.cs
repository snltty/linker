using cmonitor.server.service;
using cmonitor.server.service.messengers.command;
using cmonitor.server.service.messengers.sign;
using common.libs;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.server.api.services
{
    public sealed class CommandClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        private readonly IClientServer clientServer;
        public CommandClientService(MessengerSender messengerSender, SignCaching signCaching, IClientServer clientServer)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.clientServer = clientServer;
        }
        public async Task<bool> Exec(ClientServiceParamsInfo param)
        {
            ExecInfo info = param.Content.DeJson<ExecInfo>();
            for (int i = 0; i < info.Names.Length; i++)
            {
                if (signCaching.Get(info.Names[i], out SignCacheInfo cache) && cache.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)CommandMessengerIds.Exec,
                        Payload = MemoryPackSerializer.Serialize(info.Commands)
                    });
                }
            }

            return true;
        }

    }

    public sealed class ExecInfo
    {
        public string[] Names { get; set; }
        public string[] Commands { get; set; }
    }
}
