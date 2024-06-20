using cmonitor.plugins.signin.messenger;
using cmonitor.plugins.snatch.messenger;
using cmonitor.plugins.snatch.report;
using cmonitor.server;
using common.libs.extends;
using common.libs.api;
using cmonitor.server.sapi;
using cmonitor.plugins.snatch.db;

namespace cmonitor.plugins.snatch
{
    public sealed class SnatchApiController : IApiServerController
    {
        private readonly MessengerSender messengerSender;
        private readonly ISnatchDB snatchDB;
        private readonly SignCaching signCaching;
        private readonly ISnatachCaching snatachCaching;

        public SnatchApiController(ISnatchDB snatchDB, MessengerSender messengerSender, SignCaching signCaching, ISnatachCaching snatachCaching)
        {
            this.snatchDB = snatchDB;
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.snatachCaching = snatachCaching;
        }

        public string Update(ApiControllerParamsInfo param)
        {
            SnatchUserInfo model = param.Content.DeJson<SnatchUserInfo>();
            snatchDB.Add(model);
            return string.Empty;
        }

        public AnswerGroupInfo[] GetQuestion(ApiControllerParamsInfo param)
        {
            if (snatachCaching.Get(param.Content, out SnatchQuestionCacheInfo info))
            {
                return info.Answers.GroupBy(c => c.Question).Select(c => new AnswerGroupInfo
                {
                    Question = c.Key,
                    Answers = c.ToArray()
                }).ToArray();
            }

            return Array.Empty<AnswerGroupInfo>();
        }

        public async Task<bool> AddQuestion(ApiControllerParamsInfo param)
        {
            SnatchQuestionCacheParamInfo info = param.Content.DeJson<SnatchQuestionCacheParamInfo>();
            if (snatachCaching.Add(info.Cache, info.Question) && snatachCaching.Get(info.Cache.UserName, out SnatchQuestionCacheInfo cache))
            {
                if (info.Question != null)
                {
                    byte[] bytes = info.Question.ToBytes();
                    for (int i = 0; i < info.Cache.MachineIds.Length; i++)
                    {
                        if (signCaching.TryGet(info.Cache.MachineIds[i], out SignCacheInfo signCache))
                        {
                            await messengerSender.SendOnly(new MessageRequestWrap
                            {
                                Connection = signCache.Connection,
                                MessengerId = (ushort)SnatchMessengerIds.AddQuestion,
                                Payload = bytes
                            });
                        }
                    }
                }
            }
            return true;
        }
        public async Task<bool> UpdateQuestion(ApiControllerParamsInfo param)
        {
            UpdateQuestionCacheParamInfo info = param.Content.DeJson<UpdateQuestionCacheParamInfo>();
            foreach (UpdateQuestionCacheParamItemInfo item in info.Items)
            {
                SnatchAnswerInfo answer = null;
                bool conti = snatachCaching.Update(info.UserName, item.MachineId, item.Question) == false
                    || snatachCaching.Get(info.UserName, item.MachineId, out answer) == false
                    || answer == null || answer.Question == null;
                if (conti)
                {
                    continue;
                }
                byte[] bytes = answer.Question.ToBytes();
                if (signCaching.TryGet(answer.MachineId, out SignCacheInfo signCache))
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = signCache.Connection,
                        MessengerId = (ushort)SnatchMessengerIds.AddQuestion,
                        Payload = bytes
                    });
                }
            }
            return true;
        }
        public async Task<bool> RemoveQuestion(ApiControllerParamsInfo param)
        {
            if (snatachCaching.Remove(param.Content, out SnatchQuestionCacheInfo info))
            {
                for (int i = 0; i < info.MachineIds.Length; i++)
                {
                    if (signCaching.TryGet(info.MachineIds[i], out SignCacheInfo cache))
                    {
                        await messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = cache.Connection,
                            MessengerId = (ushort)SnatchMessengerIds.RemoveQuestion
                        });
                    }
                }
            }
            return true;
        }


        public sealed class UpdateQuestionCacheParamInfo
        {
            public string UserName { get; set; }
            public UpdateQuestionCacheParamItemInfo[] Items { get; set; }
        }
        public sealed class UpdateQuestionCacheParamItemInfo
        {
            public string MachineId { get; set; }
            public SnatchQuestionInfo Question { get; set; }
        }
        public sealed class SnatchQuestionCacheParamInfo
        {
            public SnatchQuestionCacheInfo Cache { get; set; }
            public SnatchQuestionInfo Question { get; set; }
        }
        public sealed class AnswerGroupInfo
        {
            public SnatchQuestionInfo Question { get; set; }
            public SnatchAnswerInfo[] Answers { get; set; }
        }
    }

}
