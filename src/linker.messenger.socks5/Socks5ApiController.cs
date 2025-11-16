using linker.libs.extends;
using System.Collections.Concurrent;
using linker.tunnel.connection;
using linker.messenger.signin;
using linker.libs;
using linker.messenger.api;
using linker.libs.web;

namespace linker.messenger.socks5
{
    public sealed class Socks5ApiController : IApiController
    {
        private readonly IMessengerSender messengerSender;
        private readonly Socks5Transfer socks5Transfer;
        private readonly SignInClientState signInClientState;
        private readonly Socks5Proxy tunnelProxy;
        private readonly ISignInClientStore signInClientStore;
        private readonly Socks5Decenter socks5Decenter;
        private readonly ISerializer serializer;
        private readonly IAccessStore accessStore;
        public Socks5ApiController(IMessengerSender messengerSender,  SignInClientState signInClientState, Socks5Proxy tunnelProxy, Socks5Transfer socks5Transfer, ISignInClientStore signInClientStore, Socks5Decenter socks5Decenter, ISerializer serializer, IAccessStore accessStore)
        {
            this.messengerSender = messengerSender;
            this.socks5Transfer = socks5Transfer;
            this.signInClientState = signInClientState;
            this.tunnelProxy = tunnelProxy;
            this.signInClientStore = signInClientStore;
            this.socks5Decenter = socks5Decenter;
            this.serializer = serializer;
            this.accessStore = accessStore;
        }

        /// <summary>
        /// 获取所有客户端的信息
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public Socks5ListInfo Get(ApiControllerParamsInfo param)
        {
            ulong hashCode = ulong.Parse(param.Content);
            if (socks5Decenter.DataVersion.Eq(hashCode, out ulong version) == false)
            {
                return new Socks5ListInfo
                {
                    List = socks5Decenter.Infos,
                    HashCode = version
                };
            }
            return new Socks5ListInfo { HashCode = version };
        }
        /// <summary>
        /// 刷新信息
        /// </summary>
        /// <param name="param"></param>
        public void Refresh(ApiControllerParamsInfo param)
        {
            socks5Decenter.Refresh();
        }

        /// <summary>
        /// 运行网卡
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Run(ApiControllerParamsInfo param)
        {
            //运行自己的
            if (param.Content == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.Socks5StatusSelf) == false) return false;
                socks5Transfer.Retstart();
            }
            else
            {
                if (accessStore.HasAccess(AccessValue.Socks5StatusOther) == false) return false;
                //运行别人的
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)Socks5MessengerIds.RunForward,
                    Payload = serializer.Serialize(param.Content)
                }).ConfigureAwait(false);
            }
            return true;
        }
        /// <summary>
        /// 停止网卡
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Stop(ApiControllerParamsInfo param)
        {
            //停止自己的
            if (param.Content == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.Socks5StatusSelf) == false) return false;
                socks5Transfer.Stop();
            }
            else
            {
                if (accessStore.HasAccess(AccessValue.Socks5StatusOther) == false) return false;
                //停止别人的
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)Socks5MessengerIds.StopForward,
                    Payload = serializer.Serialize(param.Content)
                }).ConfigureAwait(false);
            }
            return true;
        }

        /// <summary>
        /// 更新网卡信息
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Update(ApiControllerParamsInfo param)
        {

            Socks5Info info = param.Content.DeJson<Socks5Info>();
            //更新自己的
            if (info.MachineId == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.Socks5ChangeSelf) == false) return false;
                socks5Transfer.UpdateConfig(info);
            }
            else
            {
                if (accessStore.HasAccess(AccessValue.Socks5ChangeOther) == false) return false;
                //更新别人的
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)Socks5MessengerIds.UpdateForward,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
            }
            return true;
        }
    }
    public sealed class Socks5ListInfo
    {
        public ConcurrentDictionary<string, Socks5Info> List { get; set; }
        public ulong HashCode { get; set; }
    }
}
