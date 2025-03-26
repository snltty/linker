using Cronos;
using linker.libs.timer;
using linker.messenger.signin;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace linker.messenger.plan
{
    public sealed class PlanTransfer
    {
        private string regex = @"([0-9]+|\?)-([0-9]+|\?)-([0-9]+|\?)\s+([0-9]+|\?):([0-9]+|\?):([0-9]+|\?)";

        private readonly ConcurrentDictionary<string, IPlanHandle> handles = new ConcurrentDictionary<string, IPlanHandle>();
        private readonly ConcurrentDictionary<int, PlanExecCacheInfo> caches = new ConcurrentDictionary<int, PlanExecCacheInfo>();

        private readonly IPlanStore planStore;
        private readonly SignInClientState signInClientState;
        public PlanTransfer(IPlanStore planStore, SignInClientState signInClientState)
        {
            this.planStore = planStore;
            this.signInClientState = signInClientState;
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
            cache.Active = UpdateNextTime(cache) && string.IsNullOrWhiteSpace(info.TriggerHandle);
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
            Load();
            signInClientState.OnSignInSuccess += (times) => RunSetup();
            TimerHelper.SetIntervalLong(RunLoop, 500);
        }
        private void Load()
        {
            foreach (PlanStoreInfo info in planStore.Get())
            {
                try
                {
                    PlanExecCacheInfo cache = new PlanExecCacheInfo { Store = info };
                    cache.Active = (cache.Store.Method < PlanMethod.At) || (UpdateNextTime(cache) && cache.Store.Method != PlanMethod.Trigger);
                    caches.TryAdd(info.Id, cache);
                }
                catch (Exception)
                {
                }
            }
        }
        private void RunSetup()
        {
            foreach (PlanExecCacheInfo item in caches.Values.Where(c => c.Store.Method == PlanMethod.Setup && c.Store.Disabled == false && c.Running == false))
            {
                Run(item);
            }
        }
        private void RunLoop()
        {
            foreach (PlanExecCacheInfo item in caches.Values.Where(c => c.NextTime <= DateTime.Now && c.Store.Method >= PlanMethod.At))
            {
                Run(item);
            }
        }
        private void Run(PlanExecCacheInfo item)
        {
            if(item.Store.Disabled || item.Active == false || item.Running || handles.TryGetValue(item.Store.Category, out IPlanHandle handle) == false)
            {
                return;
            }

            item.Running = true;
            item.Active = (item.Store.Method < PlanMethod.At) || (UpdateNextTime(item) && item.Store.Method != PlanMethod.Trigger);
            item.LastTime = DateTime.Now;

            handle.HandleAsync(item.Store.Handle, item.Store.Key, item.Store.Value).ContinueWith((result) =>
            {
                item.Running = false;
                PlanExecCacheInfo trigger = caches.Values.FirstOrDefault(c => c.Store.Category == item.Store.Category && c.Store.Key == item.Store.Key && c.Store.TriggerHandle == item.Store.Handle && c.Store.TriggerHandle != c.Store.Handle && c.Store.Method == PlanMethod.Trigger);
                if (trigger != null)
                {
                    trigger.Active = UpdateNextTime(trigger);
                }
            });
        }

        private bool UpdateNextTime(PlanExecCacheInfo cache)
        {
            try
            {
                if (cache.Store.Method == PlanMethod.At)
                {
                    return NextTimeAt(cache);
                }
                else if (cache.Store.Method == PlanMethod.Timer)
                {
                    return NextTimeTimer(cache);
                }
                else if (cache.Store.Method == PlanMethod.Cron)
                {
                    return NextTimeCorn(cache);
                }
                else if (cache.Store.Method == PlanMethod.Trigger)
                {
                    return NextTimeAfter(cache);
                }
            }
            catch (Exception ex)
            {
                cache.Error = ex.Message;
            }
            return false;
        }
        private bool NextTimeCorn(PlanExecCacheInfo cache)
        {
            try
            {
                CronExpression cron = CronExpression.Parse(cache.Store.Rule, CronFormat.IncludeSeconds);
                DateTimeOffset? nextOccurrence = cron.GetNextOccurrence(DateTimeOffset.Now, TimeZoneInfo.Local);
                if (nextOccurrence.HasValue)
                {
                    cache.NextTime = nextOccurrence.Value.LocalDateTime;

                }
                return true;
            }
            catch (Exception ex)
            {
                cache.Error = ex.Message;
            }
            return false;
        }
        private bool NextTimeAt(PlanExecCacheInfo cache)
        {
            if (Regex.IsMatch(cache.Store.Rule, regex) == false)
            {
                cache.Error = $"{cache.Store.Rule} format error";
                return false;
            }

            DateTime from = DateTime.Now;

            GroupCollection groups = Regex.Match(cache.Store.Rule, regex).Groups;
            int year = groups[1].Value == "?" ? from.Year : int.Parse(groups[1].Value);
            int month = groups[2].Value == "?" ? from.Month : int.Parse(groups[2].Value);
            int day = groups[3].Value == "?" ? from.Day : int.Parse(groups[3].Value);
            int hour = groups[4].Value == "?" ? from.Hour : int.Parse(groups[4].Value);
            int minute = groups[5].Value == "?" ? from.Minute : int.Parse(groups[5].Value);
            int second = groups[6].Value == "?" ? from.Second : int.Parse(groups[6].Value);

            DateTime next = new DateTime(year, month, day, hour, minute, second);
            if (next <= from)
            {
                if (groups[5].Value == "?") next = next.AddMinutes(1);
                else if (groups[4].Value == "?") next = next.AddHours(1);
                else if (groups[3].Value == "?") next = next.AddDays(1);
                else if (groups[2].Value == "?") next = next.AddMonths(1);
                else if (groups[1].Value == "?") next = next.AddYears(1);
            }
            cache.NextTime = next;
            return true;
        }
        private bool NextTimeTimer(PlanExecCacheInfo cache)
        {
            if (Regex.IsMatch(cache.Store.Rule, regex) == false)
            {
                cache.Error = $"{cache.Store.Rule} format error";
                return false;
            }

            GroupCollection groups = Regex.Match(cache.Store.Rule, regex).Groups;
            int year = groups[1].Value == "?" ? 0 : int.Parse(groups[1].Value);
            int month = groups[2].Value == "?" ? 0 : int.Parse(groups[2].Value);
            int day = groups[3].Value == "?" ? 0 : int.Parse(groups[3].Value);
            int hour = groups[4].Value == "?" ? 0 : int.Parse(groups[4].Value);
            int minute = groups[5].Value == "?" ? 0 : int.Parse(groups[5].Value);
            int second = groups[6].Value == "?" ? 0 : int.Parse(groups[6].Value);

            cache.NextTime = DateTime.Now.AddYears(year).AddMonths(month).AddDays(day).AddHours(hour).AddMinutes(minute).AddSeconds(second);
            return true;
        }
        private bool NextTimeAfter(PlanExecCacheInfo cache)
        {
            if (Regex.IsMatch(cache.Store.Rule, regex) == false)
            {
                cache.Error = $"{cache.Store.Rule} format error";
                return false;
            }

            GroupCollection groups = Regex.Match(cache.Store.Rule, regex).Groups;
            int year = groups[1].Value == "?" ? 0 : int.Parse(groups[1].Value);
            int month = groups[2].Value == "?" ? 0 : int.Parse(groups[2].Value);
            int day = groups[3].Value == "?" ? 0 : int.Parse(groups[3].Value);
            int hour = groups[4].Value == "?" ? 0 : int.Parse(groups[4].Value);
            int minute = groups[5].Value == "?" ? 0 : int.Parse(groups[5].Value);
            int second = groups[6].Value == "?" ? 0 : int.Parse(groups[6].Value);

            cache.NextTime = DateTime.Now.AddYears(year).AddMonths(month).AddDays(day).AddHours(hour).AddMinutes(minute).AddSeconds(second);
            return true;
        }

    }

    public sealed class PlanExecCacheInfo
    {
        public PlanStoreInfo Store { get; set; }
        public DateTime LastTime { get; set; }
        public DateTime NextTime { get; set; }

        public bool Running { get; set; }
        public bool Active { get; set; }

        public string Error { get; set; }
    }

}
