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
        private readonly ILeaseClientStore leaseClientStore;
        private readonly ISignInClientStore signInClientStore;
        public LeaseClientTreansfer(IMessengerSender messengerSender, SignInClientState signInClientState, ISerializer serializer, ILeaseClientStore leaseClientStore, ISignInClientStore signInClientStore)
        {
            this.messengerSender = messengerSender;
            this.signInClientState = signInClientState;
            this.serializer = serializer;
            this.leaseClientStore = leaseClientStore;
            this.signInClientStore = signInClientStore;
            LeaseExpTask();

        }

        public async Task AddNetwork(LeaseInfo info)
        {
            leaseClientStore.Set(signInClientStore.Group.Id, info);
            leaseClientStore.Confirm();
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
                MessengerId = (ushort)TuntapMessengerIds.LeaseGetNetwork,
            }).ConfigureAwait(false);
            LeaseInfo info = new LeaseInfo { IP = IPAddress.Any, PrefixLength = 24 };
            if (resp.Code == MessageResponeCodes.OK)
            {
                info = serializer.Deserialize<LeaseInfo>(resp.Data.Span);
            }

            if(info.IP.Equals(IPAddress.Any) == false)
            {
                leaseClientStore.Set(signInClientStore.Group.Id, info);
                leaseClientStore.Confirm();
            }
           
            return info;
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
                if (newip.IP.Equals(IPAddress.Any) == false)
                {
                    return newip;
                }
            }
            return new LeaseInfo { IP = ip, PrefixLength = prefixLength };
        }

        private void LeaseExpTask()
        {
            signInClientState.OnSignInSuccess += (times) =>
            {
                TimerHelper.Async(async () =>
                {
                    try
                    {
                        LeaseInfo info = await GetNetwork();
                        if (info.IP.Equals(IPAddress.Any))
                        {
                            info = leaseClientStore.Get(signInClientStore.Group.Id);
                            if (info != null && info.IP.Equals(IPAddress.Any) == false)
                            {
                                await AddNetwork(info);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                });
            };

            TimerHelper.SetIntervalLong(async () =>
            {
                await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.LeaseExp,
                }).ConfigureAwait(false);
            }, 60000);
        }
    }
}
