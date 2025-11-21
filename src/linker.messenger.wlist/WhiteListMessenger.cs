using linker.libs;
using linker.messenger.signin;
using linker.messenger.wlist.order;

namespace linker.messenger.wlist
{

    public class WhiteListServerMessenger : IMessenger
    {
        private readonly SignInServerCaching signCaching;
        private readonly ISerializer serializer;
        private readonly IWhiteListServerStore whiteListServerStore;
        private readonly OrderTransfer orderTransfer;

        public WhiteListServerMessenger(SignInServerCaching signCaching, ISerializer serializer, IWhiteListServerStore whiteListServerStore, OrderTransfer orderTransfer)
        {
            this.signCaching = signCaching;
            this.serializer = serializer;
            this.whiteListServerStore = whiteListServerStore;
            this.orderTransfer = orderTransfer;
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)WhiteListMessengerIds.Add)]
        public async Task Add(IConnection connection)
        {
            WhiteListAddInfo info = serializer.Deserialize<WhiteListAddInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false || cache.Super == false)
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
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false || cache.Super == false)
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
            if (cache.Super == false && string.IsNullOrWhiteSpace(info.MachineId))
            {
                info.MachineId = cache.MachineId;
            }
            var page = await whiteListServerStore.Page(info).ConfigureAwait(false);

            connection.Write(serializer.Serialize(page));
        }


        /// <summary>
        /// 状态
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)WhiteListMessengerIds.Status)]
        public async Task Status(IConnection connection)
        {
            KeyValuePair<string, string> info;

            try
            {
                info = serializer.Deserialize<KeyValuePair<string, string>>(connection.ReceiveRequestWrap.Payload.Span);
            }
            catch (Exception)
            {
                string type = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
                info = new KeyValuePair<string, string>(type, connection.Id);
            }

            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false || signCaching.TryGet(info.Value, out SignCacheInfo cacheTo) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            List<WhiteListInfo> list = await whiteListServerStore.Get(info.Key, [cacheTo.UserId], [cacheTo.MachineId]).ConfigureAwait(false);
            WhiteListInfo result = list.FirstOrDefault(c => c.Bandwidth < 0) ?? list.FirstOrDefault(c => c.Bandwidth == 0) ?? list.OrderByDescending(c => c.Bandwidth).FirstOrDefault();

            connection.Write(serializer.Serialize(new WhiteListOrderStatusInfo
            {
                Type = whiteListServerStore.Config.Type,
                Enabled = orderTransfer.CheckEnabled(),
                Info = result
            }));
        }


        /// <summary>
        /// 添加订单
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)WhiteListMessengerIds.AddOrder)]
        public async Task AddOrder(IConnection connection)
        {
            KeyValuePair<string, string> kvp = serializer.Deserialize<KeyValuePair<string, string>>(connection.ReceiveRequestWrap.Payload.Span);

            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(serializer.Serialize("please sign in"));
                return;
            }

            connection.Write(serializer.Serialize(await orderTransfer.AddOrder(cache.UserId, cache.MachineId, kvp.Key, kvp.Value)));
        }


        /// <summary>
        /// 列表
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)WhiteListMessengerIds.List)]
        public async Task List(IConnection connection)
        {
            KeyValuePair<string, List<string>> info = serializer.Deserialize<KeyValuePair<string, List<string>>>(connection.ReceiveRequestWrap.Payload.Span);
            List<string> userids = signCaching.GetUserIds(info.Value);

            List<WhiteListInfo> whites = await whiteListServerStore.Get(info.Key, userids, info.Value).ConfigureAwait(false);

            var result = whites.Where(c => string.IsNullOrWhiteSpace(c.UserId) == false).GroupBy(c => c.UserId)
                .ToDictionary(c => $"u_{c.Key}", v => v.Select(c => c).ToDictionary(x => x.Id, x => x.Bandwidth))
                .Concat(whites.Where(c => string.IsNullOrWhiteSpace(c.MachineId) == false).GroupBy(c => c.MachineId)
                .ToDictionary(c => $"m_{c.Key}", v => v.Select(c => c).ToDictionary(x => x.Id, x => x.Bandwidth)))
                .ToDictionary();
            connection.Write(serializer.Serialize(result));
        }
    }
}
