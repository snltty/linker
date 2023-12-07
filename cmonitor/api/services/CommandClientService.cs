using cmonitor.client.reports.command;
using cmonitor.service;
using cmonitor.service.messengers.command;
using cmonitor.service.messengers.sign;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.api.services
{
    public sealed class CommandClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        public CommandClientService(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }
        public async Task<bool> Exec(ClientServiceParamsInfo param)
        {
            ExecInfo info = param.Content.DeJson<ExecInfo>();
            byte[] bytes = MemoryPackSerializer.Serialize(info.Commands);
            for (int i = 0; i < info.Names.Length; i++)
            {
                if (signCaching.Get(info.Names[i], out SignCacheInfo cache) && cache.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)CommandMessengerIds.Exec,
                        Payload = bytes
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
