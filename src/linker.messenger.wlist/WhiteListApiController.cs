using linker.libs;
using linker.libs.extends;
using linker.libs.web;
using linker.messenger.signin;

namespace linker.messenger.wlist
{
    /// <summary>
    /// 中继管理接口
    /// </summary>
    public sealed class WhiteListApiController : IApiController
    {
        private readonly SignInClientState signInClientState;
        private readonly IMessengerSender messengerSender;
        private readonly ISerializer serializer;

        public WhiteListApiController(SignInClientState signInClientState, IMessengerSender messengerSender, ISerializer serializer)
        {
            this.signInClientState = signInClientState;
            this.messengerSender = messengerSender;
            this.serializer = serializer;
        }
        /// <summary>
        /// 添加修改
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Add(ApiControllerParamsInfo param)
        {
            WhiteListInfo info = param.Content.DeJson<WhiteListInfo>();
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)WhiteListMessengerIds.Add,
                Payload = serializer.Serialize(new WhiteListAddInfo
                {
                    Data = info
                })
            }).ConfigureAwait(false);

            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Del(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)WhiteListMessengerIds.Del,
                Payload = serializer.Serialize(new WhiteListDelInfo
                {
                    Id = int.Parse(param.Content)
                })
            }).ConfigureAwait(false);

            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
        /// <summary>
        /// 用分页查询
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<WhiteListPageResultInfo> Page(ApiControllerParamsInfo param)
        {
            WhiteListPageRequestInfo info = param.Content.DeJson<WhiteListPageRequestInfo>();
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)WhiteListMessengerIds.Page,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<WhiteListPageResultInfo>(resp.Data.Span);
            }

            return new WhiteListPageResultInfo();
        }

        /// <summary>
        /// 状态
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<WhiteListOrderStatusInfo> Status(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)WhiteListMessengerIds.Status,
                Payload = serializer.Serialize(param.Content)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.FalseArray) == false)
            {
                return serializer.Deserialize<WhiteListOrderStatusInfo>(resp.Data.Span);
            }

            return new WhiteListOrderStatusInfo();
        }
        /// <summary>
        /// 添加一个订单
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<string> AddOrder(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)WhiteListMessengerIds.AddOrder,
                Payload = serializer.Serialize(param.Content.DeJson<KeyValuePair<string, string>>())
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<string>(resp.Data.Span);
            }

            return string.Empty;
        }

        public async Task<Dictionary<string, double>> List(ApiControllerParamsInfo param)
        {
            KeyValueInfo info = param.Content.DeJson<KeyValueInfo>();

            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)WhiteListMessengerIds.List,
                Payload = serializer.Serialize(new KeyValuePair<string, List<string>>(info.Key, info.Value))
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<Dictionary<string, double>>(resp.Data.Span);
            }

            return new Dictionary<string, double>();
        }
        sealed class KeyValueInfo
        {
            public string Key { get; set; } = string.Empty;
            public List<string> Value { get; set; } = [];
        }
    }

}
