using cmonitor.plugins.active.messenger;
using cmonitor.plugins.active.report;
using cmonitor.plugins.signin.messenger;
using cmonitor.server;
using common.libs;
using common.libs.extends;
using MemoryPack;
using common.libs.api;
using cmonitor.server.sapi;
using cmonitor.plugins.active.db;

namespace cmonitor.plugins.active
{
    public sealed class ActiveApiController : IApiServerController
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        private readonly IActiveWindowDB activeWindowDB;

        public ActiveApiController(MessengerSender messengerSender, SignCaching signCaching, IActiveWindowDB activeWindowDB)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.activeWindowDB = activeWindowDB;
        }
        public async Task<ActiveWindowTimeReportInfo> Get(ApiControllerParamsInfo param)
        {
            if (signCaching.TryGet(param.Content, out SignCacheInfo cache) && cache.Connected)
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)ActiveMessengerIds.Get
                });
                if (resp.Code == MessageResponeCodes.OK)
                {
                    return MemoryPackSerializer.Deserialize<ActiveWindowTimeReportInfo>(resp.Data.Span);
                }
            }
            return new ActiveWindowTimeReportInfo();
        }
        public async Task<Dictionary<uint, string>> Windows(ApiControllerParamsInfo param)
        {
            if (signCaching.TryGet(param.Content, out SignCacheInfo cache) && cache.Connected)
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)ActiveMessengerIds.Windows
                });
                if (resp.Code == MessageResponeCodes.OK)
                {
                    return MemoryPackSerializer.Deserialize<Dictionary<uint, string>>(resp.Data.Span);
                }
            }
            return new Dictionary<uint, string>();
        }


        public async Task<bool> Clear(ApiControllerParamsInfo param)
        {
            if (signCaching.TryGet(param.Content, out SignCacheInfo cache) && cache.Connected)
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)ActiveMessengerIds.Clear
                });
                return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
            }
            return false;
        }
        public async Task<bool> Disallow(ApiControllerParamsInfo param)
        {
            DisallowInfo disallowInfo = param.Content.DeJson<DisallowInfo>();
            byte[] bytes = MemoryPackSerializer.Serialize(new ActiveDisallowInfo { FileNames = disallowInfo.Data, Ids1 = disallowInfo.Ids1, Ids2 = disallowInfo.Ids2 });
            foreach (string name in disallowInfo.Devices)
            {
                if (signCaching.TryGet(name, out SignCacheInfo cache) && cache.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)ActiveMessengerIds.Disallow,
                        Payload = bytes
                    });

                }
            }
            return true;
        }
        public async Task<bool> Kill(ApiControllerParamsInfo param)
        {
            ActiveKillInfo activeKillInfo = param.Content.DeJson<ActiveKillInfo>();
            byte[] bytes = activeKillInfo.Pid.ToBytes();
            if (signCaching.TryGet(activeKillInfo.UserName, out SignCacheInfo cache) && cache.Connected)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)ActiveMessengerIds.Kill,
                    Payload = bytes
                });
                return true;
            }

            return false;
        }

        public string Update(ApiControllerParamsInfo param)
        {
            WindowUserInfo model = param.Content.DeJson<WindowUserInfo>();
            activeWindowDB.Add(model);
            return string.Empty;
        }
    }



    public sealed class DisallowInfo
    {
        public string[] Devices { get; set; }
        public string[] Data { get; set; }
        public string[] Ids1 { get; set; }
        public string[] Ids2 { get; set; }
    }

    public sealed class ActiveKillInfo
    {
        public string UserName { get; set; }
        public int Pid { get; set; }
    }
}
