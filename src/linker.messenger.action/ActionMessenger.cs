using linker.libs;
using linker.messenger.signin;

namespace linker.messenger.action
{
    public class ActionClientMessenger : IMessenger
    {
        private readonly IMessengerSender messengerSender;
        private readonly ISerializer serializer;
        private readonly ActionTransfer actionTransfer;

        public ActionClientMessenger(IMessengerSender messengerSender, ISerializer serializer, ActionTransfer actionTransfer)
        {
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            this.actionTransfer = actionTransfer;
        }
        [MessengerId((ushort)ActionMessengerIds.Get)]
        public void Get(IConnection connection)
        {
            connection.Write(serializer.Serialize(actionTransfer.GetActionStaticArg()));
        }
        [MessengerId((ushort)ActionMessengerIds.Set)]
        public void Set(IConnection connection)
        {
            actionTransfer.SetActionStaticArg(serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span));
        }
    }

    public class ActionServerMessenger : IMessenger
    {
        private readonly IMessengerSender messengerSender;
        private readonly SignInServerCaching signCaching;
        private readonly ISerializer serializer;

        public ActionServerMessenger(IMessengerSender messengerSender, SignInServerCaching signCaching, ISerializer serializer)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.serializer = serializer;
        }

        [MessengerId((ushort)ActionMessengerIds.GetForward)]
        public void GetForward(IConnection connection)
        {
            string machineid = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, machineid, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                _ = messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)ActionMessengerIds.Get
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK && result.Result.Data.Length > 0)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Payload = result.Result.Data,
                            RequestId = requestid,
                        }, (ushort)ActionMessengerIds.GetForward).ConfigureAwait(false);
                    }
                });
            }
        }
        [MessengerId((ushort)ActionMessengerIds.SetForward)]
        public void SetForward(IConnection connection)
        {
            KeyValuePair<string, string> info = serializer.Deserialize<KeyValuePair<string,string>>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.Key, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                _ = messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)ActionMessengerIds.Set,
                    Payload = serializer.Serialize(info.Value)
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK && result.Result.Data.Length > 0)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Payload = result.Result.Data,
                            RequestId = requestid,
                        }, (ushort)ActionMessengerIds.SetForward).ConfigureAwait(false);
                    }
                });
            }
        }
    }
}
