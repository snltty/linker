using cmonitor.plugins.snatch.report;
using LiteDB;

namespace cmonitor.plugins.snatch.db
{
    public interface ISnatchDB
    {
        public bool Add(SnatchUserInfo snatchUserInfo);
        public List<SnatchUserInfo> Get();
    }

    public sealed class SnatchUserInfo
    {
        public ObjectId Id { get; set; }
        public string UserName { get; set; }
        public List<SnatchGroupInfo> Data { get; set; }
    }
    public sealed class SnatchGroupInfo
    {
        public string Name { get; set; }
        public List<SnatchItemInfo> List { get; set; } = new List<SnatchItemInfo>();
    }

    public sealed class SnatchItemInfo
    {
        public uint ID { get; set; }
        public string Title { get; set; }

        public SnatchCate Cate { get; set; } = SnatchCate.Question;
        public SnatchType Type { get; set; } = SnatchType.Select;
        /// <summary>
        /// 问题
        /// </summary>
        public string Question { get; set; }
        /// <summary>
        /// 选项数
        /// </summary>
        public List<SnatchItemOptionInfo> Options { get; set; }
        /// <summary>
        /// 答案
        /// </summary>
        public string Correct { get; set; }
        /// <summary>
        /// 最多答题次数
        /// </summary>
        public int Chance { get; set; }
    }
    public sealed class SnatchItemOptionInfo
    {
        public string Text { get; set; }
        public bool Value { get; set; }
    }
}
