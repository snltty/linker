using cmonitor.api;
using cmonitor.client.ruleConfig;
using cmonitor.plugins.active.messenger;
using cmonitor.plugins.active.report;
using cmonitor.plugins.signIn.messenger;
using cmonitor.server;
using common.libs;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.plugins.active
{
    public sealed class ActiveApiController : IApiController
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        private readonly RuleConfig ruleConfig;
        public ActiveApiController(MessengerSender messengerSender, SignCaching signCaching, RuleConfig ruleConfig)
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
            byte[] bytes = MemoryPackSerializer.Serialize(new ActiveDisallowInfo { FileNames = disallowInfo.FileNames, Ids = disallowInfo.Ids });
            foreach (string name in disallowInfo.UserNames)
            {
                if (signCaching.Get(name, out SignCacheInfo cache) && cache.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)ActiveMessengerIds.Disallow,
                        Payload = bytes
                    });
                    return true;
                }
            }

            return false;
        }
        public async Task<bool> Kill(ApiControllerParamsInfo param)
        {
            ActiveKillInfo activeKillInfo = param.Content.DeJson<ActiveKillInfo>();
            byte[] bytes = activeKillInfo.pid.ToBytes();
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

        public string AddGroup(ApiControllerParamsInfo param)
        {
            return ruleConfig.AddWindowGroup(param.Content.DeJson<UpdateWindowGroupInfo>());
        }
        public string DeleteGroup(ApiControllerParamsInfo param)
        {
            return ruleConfig.DeleteWindowGroup(param.Content.DeJson<DeleteWindowGroupInfo>());
        }
        public string Add(ApiControllerParamsInfo param)
        {
            return ruleConfig.AddWindow(param.Content.DeJson<AddWindowItemInfo>());
        }
        public string Del(ApiControllerParamsInfo param)
        {
            return ruleConfig.DelWindow(param.Content.DeJson<DeletedWindowItemInfo>());
        }
    }

    public sealed class DisallowInfo
    {
        public string[] UserNames { get; set; }
        public string[] FileNames { get; set; }
        public uint[] Ids { get; set; }
    }

    public sealed class ActiveKillInfo
    {
        public string UserName { get; set; }
        public int pid { get; set; }
    }
}
