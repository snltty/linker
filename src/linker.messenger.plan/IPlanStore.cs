namespace linker.messenger.plan
{
    public interface IPlanStore
    {
        public bool Add(PlanStoreInfo info);
        public IEnumerable<PlanStoreInfo> Get();
        public IEnumerable<PlanStoreInfo> Get(string category);
        public PlanStoreInfo Get(string category,string key);
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
        public Task HandleAsync(string handle,string key,string value);
    }

    public sealed class PlanStoreInfo
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

    /// <summary>
    /// 计划任务方法
    /// </summary>
    public enum PlanMethod : byte
    {
        /// <summary>
        /// 手动
        /// </summary>
        Hand = 0,
        /// <summary>
        /// 定时
        /// </summary>
        Timer = 1,
        /// <summary>
        /// 表达式
        /// </summary>
        Cron = 2,
        /// <summary>
        /// 启动后
        /// </summary>
        Setup = 4,
        /// <summary>
        /// 触发
        /// </summary>
        Trigger = 8,
    }
    
}
