using linker.libs;
using linker.libs.extends;
using linker.messenger.signin;
using linker.nat;

namespace linker.messenger.firewall
{
    public sealed class FirewallServerMessenger : IMessenger
    {

        private readonly IMessengerSender sender;
        private readonly SignInServerCaching signCaching;
        private readonly ISerializer serializer;
        public FirewallServerMessenger(IMessengerSender sender, SignInServerCaching signCaching, ISerializer serializer)
        {
            this.sender = sender;
            this.signCaching = signCaching;
            this.serializer = serializer;
        }

        /// <summary>
        /// 获取对端的记录
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)FirewallMessengerIds.GetForward)]
        public void GetForward(IConnection connection)
        {
            FirewallSearchForwardInfo info = serializer.Deserialize<FirewallSearchForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                sender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)FirewallMessengerIds.Get,
                    Payload = serializer.Serialize(info.Data)
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK)
                    {
                        await sender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Code = MessageResponeCodes.OK,
                            Payload = result.Result.Data,
                            RequestId = requestid
                        }, (ushort)FirewallMessengerIds.GetForward).ConfigureAwait(false);
                    }
                });
            }
            else
            {
                connection.Write(serializer.Serialize(new FirewallListInfo()));
            }
        }
        /// <summary>
        /// 添加对端的记录
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)FirewallMessengerIds.AddForward)]
        public async Task AddForward(IConnection connection)
        {
            FirewallAddForwardInfo info = serializer.Deserialize<FirewallAddForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                await sender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)FirewallMessengerIds.Add,
                    Payload = serializer.Serialize(info.Data)
                }).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// 删除对端的记录
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)FirewallMessengerIds.RemoveForward)]
        public async Task RemoveForward(IConnection connection)
        {
            FirewallRemoveForwardInfo info = serializer.Deserialize<FirewallRemoveForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                await sender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)FirewallMessengerIds.Remove,
                    Payload = serializer.Serialize(info.Id)
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 设置对端状态
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)FirewallMessengerIds.StateForward)]
        public async Task StateForward(IConnection connection)
        {
            FirewallStateForwardInfo info = serializer.Deserialize<FirewallStateForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                await sender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)FirewallMessengerIds.State,
                    Payload = serializer.Serialize(info.State)
                }).ConfigureAwait(false);
            }
        }
    }

    public sealed class FirewallClientMessenger : IMessenger
    {
        private readonly FirewallTransfer firewallTransfer;
        private readonly IMessengerSender sender;
        private readonly ISerializer serializer;
        public FirewallClientMessenger(FirewallTransfer firewallTransfer, IMessengerSender sender, ISerializer serializer)
        {
            this.firewallTransfer = firewallTransfer;
            this.sender = sender;
            this.serializer = serializer;
        }

        [MessengerId((ushort)FirewallMessengerIds.Get)]
        public void Get(IConnection connection)
        {
            FirewallSearchInfo info = serializer.Deserialize<FirewallSearchInfo>(connection.ReceiveRequestWrap.Payload.Span);
            connection.Write(serializer.Serialize(firewallTransfer.Get(info)));
        }
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)FirewallMessengerIds.Add)]
        public void Add(IConnection connection)
        {
            FirewallRuleInfo info = serializer.Deserialize<FirewallRuleInfo>(connection.ReceiveRequestWrap.Payload.Span);
            firewallTransfer.Add(info);
        }
        // <summary>
        /// 删除
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)FirewallMessengerIds.Remove)]
        public void Remove(IConnection connection)
        {
            string id = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            firewallTransfer.Remove(id);
        }


        [MessengerId((ushort)FirewallMessengerIds.State)]
        public void State(IConnection connection)
        {
            LinkerFirewallState state = serializer.Deserialize<LinkerFirewallState>(connection.ReceiveRequestWrap.Payload.Span);
            firewallTransfer.State(state);
        }
    }


}
