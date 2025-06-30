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
        private readonly IWhiteListClientStore whiteListClientStore;

        public WhiteListApiController(SignInClientState signInClientState, IMessengerSender messengerSender, ISerializer serializer, IWhiteListClientStore whiteListClientStore)
        {
            this.signInClientState = signInClientState;
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            this.whiteListClientStore = whiteListClientStore;
        }
        public string GetSecretKey(ApiControllerParamsInfo param)
        {
            return whiteListClientStore.SecretKey;
        }
        public void SetSecretKey(ApiControllerParamsInfo param)
        {
            whiteListClientStore.SetSecretKey(param.Content);
        }

        /// 检查密钥
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> CheckKey(ApiControllerParamsInfo param)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)WhiteListMessengerIds.CheckKey,
                Payload = serializer.Serialize(whiteListClientStore.SecretKey)
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
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
                    Data = info,
                    SecretKey = whiteListClientStore.SecretKey
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
                    Id = int.Parse(param.Content),
                    SecretKey = whiteListClientStore.SecretKey
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
            info.SecretKey = whiteListClientStore.SecretKey;
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
    }

}
