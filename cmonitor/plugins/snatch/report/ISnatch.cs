using common.libs.extends;
using System.Text.Json.Serialization;

namespace cmonitor.plugins.snatch.report
{
    public interface ISnatch
    {
        public void StartUp(SnatchQuestionInfo snatchQuestionInfo);
    }

    public sealed partial class SnatchRandomInfo
    {
        public bool End { get; set; }
        public int Join { get; set; }
        public int Right { get; set; }
        public int Wrong { get; set; }

    }

    public sealed partial class SnatchQuestionInfo
    {
        public string UserName { get; set; }
        public SnatchCate Cate { get; set; }
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
        /// 已结束
        /// </summary>
        public bool End { get; set; }
        /// <summary>
        /// 选项数
        /// </summary>
        public int Option { get; set; }
        /// <summary>
        /// 最多答题次数
        /// </summary>
        public ushort Chance { get; set; } = ushort.MaxValue;

        public int Join { get; set; }
        public int Right { get; set; }
        public int Wrong { get; set; }

        public byte[] ToBytes()
        {
            ReadOnlySpan<byte> questionBytes = Question.GetUTF16Bytes();
            ReadOnlySpan<byte> correctBytes = Correct.GetUTF16Bytes();
            ReadOnlySpan<byte> nameBytes = UserName.GetUTF16Bytes();

            byte[] bytes = new byte[
                1
                + 1
                + 2 + 2 + questionBytes.Length + 2 + 2 + correctBytes.Length + 2 + 2 + nameBytes.Length
                + 1
                + 4
                + 2
                + 4 + 4 + 4
                ];
            int index = 0;

            bytes[index] = (byte)Type;
            index += 1;

            bytes[index] = (byte)Cate;
            index += 1;

            ((ushort)Question.Length).ToBytes(bytes.AsMemory(index));
            index += 2;
            ((ushort)questionBytes.Length).ToBytes(bytes.AsMemory(index));
            index += 2;
            questionBytes.CopyTo(bytes.AsSpan(index));
            index += questionBytes.Length;

            ((ushort)Correct.Length).ToBytes(bytes.AsMemory(index));
            index += 2;
            ((ushort)correctBytes.Length).ToBytes(bytes.AsMemory(index));
            index += 2;
            correctBytes.CopyTo(bytes.AsSpan(index));
            index += correctBytes.Length;

            ((ushort)UserName.Length).ToBytes(bytes.AsMemory(index));
            index += 2;
            ((ushort)nameBytes.Length).ToBytes(bytes.AsMemory(index));
            index += 2;
            nameBytes.CopyTo(bytes.AsSpan(index));
            index += nameBytes.Length;

            bytes[index] = (byte)(End ? 1 : 0);
            index += 1;


            Option.ToBytes(bytes.AsMemory(index));
            index += 4;
            Chance.ToBytes(bytes.AsMemory(index));
            index += 2;
            Join.ToBytes(bytes.AsMemory(index));
            index += 4;
            Right.ToBytes(bytes.AsMemory(index));
            index += 4;
            Wrong.ToBytes(bytes.AsMemory(index));
            index += 4;

            return bytes;
        }
        public static SnatchQuestionInfo DeBytes(ReadOnlyMemory<byte> data)
        {
            SnatchQuestionInfo snatchQuestionInfo = new SnatchQuestionInfo();

            var span = data.Span;
            int index = 0;
            snatchQuestionInfo.Type = (SnatchType)span[index];
            index += 1;

            snatchQuestionInfo.Cate = (SnatchCate)span[index];
            index += 1;

            int strLength = span.Slice(index).ToUInt16();
            index += 2;
            int byteLength = span.Slice(index).ToUInt16();
            index += 2;
            snatchQuestionInfo.Question = span.Slice(index, byteLength).GetUTF16String(strLength);
            index += byteLength;

            strLength = span.Slice(index).ToUInt16();
            index += 2;
            byteLength = span.Slice(index).ToUInt16();
            index += 2;
            snatchQuestionInfo.Correct = span.Slice(index, byteLength).GetUTF16String(strLength);
            index += byteLength;

            strLength = span.Slice(index).ToUInt16();
            index += 2;
            byteLength = span.Slice(index).ToUInt16();
            index += 2;
            snatchQuestionInfo.UserName = span.Slice(index, byteLength).GetUTF16String(strLength);
            index += byteLength;

            snatchQuestionInfo.End = span[index] == 1;
            index += 1;

            snatchQuestionInfo.Option = span.Slice(index).ToInt32();
            index += 4;
            snatchQuestionInfo.Chance = span.Slice(index).ToUInt16();
            index += 2;
            snatchQuestionInfo.Join = span.Slice(index).ToInt32();
            index += 4;
            snatchQuestionInfo.Right = span.Slice(index).ToInt32();
            index += 4;
            snatchQuestionInfo.Wrong = span.Slice(index).ToInt32();
            index += 4;

            return snatchQuestionInfo;
        }
    }
    public sealed class SnatchAnswerInfo
    {
        public string UserName { get; set; }
        public string MachineName { get; set; }
        public SnatchState State { get; set; }
        public bool Result { get; set; }
        public long Time { get; set; }
        public ushort Times { get; set; }
        public string ResultStr { get; set; }

