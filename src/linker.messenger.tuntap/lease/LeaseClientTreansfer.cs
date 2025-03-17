using System.Net;
using linker.libs;
using linker.libs.timer;
using linker.messenger.signin;
using linker.messenger.tuntap.messenger;

namespace linker.messenger.tuntap.lease
{
    public sealed class LeaseClientTreansfer
    {
        private readonly IMessengerSender messengerSender;
        private readonly SignInClientState signInClientState;
        private readonly ISerializer serializer;
        public LeaseClientTreansfer(IMessengerSender messengerSender, SignInClientState signInClientState, ISerializer serializer)
        {
            this.messengerSender = messengerSender;
            this.signInClientState = signInClientState;
            this.serializer = serializer;
            LeaseExpTask();
        }

        public async Task AddNetwork(LeaseInfo info)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)TuntapMessengerIds.LeaseAddNetwork,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
        }
        public async Task<LeaseInfo> GetNetwork()
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)TuntapMessengerIds.LeaseGetNetwork

            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                LeaseInfo info = serializer.Deserialize<LeaseInfo>(resp.Data.Span);
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
            }).ConfigureAwait(false);
        }

        public async Task<LeaseInfo> LeaseIp(IPAddress ip, byte prefixLength)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)TuntapMessengerIds.LeaseIP,
                Payload = serializer.Serialize(new LeaseInfo { IP = ip, PrefixLength = prefixLength })

            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                LeaseInfo newip = serializer.Deserialize<LeaseInfo>(resp.Data.Span);
                if (newip.Equals(IPAddress.Any) == false)
                {
                    return newip;
                }
            }
            return new LeaseInfo { IP = ip, PrefixLength = prefixLength };
        }

        private void LeaseExpTask()
        {
            TimerHelper.SetIntervalLong(async () =>
            {
                await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.LeaseExp,
                }).ConfigureAwait(false);

                return true;
            },  60000);
        }
    }
}
