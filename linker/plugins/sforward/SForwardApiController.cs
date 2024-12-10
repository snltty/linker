using linker.libs.api;
using linker.libs.extends;
using linker.client.config;
using MemoryPack;
using linker.plugins.sforward.messenger;
using linker.plugins.client;
using linker.plugins.capi;
using linker.plugins.messenger;
using linker.config;
using System.Collections.Concurrent;
using linker.plugins.access;

namespace linker.plugins.sforward
{
    public sealed class SForwardClientApiController : IApiClientController
    {
        private readonly SForwardTransfer forwardTransfer;
        private readonly IMessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;
        private readonly FileConfig config;
        private readonly AccessTransfer accessTransfer;
        private readonly ClientConfigTransfer clientConfigTransfer;

        public SForwardClientApiController(SForwardTransfer forwardTransfer, IMessengerSender messengerSender, ClientSignInState clientSignInState, FileConfig config, AccessTransfer accessTransfer, ClientConfigTransfer clientConfigTransfer)
        {
            this.forwardTransfer = forwardTransfer;
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;
            this.config = config;
            this.accessTransfer = accessTransfer;
            this.clientConfigTransfer = clientConfigTransfer;
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


        public void Refresh(ApiControllerParamsInfo param)
        {
            forwardTransfer.RefreshConfig();
        }
        /// <summary>
        /// 获取数量
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public SForwardListInfo GetCount(ApiControllerParamsInfo param)
        {
            ulong hashCode = ulong.Parse(param.Content);
            if (forwardTransfer.Version.Eq(hashCode, out ulong version) == false)
            {
                return new SForwardListInfo
                {
                    List = forwardTransfer.GetCount(),
                    HashCode = version
                };
            }
            return new SForwardListInfo { HashCode = version };
        }

        /// <summary>
        /// 获取穿透列表
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<List<SForwardInfo>> Get(ApiControllerParamsInfo param)
        {
            if (param.Content == clientConfigTransfer.Id)
            {
                if (accessTransfer.HasAccess(ClientApiAccess.ForwardShowSelf) == false) return new List<SForwardInfo>();
                return forwardTransfer.Get();
            }
            if (accessTransfer.HasAccess(ClientApiAccess.ForwardShowOther) == false) return new List<SForwardInfo>();

            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.GetForward,
                Payload = MemoryPackSerializer.Serialize(param.Content)
            });
            if (resp.Code == MessageResponeCodes.OK)
            {
                return MemoryPackSerializer.Deserialize<List<SForwardInfo>>(resp.Data.Span);
            }
            return new List<SForwardInfo>();
        }

        /// <summary>
        /// 添加穿透
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Add(ApiControllerParamsInfo param)
        {
            SForwardAddForwardInfo info = param.Content.DeJson<SForwardAddForwardInfo>();
            if (info.MachineId == clientConfigTransfer.Id)
            {
                if (accessTransfer.HasAccess(ClientApiAccess.ForwardSelf) == false) return false;
                return forwardTransfer.Add(info.Data);
            }
            if (accessTransfer.HasAccess(ClientApiAccess.ForwardOther) == false) return false;

            return await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.AddClientForward,
                Payload = MemoryPackSerializer.Serialize(info)
            });
        }

        /// <summary>
        /// 删除穿透
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Remove(ApiControllerParamsInfo param)
        {
            SForwardRemoveForwardInfo info = param.Content.DeJson<SForwardRemoveForwardInfo>();
            if (info.MachineId == clientConfigTransfer.Id)
            {
                if (accessTransfer.HasAccess(ClientApiAccess.ForwardSelf) == false) return false;
                return forwardTransfer.Remove(info.Id);
            }

            if (accessTransfer.HasAccess(ClientApiAccess.ForwardOther) == false) return false;
            return await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.RemoveClientForward,
                Payload = MemoryPackSerializer.Serialize(info)
            });
        }

        /// <summary>
        /// 测试服务
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> TestLocal(ApiControllerParamsInfo param)
        {
            if (param.Content == clientConfigTransfer.Id)
            {
                forwardTransfer.TestLocal();
                return true;
            }
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.TestClientForward,
                Payload = MemoryPackSerializer.Serialize(param.Content)
            });
            return true;
        }

    }

    public sealed class SForwardListInfo
    {
        public ConcurrentDictionary<string,int> List { get; set; }
        public ulong HashCode { get; set; }
    }
}
