using linker.libs.api;
using MemoryPack;
using linker.libs.extends;
using System.Collections.Concurrent;
using linker.config;
using linker.tunnel.connection;
using linker.plugins.client;
using linker.plugins.capi;
using linker.plugins.messenger;
using linker.client.config;
using linker.plugins.socks5.config;
using linker.plugins.socks5.messenger;

namespace linker.plugins.socks5
{
    public sealed class Socks5ClientApiController : IApiClientController
    {
        private readonly IMessengerSender messengerSender;
        private readonly Socks5ConfigTransfer socks5ConfigTransfer;
        private readonly ClientSignInState clientSignInState;
        private readonly FileConfig config;
        private readonly TunnelProxy tunnelProxy;
        private readonly RunningConfig runningConfig;


        public Socks5ClientApiController(IMessengerSender messengerSender, Socks5ConfigTransfer socks5ConfigTransfer, ClientSignInState clientSignInState, FileConfig config, TunnelProxy tunnelProxy, RunningConfig runningConfig, Socks5ConfigTransfer Socks5ConfigTransfer)
        {
            this.messengerSender = messengerSender;
            this.socks5ConfigTransfer = socks5ConfigTransfer;
            this.clientSignInState = clientSignInState;
            this.config = config;
            this.tunnelProxy = tunnelProxy;
            this.runningConfig = runningConfig;
        }

        public ConnectionListInfo Connections(ApiControllerParamsInfo param)
        {
            ulong hashCode = ulong.Parse(param.Content);
            if (tunnelProxy.Version.Eq(hashCode, out ulong version) == false)
            {
                return new ConnectionListInfo
                {
                    List = tunnelProxy.GetConnections(),
                    HashCode = version
                };
            }
            return new ConnectionListInfo { HashCode = version };
        }

        [ClientApiAccess(ClientApiAccess.TunnelRemove)]
        public void RemoveConnection(ApiControllerParamsInfo param)
        {
            tunnelProxy.RemoveConnection(param.Content);
        }

        /// <summary>
        /// 获取所有客户端的信息
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public Socks5ListInfo Get(ApiControllerParamsInfo param)
        {
            ulong hashCode = ulong.Parse(param.Content);
            if (socks5ConfigTransfer.Version.Eq(hashCode, out ulong version) == false)
            {
                return new Socks5ListInfo
                {
                    List = socks5ConfigTransfer.Infos,
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
            socks5ConfigTransfer.RefreshConfig();
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
                if (config.Data.Client.HasAccess(ClientApiAccess.Socks5StatusSelf) == false) return false;

                socks5ConfigTransfer.Retstart();
            }
            else
            {
                if (config.Data.Client.HasAccess(ClientApiAccess.Socks5StatusOther) == false) return false;
                //运行别人的
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)Socks5MessengerIds.RunForward,
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
                if (config.Data.Client.HasAccess(ClientApiAccess.Socks5StatusSelf) == false) return false;
                socks5ConfigTransfer.Stop();
            }
            else
            {
                if (config.Data.Client.HasAccess(ClientApiAccess.Socks5StatusOther) == false) return false;
                //停止别人的
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)Socks5MessengerIds.StopForward,
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

            Socks5Info info = param.Content.DeJson<Socks5Info>();
            //更新自己的
            if (info.MachineId == config.Data.Client.Id)
            {
                if (config.Data.Client.HasAccess(ClientApiAccess.Socks5ChangeSelf) == false) return false;
                socks5ConfigTransfer.UpdateConfig(info);
            }
            else
            {
                if (config.Data.Client.HasAccess(ClientApiAccess.Socks5ChangeOther) == false) return false;
                //更新别人的
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)Socks5MessengerIds.UpdateForward,
                    Payload = MemoryPackSerializer.Serialize(info)
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
    public sealed class ConnectionListInfo
    {
        public ConcurrentDictionary<string, ITunnelConnection> List { get; set; }
        public ulong HashCode { get; set; }
    }
}
