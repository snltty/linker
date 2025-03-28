using Cronos;
using linker.libs.timer;
using linker.messenger.signin;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace linker.messenger.plan
{
    public sealed class PlanTransfer
    {
        private string regex = @"([0-9]+|\*)-([0-9]+|\*)-([0-9]+|\*)\s+([0-9]+|\*):([0-9]+|\*):([0-9]+|\*)";
        private string regexNumver = @"([0-9]+)-([0-9]+)-([0-9]+)\s+([0-9]+):([0-9]+):([0-9]+)";

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

        public IEnumerable<PlanInfo> Get(string category)
        {
            return planStore.Get(category);
        }
        public bool Add(PlanInfo info)
        {
            bool result = planStore.Add(info);

            caches.TryRemove(info.Id, out _);
            PlanExecCacheInfo cache = new PlanExecCacheInfo { Plan = info };
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
            foreach (PlanInfo info in planStore.Get())
            {
                try
                {
                    PlanExecCacheInfo cache = new PlanExecCacheInfo { Plan = info };
                    cache.Active = (cache.Plan.Method < PlanMethod.At) || (UpdateNextTime(cache) && cache.Plan.Method != PlanMethod.Trigger);
                    caches.TryAdd(info.Id, cache);
                }
                catch (Exception)
                {
                }
            }
        }
        private void RunSetup()
        {
            foreach (PlanExecCacheInfo item in caches.Values.Where(c => c.Plan.Method == PlanMethod.Setup && c.Plan.Disabled == false && c.Running == false))
            {
                Run(item);
            }
        }
        private void RunLoop()
        {
            foreach (PlanExecCacheInfo item in caches.Values.Where(c => c.NextTime <= DateTime.Now && c.Plan.Method >= PlanMethod.At))
            {
                Run(item);
            }
        }
        private void Run(PlanExecCacheInfo item)
        {
            if (item.Plan.Disabled || item.Active == false || item.Running || handles.TryGetValue(item.Plan.Category, out IPlanHandle handle) == false)
            {
                return;
            }

            item.Running = true;
            item.Active = (item.Plan.Method < PlanMethod.At) || (UpdateNextTime(item) && item.Plan.Method != PlanMethod.Trigger);
            item.LastTime = DateTime.Now;

            handle.HandleAsync(item.Plan.Handle, item.Plan.Key, item.Plan.Value).ContinueWith((result) =>
            {
                item.Running = false;
                PlanExecCacheInfo trigger = caches.Values.FirstOrDefault(c => c.Plan.Category == item.Plan.Category && c.Plan.Key == item.Plan.Key && c.Plan.TriggerHandle == item.Plan.Handle && c.Plan.TriggerHandle != c.Plan.Handle && c.Plan.Method == PlanMethod.Trigger);
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
                if (cache.Plan.Method == PlanMethod.At)
                {
                    return NextTimeAt(cache);
                }
                else if (cache.Plan.Method == PlanMethod.Timer)
                {
                    return NextTimeTimer(cache);
                }
                else if (cache.Plan.Method == PlanMethod.Cron)
                {
                    return NextTimeCorn(cache);
                }
                else if (cache.Plan.Method == PlanMethod.Trigger)
                {
                    return NextTimeTrigger(cache);
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
                CronExpression cron = CronExpression.Parse(cache.Plan.Rule, CronFormat.IncludeSeconds);
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
            if (Regex.IsMatch(cache.Plan.Rule, regex) == false)
            {
                cache.Error = $"{cache.Plan.Rule} format error";
                return false;
            }

            DateTime from = DateTime.Now;

            GroupCollection groups = Regex.Match(cache.Plan.Rule, regex).Groups;
            int year = groups[1].Value == "*" ? from.Year : int.Parse(groups[1].Value);
            int month = groups[2].Value == "*" ? from.Month : int.Parse(groups[2].Value);
            int day = groups[3].Value == "*" ? from.Day : int.Parse(groups[3].Value);
            int hour = groups[4].Value == "*" ? from.Hour : int.Parse(groups[4].Value);
            int minute = groups[5].Value == "*" ? from.Minute : int.Parse(groups[5].Value);
            int second = groups[6].Value == "*" ? from.Second : int.Parse(groups[6].Value);

            DateTime next = new DateTime(year, month, day, hour, minute, second);
            if (next <= from)
            {
                if (groups[6].Value == "*") next = next.AddSeconds(1);
                else if (groups[5].Value == "*") next = next.AddMinutes(1);
                else if (groups[4].Value == "*") next = next.AddHours(1);
                else if (groups[3].Value == "*") next = next.AddDays(1);
                else if (groups[2].Value == "*") next = next.AddMonths(1);
                else if (groups[1].Value == "*") next = next.AddYears(1);
            }
            cache.NextTime = next;
            return true;
        }
        private bool NextTimeTimer(PlanExecCacheInfo cache)
        {
            if (Regex.IsMatch(cache.Plan.Rule, regexNumver) == false)
            {
                cache.Error = $"{cache.Plan.Rule} format error";
                return false;
            }

            GroupCollection groups = Regex.Match(cache.Plan.Rule, regexNumver).Groups;
            int year = int.Parse(groups[1].Value);
            int month = int.Parse(groups[2].Value);
            int day = int.Parse(groups[3].Value);
            int hour = int.Parse(groups[4].Value);
            int minute = int.Parse(groups[5].Value);
            int second = int.Parse(groups[6].Value);

            cache.NextTime = DateTime.Now.AddYears(year).AddMonths(month).AddDays(day).AddHours(hour).AddMinutes(minute).AddSeconds(second);
            return true;
        }
        private bool NextTimeTrigger(PlanExecCacheInfo cache)
        {
            if (Regex.IsMatch(cache.Plan.Rule, regexNumver) == false)
            {
                cache.Error = $"{cache.Plan.Rule} format error";
                return false;
            }

            GroupCollection groups = Regex.Match(cache.Plan.Rule, regexNumver).Groups;
            int year = int.Parse(groups[1].Value);
            int month = int.Parse(groups[2].Value);
            int day = int.Parse(groups[3].Value);
            int hour = int.Parse(groups[4].Value);
            int minute = int.Parse(groups[5].Value);
            int second = int.Parse(groups[6].Value);

            cache.NextTime = DateTime.Now.AddYears(year).AddMonths(month).AddDays(day).AddHours(hour).AddMinutes(minute).AddSeconds(second);
            return true;
        }

    }

    public sealed class PlanExecCacheInfo
    {
        public PlanInfo Plan { get; set; }
        public DateTime LastTime { get; set; }
        public DateTime NextTime { get; set; }

        public bool Running { get; set; }
        public bool Active { get; set; }

        public string Error { get; set; }
    }

}
