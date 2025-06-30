using linker.libs;
using linker.messenger.signin;

namespace linker.messenger.wlist
{

    /// <summary>
    /// 中继服务端
    /// </summary>
    public class WhiteListServerMessenger : IMessenger
    {
        private readonly IMessengerSender messengerSender;
        private readonly SignInServerCaching signCaching;
        private readonly ISerializer serializer;
        private readonly IWhiteListServerStore whiteListServerStore;

        public WhiteListServerMessenger(IMessengerSender messengerSender, SignInServerCaching signCaching, ISerializer serializer, IWhiteListServerStore whiteListServerStore)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.serializer = serializer;
            this.whiteListServerStore = whiteListServerStore;
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)WhiteListMessengerIds.Add)]
        public async Task Add(IConnection connection)
        {
            WhiteListAddInfo info = serializer.Deserialize<WhiteListAddInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            if (whiteListServerStore.ValidateSecretKey(info.SecretKey) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            await whiteListServerStore.Add(info.Data).ConfigureAwait(false);
            connection.Write(Helper.TrueArray);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)WhiteListMessengerIds.Del)]
        public async Task Del(IConnection connection)
        {
            WhiteListDelInfo info = serializer.Deserialize<WhiteListDelInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            if (whiteListServerStore.ValidateSecretKey(info.SecretKey) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            await whiteListServerStore.Del(info.Id).ConfigureAwait(false);
            connection.Write(Helper.TrueArray);
        }

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)WhiteListMessengerIds.Page)]
        public async Task Page(IConnection connection)
        {
            WhiteListPageRequestInfo info = serializer.Deserialize<WhiteListPageRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(serializer.Serialize(new WhiteListPageResultInfo { }));
                return;
            }
            if (whiteListServerStore.ValidateSecretKey(info.SecretKey) == false && string.IsNullOrWhiteSpace(info.UserId))
            {
                connection.Write(serializer.Serialize(new WhiteListPageResultInfo { }));
                return;
            }

            var page = await whiteListServerStore.Page(info).ConfigureAwait(false);

            connection.Write(serializer.Serialize(page));
        }

        [MessengerId((ushort)WhiteListMessengerIds.CheckKey)]
        public void CheckKey(IConnection connection)
        {
            string key = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            connection.Write(whiteListServerStore.ValidateSecretKey(key) ? Helper.TrueArray : Helper.FalseArray);
        }

    }
}
