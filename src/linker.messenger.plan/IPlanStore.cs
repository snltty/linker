namespace linker.messenger.plan
{
    public interface IPlanStore
    {
        public bool Add(PlanStoreInfo info);
        public IEnumerable<PlanStoreInfo> Get();
        public IEnumerable<PlanStoreInfo> Get(string category);
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
        /// <param name="value">值</param>
        public Task HandleAsync(string handle,string value);
    }

    public sealed class PlanStoreInfo
    {
        public int Id { get; set; }
        public string Value { get; set; }

        public string Category { get; set; }
        public string Handle { get; set; }

        public PlanMethod Method { get; set; }
        public PlanPeriod Period { get; set; }

        public string Corn { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int Hour { get; set; }
        public int Min { get; set; }
        public int Sec { get; set; }
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
        Corn = 2,
        /// <summary>
        /// 启动后
        /// </summary>
        Setup = 4,
    }
    /// <summary>
    /// 计划任务周期
    /// </summary>
    public enum PlanPeriod : byte
    {
        Year = 0,
        Month = 1,
        Day = 2,
        Hour = 4,
        Min = 8
    }
}
