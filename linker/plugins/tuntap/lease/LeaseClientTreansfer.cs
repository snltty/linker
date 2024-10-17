using linker.store;
using LiteDB;
using System.Net;
using linker.libs;
using MemoryPack;
using System.Collections.Concurrent;
using linker.plugins.client;
using linker.plugins.messenger;
using linker.plugins.tuntap.messenger;
using linker.client.config;

namespace linker.plugins.tuntap.lease
{
    public sealed class LeaseClientTreansfer
    {
        private readonly MessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;
        private readonly RunningConfig runningConfig;

        public LeaseClientTreansfer(MessengerSender messengerSender, ClientSignInState clientSignInState, RunningConfig runningConfig)
        {
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;
            this.runningConfig = runningConfig;
        }
        public async Task<IPAddress> LeaseIp()
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)TuntapMessengerIds.Lease,
                Payload = MemoryPackSerializer.Serialize(runningConfig.Data.Tuntap.IP)

            });
            if(resp.Code == MessageResponeCodes.OK)
            {
                IPAddress ip = MemoryPackSerializer.Deserialize<IPAddress>(resp.Data.Span);
                if (ip.Equals(IPAddress.Any) == false)
                {
                    return IPAddress.Any;
                }
            }
            return runningConfig.Data.Tuntap.IP;
        }
    }
}
