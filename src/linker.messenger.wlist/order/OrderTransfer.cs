using System.Collections.Frozen;
namespace linker.messenger.wlist.order
{
    public sealed class OrderTransfer
    {
        private readonly FrozenDictionary<string, IOrder> orders;

        private readonly IWhiteListServerStore whiteListServerStore;
        public OrderTransfer(OrderAfdian orderAfdian, IWhiteListServerStore whiteListServerStore)
        {
            orders = new Dictionary<string, IOrder> {
                {orderAfdian.Type,orderAfdian }
            }.ToFrozenDictionary();

            this.whiteListServerStore = whiteListServerStore;
        }

        public async Task<string> AddOrder(string userid, string machineId,string type, string tradeNo)
        {
            if (orders.TryGetValue(whiteListServerStore.Config.Type, out IOrder order))
            {
                return await order.ExecuteAsync(userid, machineId, type, tradeNo).ConfigureAwait(false);
            }
            return $"order interface [{whiteListServerStore.Config.Type}] is not implemented";
        }
        public bool CheckEnabled()
        {
            if (orders.TryGetValue(whiteListServerStore.Config.Type, out IOrder order))
            {
                return order.CheckEnabled();
            }
            return false;
        }
    }
}