        [JsonIgnore]
        public SnatchQuestionInfo Question { get; set; }

        public byte[] ToBytes()
        {
            ReadOnlySpan<byte> resultBytes = ResultStr.GetUTF16Bytes();
            ReadOnlySpan<byte> nameBytes = UserName.GetUTF16Bytes();

            byte[] bytes = new byte[
                1
                + 1
                + 8
                + 2
                + 2 + 2 + nameBytes.Length
                + 2 + 2 + resultBytes.Length
                ];
            int index = 0;

            bytes[index] = (byte)State;
            index += 1;


            bytes[index] = (byte)(Result ? 1 : 0);
            index += 1;

            Time.ToBytes(bytes.AsMemory(index));
            index += 8;
            Times.ToBytes(bytes.AsMemory(index));
            index += 2;

            ((ushort)UserName.Length).ToBytes(bytes.AsMemory(index));
            index += 2;
            ((ushort)nameBytes.Length).ToBytes(bytes.AsMemory(index));
            index += 2;
            nameBytes.CopyTo(bytes.AsSpan(index));
            index += nameBytes.Length;

            ((ushort)ResultStr.Length).ToBytes(bytes.AsMemory(index));
            index += 2;
            ((ushort)resultBytes.Length).ToBytes(bytes.AsMemory(index));
            index += 2;
            resultBytes.CopyTo(bytes.AsSpan(index));
            index += resultBytes.Length;

            return bytes;
        }
        public static SnatchAnswerInfo DeBytes(ReadOnlyMemory<byte> data)
        {
            SnatchAnswerInfo snatchAnswerInfo = new SnatchAnswerInfo();

            var span = data.Span;
            int index = 0;
            snatchAnswerInfo.State = (SnatchState)span[index];
            index += 1;
            snatchAnswerInfo.Result = span[index] == 1;
            index += 1;

            snatchAnswerInfo.Time = span.Slice(index).ToInt64();
            index += 8;

            snatchAnswerInfo.Times = span.Slice(index).ToUInt16();
            index += 2;

            int strLength = span.Slice(index).ToUInt16();
            index += 2;
            int byteLength = span.Slice(index).ToUInt16();
            index += 2;
            snatchAnswerInfo.UserName = span.Slice(index, byteLength).GetUTF16String(strLength);
            index += byteLength;

            strLength = span.Slice(index).ToUInt16();
            index += 2;
            byteLength = span.Slice(index).ToUInt16();
            index += 2;
            snatchAnswerInfo.ResultStr = span.Slice(index, byteLength).GetUTF16String(strLength);
            index += byteLength;



            return snatchAnswerInfo;
        }
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
    public enum SnatchCate : byte
    {
        None = 0,
        Question = 1,
        Vote = 2,
    }
}
