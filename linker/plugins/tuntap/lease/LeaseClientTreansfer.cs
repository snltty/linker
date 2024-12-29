using System.Net;
using linker.serializer;
using linker.plugins.client;
using linker.plugins.tuntap.messenger;
using linker.libs;
using linker.messenger;
using linker.messenger.signin;

namespace linker.plugins.tuntap.lease
{
    public sealed class LeaseClientTreansfer
    {
        private readonly IMessengerSender messengerSender;
        private readonly SignInClientState signInClientState;

        public LeaseClientTreansfer(IMessengerSender messengerSender, SignInClientState signInClientState)
        {
            this.messengerSender = messengerSender;
            this.signInClientState = signInClientState;

            LeaseExpTask();
        }

        public async Task AddNetwork(LeaseInfo info)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)TuntapMessengerIds.LeaseAddNetwork,
                Payload = Serializer.Serialize(info)
            });
        }
        public async Task<LeaseInfo> GetNetwork()
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)TuntapMessengerIds.LeaseGetNetwork

            });
            if (resp.Code == MessageResponeCodes.OK)
            {
                LeaseInfo info = Serializer.Deserialize<LeaseInfo>(resp.Data.Span);
                return info;
            }
            return new LeaseInfo { IP = IPAddress.Any, PrefixLength = 24 };
        }
        public async Task LeaseChange()
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)TuntapMessengerIds.LeaseChangeForward
            });
        }

        public async Task<LeaseInfo> LeaseIp(IPAddress ip, byte prefixLength)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)TuntapMessengerIds.LeaseIP,
                Payload = Serializer.Serialize(new LeaseInfo { IP = ip, PrefixLength = prefixLength })

            });
            if (resp.Code == MessageResponeCodes.OK)
            {
                LeaseInfo newip = Serializer.Deserialize<LeaseInfo>(resp.Data.Span);
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
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.LeaseExp,
                });

                return true;
            }, () => 60000);
        }
    }
}
