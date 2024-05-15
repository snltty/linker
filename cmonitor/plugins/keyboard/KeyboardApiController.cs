using cmonitor.plugins.keyboard.messenger;
using cmonitor.plugins.keyboard.report;
using cmonitor.plugins.signin.messenger;
using cmonitor.server;
using common.libs.extends;
using MemoryPack;
using common.libs.api;
using cmonitor.plugins.sapi;

namespace cmonitor.plugins.keyboard
{
    public sealed class KeyboardApiController : IApiServerController
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        public KeyboardApiController(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }

        public async Task<bool> KeyBoard(ApiControllerParamsInfo param)
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

        public async Task<bool> CtrlAltDelete(ApiControllerParamsInfo param)
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


        public async Task<bool> MouseSet(ApiControllerParamsInfo param)
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
        public async Task<bool> MouseClick(ApiControllerParamsInfo param)
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
        public report.MouseSetInfo Input { get; set; }
    }
    public sealed class MouseClickInfo
    {
        public string[] Names { get; set; }
        public MouseClickInfo Input { get; set; }
    }
}
