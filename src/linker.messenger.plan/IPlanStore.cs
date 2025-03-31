namespace linker.messenger.plan
{
    public interface IPlanStore
    {
        public bool Add(PlanInfo info);
        public IEnumerable<PlanInfo> Get();
        public IEnumerable<PlanInfo> Get(string category);
        public PlanInfo Get(string category, string key);
        public bool Remove(int id);
    }

    public interface IPlanHandle
    {
        /// <summary>
        /// 操作分类
        /// </summary>
        public string CategoryName { get; }
        /// <summary>
        /// 操作
        /// </summary>
        /// <param name="handle">操作名</param>
        /// <param name="key">key</param>
        /// <param name="value">值</param>
        public Task HandleAsync(string handle, string key, string value);
    }

    public sealed class PlanInfo
    {
        public int Id { get; set; }

        public string Category { get; set; }
        public string Key { get; set; }
        public string Handle { get; set; }
        public string Value { get; set; }

        public bool Disabled { get; set; }
        public string TriggerHandle { get; set; }

        public PlanMethod Method { get; set; }
        public string Rule { get; set; }
    }
    public sealed class PlanGetInfo
    {
        public string MachineId { get; set; }
        public string Category { get; set; }
    }
    public sealed class PlanAddInfo
    {
        public string  MachineId { get; set; }
        public PlanInfo Plan { get; set; }
    }
    public sealed class PlanRemoveInfo
    {
        public string MachineId { get; set; }
        public int PlanId { get; set; }
    }

    /// <summary>
    /// 计划任务方法
    /// </summary>
    [Flags]
    public enum PlanMethod : byte
    {
        /// <summary>
        /// 手动
        /// </summary>
        None = 0,
        /// <summary>
        /// 启动后
        /// </summary>
        Setup = 1,
        /// <summary>
        /// 到点
        /// </summary>
        At = 2,
        /// <summary>
        /// 定时
        /// </summary>
        Timer = 4,
        /// <summary>
        /// 表达式
        /// </summary>
        Cron = 8,
        /// <summary>
        /// 触发
        /// </summary>
        Trigger = 16,
    }

}
