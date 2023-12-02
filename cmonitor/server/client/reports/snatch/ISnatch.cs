using MemoryPack;

namespace cmonitor.server.client.reports.snatch
{
    public interface ISnatch
    {
        public void Set(SnatchQuestionInfo snatchQuestionInfo);
    }

    [MemoryPackable]
    public sealed partial class SnatchQuestionInfo
    {
        public SnatchType Type { get; set; }
        /// <summary>
        /// 问题
        /// </summary>
        public string Question { get; set; }
        /// <summary>
        /// 正确答案
        /// </summary>
        public string Correct { get; set; }
        /// <summary>
        /// 选项数
        /// </summary>
        public int Option { get; set; }
        /// <summary>
        /// 最多答题次数
        /// </summary>
        public int Max { get; set; } = int.MaxValue;
        /// <summary>
        /// 已结束
        /// </summary>
        public bool End { get; set; } = false;
        /// <summary>
        /// 重复答题
        /// </summary>
        public bool Repeat { get; set; } = true;
        public int Join { get; set; } = 0;
        public int Right { get; set; } = 0;
        public int Wrong { get; set; } = 0;
    }
    public sealed class SnatchAnswerInfo
    {
        public SnatchState State { get; set; }
        public bool Result { get; set; }
        public long Time { get; set; }
        public string ResultStr { get; set; }
    }

    public enum SnatchState : byte
    {
        None = 0,
        Ask = 1,
        Confirm = 2,
    }
    public enum SnatchType : byte
    {
        None = 0,
        Select = 1,
        Input = 2,
    }
}
