using linker.libs;
using linker.libs.extends;
using linker.libs.web;
using linker.messenger.api;
using linker.messenger.signin;

namespace linker.messenger.cdkey
{
    /// <summary>
    /// 中继管理接口
    /// </summary>
    public sealed class CdkeyApiController : IApiController
    {
        private readonly SignInClientState signInClientState;
        private readonly IMessengerSender messengerSender;
        private readonly ISerializer serializer;
        private readonly ISignInClientStore signInClientStore;


        public CdkeyApiController(SignInClientState signInClientState, IMessengerSender messengerSender, ISerializer serializer, ISignInClientStore signInClientStore)
        {
            this.signInClientState = signInClientState;
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            this.signInClientStore = signInClientStore;
        }

        [Access(AccessValue.Cdkey)]
        public async Task<bool> AddCdkey(ApiControllerParamsInfo param)
        {
            CdkeyStoreInfo info = param.Content.DeJson<CdkeyStoreInfo>();
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)CdkeyMessengerIds.AddCdkey,
                Payload = serializer.Serialize(new CdkeyAddInfo
                {
                    Data = info,
                })
            }).ConfigureAwait(false);

            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        [Access(AccessValue.Cdkey)]
        public async Task<bool> DelCdkey(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)CdkeyMessengerIds.DelCdkey,
                Payload = serializer.Serialize(new CdkeyDelInfo
                {
                    Id = int.Parse(param.Content),
                    UserId = signInClientStore.Server.UserId
                })
            }).ConfigureAwait(false);

            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        [Access(AccessValue.Cdkey)]
        public async Task<CdkeyPageResultInfo> PageCdkey(ApiControllerParamsInfo param)
        {
            CdkeyPageRequestInfo info = param.Content.DeJson<CdkeyPageRequestInfo>();
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)CdkeyMessengerIds.PageCdkey,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<CdkeyPageResultInfo>(resp.Data.Span);
            }

            return new CdkeyPageResultInfo();
        }
        public async Task<CdkeyPageResultInfo> MyCdkey(ApiControllerParamsInfo param)
        {
            CdkeyPageRequestInfo info = param.Content.DeJson<CdkeyPageRequestInfo>();
            info.UserId = signInClientStore.Server.UserId;
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)CdkeyMessengerIds.PageCdkey,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<CdkeyPageResultInfo>(resp.Data.Span);
            }

            return new CdkeyPageResultInfo();
        }
        public async Task<CdkeyTestResultInfo> TestCdkey(ApiControllerParamsInfo param)
        {
            CdkeyImportInfo info = param.Content.DeJson<CdkeyImportInfo>();
            info.UserId = signInClientStore.Server.UserId;
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)CdkeyMessengerIds.TestCdkey,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<CdkeyTestResultInfo>(resp.Data.Span);
            }

            return new CdkeyTestResultInfo();
        }

        public async Task<string> ImportCdkey(ApiControllerParamsInfo param)
        {
            CdkeyImportInfo info = param.Content.DeJson<CdkeyImportInfo>();
            info.UserId = signInClientStore.Server.UserId;
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)CdkeyMessengerIds.ImportCdkey,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<string>(resp.Data.Span);
            }
            return "Network";
        }
    }
}
