using Cronos;
using linker.libs.timer;
using linker.messenger.signin;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace linker.messenger.plan
{
    public sealed class PlanTransfer
    {
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
            try
            {
                return planStore.Get(category);
            }
            catch (Exception)
            {
            }
            return [];
        }
        public bool Add(PlanInfo info)
        {
            bool result = planStore.Add(info);
            caches.TryRemove(info.Id, out _);
            caches.TryAdd(info.Id, new PlanExecCacheInfo(info));
            return result;
        }
        public bool Remove(int id)
        {
            bool result = planStore.Remove(id);
            caches.TryRemove(id, out _);
            return result;
        }

        public void Trigger(string category, string key, string handle)
        {
            PlanExecCacheInfo trigger = caches.Values.FirstOrDefault(c => c.Plan.Category == category && c.Plan.Key == key && c.Plan.TriggerHandle == handle && c.Plan.TriggerHandle != c.Plan.Handle && c.Plan.Method == PlanMethod.Trigger);
            trigger?.UpdateNextTime(1);
        }

        private void PlanTask()
        {
            Load();
            signInClientState.OnSignInSuccess += (times) => RunSetup();
            TimerHelper.SetIntervalLong(RunLoop, 500);
        }
        private void Load()
        {
            try
            {
                foreach (PlanInfo info in planStore.Get())
                {
                    caches.TryAdd(info.Id, new PlanExecCacheInfo(info));
                }
            }
            catch (Exception)
            {
            }
        }
        private void RunSetup()
        {
            try
            {
                foreach (PlanExecCacheInfo item in caches.Values.Where(c => c.Plan.Method == PlanMethod.Setup))
                {
                    item.UpdateNextTime(1);
                }
            }
            catch (Exception)
            {
            }
        }
        private void RunLoop()
        {
            foreach (PlanExecCacheInfo item in caches.Values.Where(c => c.Plan.Disabled == false && c.Running == false && c.Times > 0 && c.NextTime != null && c.NextTime <= DateTime.Now))
            {
                Run(item);
            }
        }
        private void Run(PlanExecCacheInfo item)
        {
            if (handles.TryGetValue(item.Plan.Category, out IPlanHandle handle))
            {
                item.StartRun();
                item.UpdateNextTime(0);
                handle.HandleAsync(item.Plan.Handle, item.Plan.Key, item.Plan.Value).ContinueWith((result) =>
                {
                    item.EndRun();
                    Trigger(item.Plan.Category, item.Plan.Key, item.Plan.Handle);
                });
            }
        }

    }

    public sealed class PlanExecCacheInfo
    {
        public static string regex = @"([0-9]+|\*)-([0-9]+|\*)-([0-9]+|\*)\s+([0-9]+|\*):([0-9]+|\*):([0-9]+|\*)";
        public static string regexNumver = @"([0-9]+)-([0-9]+)-([0-9]+)\s+([0-9]+):([0-9]+):([0-9]+)";

        public PlanInfo Plan { get; set; }
        public DateTime LastTime { get; set; }
        public DateTime? NextTime { get; set; } = DateTime.Now;
        public bool Running { get; set; }
        public ulong Times { get; set; }
        public string Error { get; set; }


        public PlanExecCacheInfo(PlanInfo plan)
        {
            Plan = plan;
            TimesMethod();
            NextTimeMethod();
        }

        public void StartRun()
        {
            Running = true;
            LastTime = DateTime.Now;
            Times--;
        }
        public void EndRun()
        {
            Running = false;
        }
        public void UpdateNextTime(ulong addTimes = 0)
        {
            Times += addTimes;
            NextTimeMethod();
        }

        private void TimesMethod()
        {
            Times = Plan.Method switch
            {
                PlanMethod.None => 0,
                PlanMethod.Setup => 0,
                PlanMethod.At => ulong.MaxValue,
                PlanMethod.Timer => ulong.MaxValue,
                PlanMethod.Cron => ulong.MaxValue,
                PlanMethod.Trigger => 0,
                _ => 0,
            };
        }
        private bool NextTimeMethod()
        {
            try
            {
                return Plan.Method switch
                {
                    PlanMethod.None => false,
                    PlanMethod.Setup => true,
                    PlanMethod.At => NextTimeAt(),
                    PlanMethod.Timer => NextTimeTimer(),
                    PlanMethod.Cron => NextTimeCorn(),
                    PlanMethod.Trigger => NextTimeTrigger(),
                    _ => false,
                };
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            return false;
        }
        private bool NextTimeCorn()
        {
            try
            {
                CronExpression cron = CronExpression.Parse(Plan.Rule, CronFormat.IncludeSeconds);
                DateTimeOffset? nextOccurrence = cron.GetNextOccurrence(DateTimeOffset.Now, TimeZoneInfo.Local);
                if (nextOccurrence.HasValue)
                {
                    NextTime = nextOccurrence.Value.LocalDateTime;
                    return true;
                }
                NextTime = null;
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            return false;
        }
        private bool NextTimeAt()
        {
            if (Regex.IsMatch(Plan.Rule, regex) == false)
            {
                Error = $"{Plan.Rule} format error";
                NextTime = null;
                return false;
            }

            DateTime from = DateTime.Now;

            GroupCollection groups = Regex.Match(Plan.Rule, regex).Groups;
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
            NextTime = next;
            return true;
        }
        private bool NextTimeTimer()
        {
            if (Regex.IsMatch(Plan.Rule, regexNumver) == false)
            {
                Error = $"{Plan.Rule} format error";
                NextTime = null;
                return false;
            }

            GroupCollection groups = Regex.Match(Plan.Rule, regexNumver).Groups;
            int year = int.Parse(groups[1].Value);
            int month = int.Parse(groups[2].Value);
            int day = int.Parse(groups[3].Value);
            int hour = int.Parse(groups[4].Value);
            int minute = int.Parse(groups[5].Value);
            int second = int.Parse(groups[6].Value);

            NextTime = DateTime.Now.AddYears(year).AddMonths(month).AddDays(day).AddHours(hour).AddMinutes(minute).AddSeconds(second);
            return true;
        }
        private bool NextTimeTrigger()
        {
            if (Regex.IsMatch(Plan.Rule, regexNumver) == false)
            {
                Error = $"{Plan.Rule} format error";
                NextTime = null;
                return false;
            }

            GroupCollection groups = Regex.Match(Plan.Rule, regexNumver).Groups;
            int year = int.Parse(groups[1].Value);
            int month = int.Parse(groups[2].Value);
            int day = int.Parse(groups[3].Value);
            int hour = int.Parse(groups[4].Value);
            int minute = int.Parse(groups[5].Value);
            int second = int.Parse(groups[6].Value);

            NextTime = DateTime.Now.AddYears(year).AddMonths(month).AddDays(day).AddHours(hour).AddMinutes(minute).AddSeconds(second);
            return true;
        }
    }

}
