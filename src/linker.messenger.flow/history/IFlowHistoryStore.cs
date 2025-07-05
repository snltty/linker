namespace linker.messenger.flow.history
{
    public interface IFlowHistoryStore
    {
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="flowHistory"></param>
        /// <returns></returns>
        bool Add(FlowHistoryInfo flowHistory);
        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        List<FlowHistoryInfo> Query(FlowHistoryQueryInfo info);
        /// <summary>
        /// 清除
        /// </summary>
        /// <param name="days">保留天数</param>
        /// <returns></returns>
        bool Clear(int days);
    }

    public sealed class FlowHistoryQueryInfo
    {

        public DateTime Time { get; set; }
        public FlowHistoryQueryType Type { get; set; }
        public string Key { get; set; }
    }
    public enum FlowHistoryQueryType : byte
    {
        Hour = 1,
        Day = 2,
        Month = 3,
        Year = 4
    }

    public sealed class FlowHistoryInfo
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public DateTime Time { get; set; }
        public long Recv { get; set; }
        public long Sendt { get; set; }

    }
}
