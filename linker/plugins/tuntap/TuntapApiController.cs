using linker.libs.api;
using linker.plugins.tuntap.messenger;
using MemoryPack;
using linker.libs.extends;
using System.Collections.Concurrent;
using linker.config;
using linker.tunnel.connection;
using linker.plugins.tuntap.proxy;
using linker.plugins.client;
using linker.plugins.capi;
using linker.plugins.messenger;
using linker.plugins.tuntap.config;

namespace linker.plugins.tuntap
{
    public sealed class TuntapClientApiController : IApiClientController
    {
        private readonly MessengerSender messengerSender;
        private readonly TuntapTransfer tuntapTransfer;
        private readonly ClientSignInState clientSignInState;
        private readonly FileConfig config;
        private readonly TuntapProxy tuntapProxy;

        public TuntapClientApiController(MessengerSender messengerSender, TuntapTransfer tuntapTransfer, ClientSignInState clientSignInState, FileConfig config, TuntapProxy tuntapProxy)
        {
            this.messengerSender = messengerSender;
            this.tuntapTransfer = tuntapTransfer;
            this.clientSignInState = clientSignInState;
            this.config = config;
            this.tuntapProxy = tuntapProxy;
        }

        public ConcurrentDictionary<string, ITunnelConnection> Connections(ApiControllerParamsInfo param)
        {
            return tuntapProxy.GetConnections();
        }
        public void RemoveConnection(ApiControllerParamsInfo param)
        {
            tuntapProxy.RemoveConnection(param.Content);
        }

        /// <summary>
        /// 获取所有客户端的网卡信息
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public TuntabListInfo Get(ApiControllerParamsInfo param)
        {
            uint hashCode = uint.Parse(param.Content);
            uint _hashCode = tuntapTransfer.InfosVersion;
            if (_hashCode != hashCode)
            {
                return new TuntabListInfo
                {
                    List = tuntapTransfer.Infos,
                    HashCode = _hashCode
                };
            }
            return new TuntabListInfo { HashCode = _hashCode };
        }
        /// <summary>
        /// 刷新网卡信息
        /// </summary>
        /// <param name="param"></param>
        public void Refresh(ApiControllerParamsInfo param)
        {
            tuntapTransfer.RefreshConfig();
        }

        /// <summary>
        /// 运行网卡
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Run(ApiControllerParamsInfo param)
        {
            //运行自己的
            if (param.Content == config.Data.Client.Id)
            {
                tuntapTransfer.Shutdown();
                tuntapTransfer.Setup();
            }
            else
            {
                //运行别人的
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.RunForward,
                    Payload = MemoryPackSerializer.Serialize(param.Content)
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
            if (param.Content == config.Data.Client.Id)
            {
                tuntapTransfer.Shutdown();
            }
            else
            {
                //停止别人的
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.StopForward,
                    Payload = MemoryPackSerializer.Serialize(param.Content)
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
            TuntapInfo info = param.Content.DeJson<TuntapInfo>();
            //更新自己的
            if (info.MachineId == config.Data.Client.Id)
            {
                tuntapTransfer.UpdateConfig(info);
            }
            else
            {
                //更新别人的
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.UpdateForward,
                    Payload = MemoryPackSerializer.Serialize(info)
                }).ConfigureAwait(false);
            }
            return true;
        }


        public sealed class TuntabListInfo
        {
            public ConcurrentDictionary<string, TuntapInfo> List { get; set; }
            public uint HashCode { get; set; }
        }
    }
}
