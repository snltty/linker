using cmonitor.server;
using common.libs.api;
using cmonitor.plugins.tuntap.vea;
using cmonitor.client;
using cmonitor.client.api;
using cmonitor.plugins.tuntap.messenger;
using MemoryPack;
using cmonitor.config;
using common.libs.extends;

namespace cmonitor.plugins.tuntap
{
    public sealed class TuntapClientApiController : IApiClientController
    {
        private readonly MessengerSender messengerSender;
        private readonly TuntapTransfer tuntapTransfer;
        private readonly ClientSignInState clientSignInState;
        private readonly Config config;

        public TuntapClientApiController(MessengerSender messengerSender, TuntapTransfer tuntapTransfer, ClientSignInState clientSignInState, Config config)
        {
            this.messengerSender = messengerSender;
            this.tuntapTransfer = tuntapTransfer;
            this.clientSignInState = clientSignInState;
            this.config = config;
        }
        public TuntabListInfo Get(ApiControllerParamsInfo param)
        {
            int hashCode = int.Parse(param.Content);
            int _hashCode = tuntapTransfer.Infos.GetHashCode();
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
        public async Task<bool> Run(ApiControllerParamsInfo param)
        {
            if (param.Content == config.Data.Client.Name)
            {
                _ = tuntapTransfer.Run();
            }
            else
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.RunForward,
                    Payload = MemoryPackSerializer.Serialize(param.Content)
                });
            }
            return true;
        }
        public async Task<bool> Stop(ApiControllerParamsInfo param)
        {
            if (param.Content == config.Data.Client.Name)
            {
                _ = tuntapTransfer.Stop();
            }
            else
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.Stop,
                    Payload = MemoryPackSerializer.Serialize(param.Content)
                });
            }
            return true;
        }

        public async Task<bool> Update(ApiControllerParamsInfo param)
        {
            TuntapInfo info = param.Content.DeJson<TuntapInfo>();
            if (info.MachineName == config.Data.Client.Name)
            {
                tuntapTransfer.Update(info);
            }
            else
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.UpdateForward,
                    Payload = MemoryPackSerializer.Serialize(info)
                });
                tuntapTransfer.OnChange();
            }
            return true;
        }


        public sealed class TuntabListInfo
        {
            public List<TuntapInfo> List { get; set; }
            public int HashCode { get; set; }
        }
    }
}
