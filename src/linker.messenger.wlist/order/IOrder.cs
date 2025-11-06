namespace linker.messenger.wlist.order
{
    public interface IOrder
    {
        public string Type { get; }

        public Task<string> ExecuteAsync(string userid, string machineId,string type, string tradeNo);
        public bool CheckEnabled();
    }
}
