using linker.libs;
using linker.messenger.signin;

namespace linker.messenger.cdkey
{
    /// <summary>
    /// 中继服务端
    /// </summary>
    public class CdkeyServerMessenger : IMessenger
    {
        private readonly IMessengerSender messengerSender;
        private readonly SignInServerCaching signCaching;
        private readonly ISerializer serializer;
        private readonly ICdkeyServerStore cdkeyStore;

        public CdkeyServerMessenger(IMessengerSender messengerSender, SignInServerCaching signCaching, ISerializer serializer, ICdkeyServerStore cdkeyStore)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.serializer = serializer;
            this.cdkeyStore = cdkeyStore;
        }
        /// <summary>
        /// 添加CDKEY
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)CdkeyMessengerIds.AddCdkey)]
        public async Task AddCdkey(IConnection connection)
        {
            CdkeyAddInfo info = serializer.Deserialize<CdkeyAddInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            if (cdkeyStore.ValidateSecretKey(info.SecretKey) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            await cdkeyStore.Add(info.Data).ConfigureAwait(false);
            connection.Write(Helper.TrueArray);
        }

        /// <summary>
        /// 删除Cdkey
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)CdkeyMessengerIds.DelCdkey)]
        public async Task DelCdkey(IConnection connection)
        {
            CdkeyDelInfo info = serializer.Deserialize<CdkeyDelInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            if (cdkeyStore.ValidateSecretKey(info.SecretKey))
            {
                await cdkeyStore.Del(info.Id).ConfigureAwait(false);
            }
            else
            {
                await cdkeyStore.Del(info.Id, info.UserId).ConfigureAwait(false);
            }
            connection.Write(Helper.TrueArray);
        }

        /// <summary>
        /// 查询CDKEY
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)CdkeyMessengerIds.PageCdkey)]
        public async Task PageCdkey(IConnection connection)
        {
            CdkeyPageRequestInfo info = serializer.Deserialize<CdkeyPageRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(serializer.Serialize(new CdkeyPageResultInfo { }));
                return;
            }
            if (cdkeyStore.ValidateSecretKey(info.SecretKey) == false && string.IsNullOrWhiteSpace(info.UserId))
            {
                connection.Write(serializer.Serialize(new CdkeyPageResultInfo { }));
                return;
            }

            var page = await cdkeyStore.Page(info).ConfigureAwait(false);

            connection.Write(serializer.Serialize(page));
        }


        /// <summary>
        /// 测试cdkey
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)CdkeyMessengerIds.TestCdkey)]
        public async Task TestCdkey(IConnection connection)
        {
            CdkeyImportInfo info = serializer.Deserialize<CdkeyImportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(serializer.Serialize(new CdkeyTestResultInfo { }));
                return;
            }
            if (cdkeyStore.ValidateSecretKey(info.SecretKey) == false)
            {
                connection.Write(serializer.Serialize(new CdkeyTestResultInfo { }));
                return;
            }
            CdkeyTestResultInfo test = await cdkeyStore.Test(info).ConfigureAwait(false);
            connection.Write(serializer.Serialize(test));
        }

        /// <summary>
        /// 导入cdkey
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)CdkeyMessengerIds.ImportCdkey)]
        public async Task ImportCdkey(IConnection connection)
        {
            CdkeyImportInfo info = serializer.Deserialize<CdkeyImportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            string result = await cdkeyStore.Import(info).ConfigureAwait(false);
            connection.Write(serializer.Serialize(result));
        }


        [MessengerId((ushort)CdkeyMessengerIds.CheckKey)]
        public void AccessCdkey(IConnection connection)
        {
            string key = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            connection.Write(cdkeyStore.ValidateSecretKey(key) ? Helper.TrueArray : Helper.FalseArray);
        }
    }
}
