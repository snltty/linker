using link.libs.api;
using link.libs.extends;
using link.client.capi;
using link.client.config;
using link.server;
using link.client;
using link.plugins.sforward.messenger;
using MemoryPack;

namespace link.plugins.sforward
{
    public sealed class SForwardClientApiController : IApiClientController
    {
        private readonly SForwardTransfer forwardTransfer;
        private readonly RunningConfig runningConfig;
        private readonly MessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;

        public SForwardClientApiController(SForwardTransfer forwardTransfer, RunningConfig runningConfig, MessengerSender messengerSender, ClientSignInState clientSignInState)
        {
            this.forwardTransfer = forwardTransfer;
            this.runningConfig = runningConfig;
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;
        }


        public string GetSecretKey(ApiControllerParamsInfo param)
        {
            return runningConfig.Data.SForwardSecretKey;
        }
        public async Task SetSecretKey(ApiControllerParamsInfo param)
        {
            SecretKeySetInfo info = param.Content.DeJson<SecretKeySetInfo>();
            if(info.SForwardSecretKey != runningConfig.Data.SForwardSecretKey)
            {
                runningConfig.Data.SForwardSecretKey = info.SForwardSecretKey;
                runningConfig.Data.Update();

                if (info.Sync)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = clientSignInState.Connection,
                        MessengerId = (ushort)SForwardMessengerIds.SecretKeyForward,
                        Payload = MemoryPackSerializer.Serialize(info.SForwardSecretKey)
                    });
                }
            }
        }

        public List<SForwardInfo> Get(ApiControllerParamsInfo param)
        {
            return forwardTransfer.Get();
        }

        public bool Add(ApiControllerParamsInfo param)
        {
            SForwardInfo info = param.Content.DeJson<SForwardInfo>();
            return forwardTransfer.Add(info);
        }

        public bool Remove(ApiControllerParamsInfo param)
        {
            if (uint.TryParse(param.Content, out uint id))
            {
                return forwardTransfer.Remove(id);
            }
            return false;
        }

        public sealed class SecretKeySetInfo
        {
            public bool Sync { get; set; }
            public string SForwardSecretKey { get; set; }
        }
    }
}
