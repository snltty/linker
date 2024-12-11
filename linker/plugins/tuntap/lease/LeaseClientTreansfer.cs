using System.Net;
using MemoryPack;
using linker.plugins.client;
using linker.plugins.messenger;
using linker.plugins.tuntap.messenger;
using linker.libs;

namespace linker.plugins.tuntap.lease
{
    public sealed class LeaseClientTreansfer
    {
        private readonly IMessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;

        public LeaseClientTreansfer(IMessengerSender messengerSender, ClientSignInState clientSignInState)
        {
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;

            LeaseExpTask();
        }

        public async Task AddNetwork(LeaseInfo info)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)TuntapMessengerIds.LeaseAddNetwork,
                Payload = MemoryPackSerializer.Serialize(info)
            });
        }
        public async Task<LeaseInfo> GetNetwork()
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)TuntapMessengerIds.LeaseGetNetwork

            });
            if (resp.Code == MessageResponeCodes.OK)
            {
                LeaseInfo info = MemoryPackSerializer.Deserialize<LeaseInfo>(resp.Data.Span);
                return info;
            }
            return new LeaseInfo { IP = IPAddress.Any, PrefixLength = 24 };
        }
        public async Task LeaseChange()
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)TuntapMessengerIds.LeaseChangeForward
            });
        }

        public async Task<LeaseInfo> LeaseIp(IPAddress ip, byte prefixLength)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)TuntapMessengerIds.LeaseIP,
                Payload = MemoryPackSerializer.Serialize(new LeaseInfo { IP = ip, PrefixLength = prefixLength })

            });
            if (resp.Code == MessageResponeCodes.OK)
            {
                LeaseInfo newip = MemoryPackSerializer.Deserialize<LeaseInfo>(resp.Data.Span);
                if (newip.Equals(IPAddress.Any) == false)
                {
                    return newip;
                }
            }
            return new LeaseInfo { IP = ip, PrefixLength = prefixLength };
        }

        private void LeaseExpTask()
        {
            TimerHelper.SetInterval(async () =>
            {
                await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.LeaseExp,
                });

                return true;
            }, () => 60000);
        }
    }
}
