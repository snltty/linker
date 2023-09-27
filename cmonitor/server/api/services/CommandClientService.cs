using cmonitor.server.client.reports.command;
using cmonitor.server.service;
using cmonitor.server.service.messengers.command;
using cmonitor.server.service.messengers.sign;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.server.api.services
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

        public async Task<bool> KeyBoard(ClientServiceParamsInfo param)
        {
            KeyBoardInfo info = param.Content.DeJson<KeyBoardInfo>();
            byte[] bytes = MemoryPackSerializer.Serialize(info.Input);
            for (int i = 0; i < info.Names.Length; i++)
            {
                if (signCaching.Get(info.Names[i], out SignCacheInfo cache) && cache.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)CommandMessengerIds.KeyBoard,
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

    public sealed class KeyBoardInfo
    {
        public string[] Names { get; set; }
        public KeyBoardInputInfo Input { get; set; }
    }
}
