using cmonitor.plugins.command.messenger;
using cmonitor.plugins.command.report;
using cmonitor.plugins.signin.messenger;
using cmonitor.server;
using common.libs.extends;
using MemoryPack;
using common.libs.api;
using cmonitor.plugins.sapi;

namespace cmonitor.plugins.command
{
    public sealed class CommandApiController : IApiServerController
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        public CommandApiController(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }
        public async Task<bool> Exec(ApiControllerParamsInfo param)
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

        public async Task<int> CommandStart(ApiControllerParamsInfo param)
        {
            if (signCaching.Get(param.Content, out SignCacheInfo cache) && cache.Connected)
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)CommandMessengerIds.CommandStart
                });
                if (resp.Code == MessageResponeCodes.OK && resp.Data.Length == 4)
                {
                    return BitConverter.ToInt32(resp.Data.Span);
                }
            }
            return 0;
        }

        public async Task<bool> CommandWrite(ApiControllerParamsInfo param)
        {
            CommandWriteInfo info = param.Content.DeJson<CommandWriteInfo>();
            if (signCaching.Get(info.Name, out SignCacheInfo cache) && cache.Connected)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)CommandMessengerIds.CommandWrite,
                    Payload = MemoryPackSerializer.Serialize(info.Write)
                });
            }

            return true;
        }

        public async Task<bool> CommandStop(ApiControllerParamsInfo param)
        {
            CommandStopInfo info = param.Content.DeJson<CommandStopInfo>();
            if (signCaching.Get(info.Name, out SignCacheInfo cache) && cache.Connected)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)CommandMessengerIds.CommandStop,
                    Payload = BitConverter.GetBytes(info.Id)
                });
            }

            return true;
        }

        public async Task<bool> CommandAlive(ApiControllerParamsInfo param)
        {
            CommandAliveInfo info = param.Content.DeJson<CommandAliveInfo>();
            if (signCaching.Get(info.Name, out SignCacheInfo cache) && cache.Connected)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)CommandMessengerIds.CommandAlive,
                    Payload = BitConverter.GetBytes(info.Id)
                });
            }

            return true;
        }

    }

    public sealed class CommandWriteInfo
    {
        public string Name { get; set; }
        public CommandLineWriteInfo Write { get; set; }
    }

    public sealed class CommandAliveInfo
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

    public sealed class CommandStopInfo
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

    public sealed class ExecInfo
    {
        public string[] Names { get; set; }
        public string[] Commands { get; set; }
    }

}
