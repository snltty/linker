using Cronos;
using System.Collections.Concurrent;

namespace linker.messenger.plan
{
    public sealed class PlanTransfer
    {
        private readonly ConcurrentDictionary<string, IPlanHandle> handles = new ConcurrentDictionary<string, IPlanHandle>();
        private readonly ConcurrentDictionary<int, PlanExecCacheInfo> caches = new ConcurrentDictionary<int, PlanExecCacheInfo>();

        private readonly IPlanStore planStore;
        public PlanTransfer(IPlanStore planStore)
        {
            this.planStore = planStore;
            PlanTask();
        }

        public void AddHandle(IPlanHandle handle)
        {
            handles.AddOrUpdate(handle.CategoryName, handle, (a, b) => handle);
        }

        public IEnumerable<PlanStoreInfo> Get(string category)
        {
            return planStore.Get(category);
        }
        public bool Add(PlanStoreInfo info)
        {
            bool result = planStore.Add(info);

            caches.TryRemove(info.Id, out _);
            PlanExecCacheInfo cache = new PlanExecCacheInfo { Store = info };
            UpdateNextTime(cache);
            caches.TryAdd(info.Id, cache);

            return result;
        }
        public bool Remove(int id)
        {
            bool result = planStore.Remove(id);
            caches.TryRemove(id, out _);
            return result;
        }

        private void PlanTask()
        {
            foreach (PlanStoreInfo info in planStore.Get())
            {
                PlanExecCacheInfo cache = new PlanExecCacheInfo { Store = info };
                UpdateNextTime(cache);
                caches.TryAdd(info.Id, cache);
            }
        }
        private void UpdateNextTime(PlanExecCacheInfo cache)
        {
            cache.LastTime = DateTime.Now;
            if (cache.Store.Method == PlanMethod.Timer)
            {
                NextTimeTimer(cache);
            }
            else if (cache.Store.Method == PlanMethod.Corn)
            {
                NextTimeCorn(cache);
            }
        }
        private void NextTimeCorn(PlanExecCacheInfo cache)
        {
            CronExpression cron = CronExpression.Parse(cache.Store.Corn);
            DateTimeOffset? nextOccurrence = cron.GetNextOccurrence(DateTimeOffset.Now, TimeZoneInfo.Local);
            if (nextOccurrence.HasValue)
            {
                cache.NextTime = nextOccurrence.Value.LocalDateTime;
            }
        }
        private void NextTimeTimer(PlanExecCacheInfo cache)
        {
            DateTime from = DateTime.Now;
            int month = cache.Store.Month;
            int day = cache.Store.Day;
            int hour = cache.Store.Hour;
            int minute = cache.Store.Min;
            int second = cache.Store.Sec;

            DateTime next = cache.Store.Period switch
            {
                PlanPeriod.Year => new DateTime(from.Year, month, day, hour, minute, second),
                PlanPeriod.Month => new DateTime(from.Year, from.Month, day, hour, minute, second),
                PlanPeriod.Day => new DateTime(from.Year, from.Month, from.Day, hour, minute, second),
                PlanPeriod.Hour => new DateTime(from.Year, from.Month, from.Day, from.Hour, minute, second),
                PlanPeriod.Min => new DateTime(from.Year, from.Month, from.Day, from.Hour, from.Minute, second),
                _ => new DateTime(from.Year, from.Month, from.Day, from.Hour, from.Minute, from.Second),
            };
            if (next <= from)
            {
                switch (cache.Store.Period)
                {
                    case PlanPeriod.Year:
                        next = next.AddYears(1);
                        break;
                    case PlanPeriod.Month:
                        next = next.AddMonths(1);
                        break;
                    case PlanPeriod.Day:
                        next = next.AddDays(1);
                        break;
                    case PlanPeriod.Hour:
                        next = next.AddHours(1);
                        break;
                    case PlanPeriod.Min:
                        next = next.AddSeconds(1);
                        break;
                    default:
                        break;
                }
            }
            cache.NextTime = next;
        }
    }

    public sealed class PlanExecCacheInfo
    {
        public PlanStoreInfo Store { get; set; }
        public DateTime LastTime { get; set; }
        public DateTime NextTime { get; set; }

        public bool Running { get; set; }
    }

}
