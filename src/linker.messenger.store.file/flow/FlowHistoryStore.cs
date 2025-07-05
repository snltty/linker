using linker.messenger.flow.history;
using LiteDB;

namespace linker.messenger.store.file.flow
{
    public sealed class FlowHistoryStore : IFlowHistoryStore
    {
        private readonly Storefactory dBfactory;
        private readonly ILiteCollection<FlowHistoryInfo> liteCollection;
        public FlowHistoryStore(Storefactory dBfactory)
        {
            this.dBfactory = dBfactory;
            liteCollection = dBfactory.GetCollection<FlowHistoryInfo>("flowHistory");
        }

        public bool Add(FlowHistoryInfo flowHistory)
        {
            liteCollection.Insert(flowHistory);
            return true;
        }
        public bool Clear(int days)
        {
            return liteCollection.DeleteMany(c => c.Time < DateTime.Now.AddDays(-days)) > 0;
        }

        public List<FlowHistoryInfo> Query(FlowHistoryQueryInfo info)
        {
            (DateTime start, DateTime end) = info.Type switch
            {
                FlowHistoryQueryType.Hour => (DateTime.Parse($"{info.Time:yyyy-MM-dd HH}:00:00"), DateTime.Parse($"{info.Time:yyyy-MM-dd HH}:59:59")),
                FlowHistoryQueryType.Day => (DateTime.Parse($"{info.Time:yyyy-MM-dd} 00:00:00"), DateTime.Parse($"{info.Time:yyyy-MM-dd} 23:59:59")),
                FlowHistoryQueryType.Month => (DateTime.Parse($"{info.Time:yyyy-MM}-01 00:00:00"), DateTime.Parse($"{info.Time:yyyy-MM}-{DateTime.Parse(info.Time.AddMonths(1).ToString("yyyy-MM-01")).AddDays(-1).Day} 23:59:59")),
                FlowHistoryQueryType.Year => (DateTime.Parse($"{info.Time:yyyy}-01-01 00:00:00"), DateTime.Parse($"{info.Time:yyyy}-12-31 23:59:59")),
                _ => (DateTime.MinValue, DateTime.MaxValue)
            };

            var query = liteCollection.Query();
            query = query.Where(c => c.Time >= start && c.Time <= end);
            if (string.IsNullOrWhiteSpace(info.Key) == false)
            {
                query = query.Where(c => c.Key == info.Key);
            }

            return query.ToList();
        }
    }
}
