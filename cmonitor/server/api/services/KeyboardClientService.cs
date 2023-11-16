using cmonitor.server.client.reports.command;
using cmonitor.server.service;
using cmonitor.server.service.messengers.keyboard;
using cmonitor.server.service.messengers.sign;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.server.api.services
{
    public sealed class KeyboardClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        public KeyboardClientService(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
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
                        MessengerId = (ushort)KeyboardMessengerIds.Keyboard,
                        Payload = bytes
                    });
                }
            }

            return true;
        }

        public async Task<bool> CtrlAltDelete(ClientServiceParamsInfo param)
        {
            string[] names = param.Content.DeJson<string[]>();
            for (int i = 0; i < names.Length; i++)
            {
                if (signCaching.Get(names[i], out SignCacheInfo cache) && cache.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)KeyboardMessengerIds.CtrlAltDelete
                    });
                }
            }

            return true;
        }


        public async Task<bool> MouseSet(ClientServiceParamsInfo param)
        {
            MouseSetInfo info = param.Content.DeJson<MouseSetInfo>();
            byte[] bytes = MemoryPackSerializer.Serialize(info.Input);
            for (int i = 0; i < info.Names.Length; i++)
            {
                if (signCaching.Get(info.Names[i], out SignCacheInfo cache) && cache.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)KeyboardMessengerIds.MouseSet,
                        Payload = bytes
                    });
                }
            }

            return true;
        }
        public async Task<bool> MouseClick(ClientServiceParamsInfo param)
        {
            MouseClickInfo info = param.Content.DeJson<MouseClickInfo>();
            byte[] bytes = MemoryPackSerializer.Serialize(info.Input);
            for (int i = 0; i < info.Names.Length; i++)
            {
                if (signCaching.Get(info.Names[i], out SignCacheInfo cache) && cache.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)KeyboardMessengerIds.MouseClick,
                        Payload = bytes
                    });
                }
            }

            return true;
        }
    }

    public sealed class KeyBoardInfo
    {
        public string[] Names { get; set; }
        public KeyBoardInputInfo Input { get; set; }
    }
    public sealed class MouseSetInfo
    {
        public string[] Names { get; set; }
        public MouseSetInfo Input { get; set; }
    }
    public sealed class MouseClickInfo
    {
        public string[] Names { get; set; }
        public MouseClickInfo Input { get; set; }
    }
}
