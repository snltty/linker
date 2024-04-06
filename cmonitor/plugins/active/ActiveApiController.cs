using cmonitor.api;
using cmonitor.plugins.active.messenger;
using cmonitor.plugins.active.report;
using cmonitor.plugins.signIn.messenger;
using cmonitor.server;
using cmonitor.server.ruleConfig;
using common.libs;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.plugins.active
{
    public sealed class ActiveApiController : IApiController
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        private readonly IRuleConfig ruleConfig;
        public ActiveApiController(MessengerSender messengerSender, SignCaching signCaching, IRuleConfig ruleConfig)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.ruleConfig = ruleConfig;
        }
        public async Task<ActiveWindowTimeReportInfo> Get(ApiControllerParamsInfo param)
        {
            if (signCaching.Get(param.Content, out SignCacheInfo cache) && cache.Connected)
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
            if (signCaching.Get(param.Content, out SignCacheInfo cache) && cache.Connected)
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
            if (signCaching.Get(param.Content, out SignCacheInfo cache) && cache.Connected)
            {
                signCaching.Update();
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
                if (signCaching.Get(name, out SignCacheInfo cache) && cache.Connected)
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
            if (signCaching.Get(activeKillInfo.UserName, out SignCacheInfo cache) && cache.Connected)
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
            UpdateWindowGroupInfo model = param.Content.DeJson<UpdateWindowGroupInfo>();
            ruleConfig.Set(model.UserName,"Windows", model.Data);
            return string.Empty;
        }
    }

    public sealed class UpdateWindowGroupInfo
    {
        public string UserName { get; set; }
        public List<WindowGroupInfo> Data { get; set; } = new List<WindowGroupInfo>();
    }
    public sealed class WindowGroupInfo
    {
        public string Name { get; set; }
        public List<WindowItemInfo> List { get; set; } = new List<WindowItemInfo>();
    }
    public sealed class WindowItemInfo
    {
        public string Name { get; set; }
        public string Desc { get; set; }
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
