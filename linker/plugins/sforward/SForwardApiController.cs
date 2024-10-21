using linker.libs.api;
using linker.libs.extends;
using linker.client.config;
using MemoryPack;
using linker.plugins.sforward.messenger;
using linker.plugins.client;
using linker.plugins.capi;
using linker.plugins.messenger;
using linker.config;

namespace linker.plugins.sforward
{
    public sealed class SForwardClientApiController : IApiClientController
    {
        private readonly SForwardTransfer forwardTransfer;
        private readonly IMessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;

        public SForwardClientApiController(SForwardTransfer forwardTransfer, IMessengerSender messengerSender, ClientSignInState clientSignInState)
        {
            this.forwardTransfer = forwardTransfer;
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;
        }

        /// <summary>
        /// 获取密钥
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public string GetSecretKey(ApiControllerParamsInfo param)
        {
            return forwardTransfer.GetSecretKey();
        }
        /// <summary>
        /// 设置密钥
        /// </summary>
        /// <param name="param"></param>
        [ClientApiAccessAttribute(ClientApiAccess.Config)]
        public void SetSecretKey(ApiControllerParamsInfo param)
        {
            forwardTransfer.SetSecretKey(param.Content);
        }

        /// <summary>
        /// 获取本机的穿透列表
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public SForwardListInfo Get(ApiControllerParamsInfo param)
        {
            ulong hashCode = ulong.Parse(param.Content);
            if (forwardTransfer.Version.Eq(hashCode, out ulong version) == false)
            {
                return new SForwardListInfo
                {
                    List = forwardTransfer.Get(),
                    HashCode = version
                };
            }
            return new SForwardListInfo { HashCode = version };
        }
        /// <summary>
        /// 获取别的客户端穿透列表
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<List<SForwardRemoteInfo>> GetRemote(ApiControllerParamsInfo param)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.GetForward,
                Payload = MemoryPackSerializer.Serialize(param.Content)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return MemoryPackSerializer.Deserialize<List<SForwardRemoteInfo>>(resp.Data.Span);
            }
            return new List<SForwardRemoteInfo>();
        }

        /// <summary>
        /// 添加穿透
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        [ClientApiAccessAttribute(ClientApiAccess.Config)]
        public bool Add(ApiControllerParamsInfo param)
        {
            SForwardInfo info = param.Content.DeJson<SForwardInfo>();
            return forwardTransfer.Add(info);
        }

        /// <summary>
        /// 删除穿透
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        [ClientApiAccessAttribute(ClientApiAccess.Config)]
        public bool Remove(ApiControllerParamsInfo param)
        {
            if (uint.TryParse(param.Content, out uint id))
            {
                return forwardTransfer.Remove(id);
            }
            return false;
        }

        /// <summary>
        /// 测试本机服务
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public bool TestLocal(ApiControllerParamsInfo param)
        {
            forwardTransfer.TestLocal();
            return true;
        }

    }

    public sealed class SForwardListInfo
    {
        public List<SForwardInfo> List { get; set; }
        public ulong HashCode { get; set; }
    }
}
