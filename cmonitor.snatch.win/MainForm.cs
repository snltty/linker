using cmonitor.libs;
using common.libs.extends;
using System;
using System.Diagnostics;
using System.Text;

namespace cmonitor.snatch.win
{
    public partial class MainForm : Form
    {
        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_APPWINDOW = 0x40000;
                const int WS_EX_TOOLWINDOW = 0x80;
                CreateParams cp = base.CreateParams;
                cp.ExStyle &= (~WS_EX_APPWINDOW);
                cp.ExStyle |= WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        private SnatchQuestionInfo snatchQuesionInfo;
        private SnatchAnswerInfo snatchAnswerInfo;
        private readonly ShareMemory shareMemory;
        private int shareQuestionIndex;
        private int shareAnswerIndex;
        private string key = "SnatchAnswer";
        private DateTime startTime = new DateTime(1970, 1, 1);

        public MainForm(string shareMkey, int shareMLength, int shareItemMLength, int shareQuestionIndex, int shareAnswerIndex)
        {
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            ShowInTaskbar = false;

            InitializeComponent();

            this.shareQuestionIndex = shareQuestionIndex;
            this.shareAnswerIndex = shareAnswerIndex;
            this.shareMemory = new ShareMemory(shareMkey, shareMLength, shareItemMLength);

        }

        private CheckBox[] options = Array.Empty<CheckBox>();
        private void OnLoad(object sender, EventArgs e)
        {
            TopMost = true;

            options = new CheckBox[] { checkA, checkB, checkC, checkD, checkE, checkF };
            for (int i = 0; i < options.Length; i++)
            {
                options[i].Click += CheckedChanged;
            }


            shareMemory.InitLocal();

            shareMemory.AddAttribute(shareQuestionIndex, ShareMemoryAttribute.Running);
            shareMemory.RemoveAttribute(shareQuestionIndex, ShareMemoryAttribute.Closed);
            shareMemory.AddAttribute(shareAnswerIndex, ShareMemoryAttribute.Running);
            shareMemory.RemoveAttribute(shareAnswerIndex, ShareMemoryAttribute.Closed);
            shareMemory.AddAttributeAction(shareQuestionIndex, QuestionAttributeChange);
            shareMemory.AddAttributeAction(shareAnswerIndex, AnswerAttributeChange);
            shareMemory.StartLoop();
            ReadMemory();
            CheckState();
        }

        private void CheckedChanged(object sender, EventArgs e)
        {
            CheckBox box = sender as CheckBox;
            if (snatchQuesionInfo.Correct.Length <= 1 && box.Checked)
            {
                for (int i = 0; i < options.Length; i++)
                {
                    options[i].Checked = false;
                }
                box.Checked = true;
            }
        }

        private void QuestionAttributeChange(ShareMemoryAttribute attr)
        {
            if ((attr & ShareMemoryAttribute.Updated) == ShareMemoryAttribute.Updated)
            {
                ReadQuestionMemory();
                CheckState();
            }
            if ((attr & ShareMemoryAttribute.Closed) == ShareMemoryAttribute.Closed)
            {
                WriteAnswerConfirmIfState(SnatchState.Ask, string.Empty);
                CloseClear();
            }
        }
        private void AnswerAttributeChange(ShareMemoryAttribute attr)
        {
            if ((attr & ShareMemoryAttribute.Updated) == ShareMemoryAttribute.Updated)
            {
                ReadAnswerMemory();
                CheckState();
            }
        }

        private string GetAnswer()
        {
            if (snatchQuesionInfo.Type == SnatchType.Select)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < snatchQuesionInfo.Option; i++)
                {
                    if (options[i].Checked)
                        sb.Append((char)(i + 65));
                }
                return sb.ToString();
            }
            else if (snatchQuesionInfo.Type == SnatchType.Input)
            {
                return inputAnswer.Text.Trim();
            }
            return string.Empty;
        }
        private void OnConfirm(object sender, EventArgs e)
        {
            if (snatchQuesionInfo.End)
            {
                MessageBox.Show($"答题已结束");
                return;
            }
            if (snatchAnswerInfo.Times >= snatchQuesionInfo.Chance)
            {
                MessageBox.Show($"超过重复答题机会");
                return;
            }

            string answer = GetAnswer();
            if (string.IsNullOrWhiteSpace(answer))
            {
                MessageBox.Show($"请给出你的答案");
                return;
            }

            loading = true;
            Task.Run(() =>
            {
                CheckBtn();
                WriteAnswerConfirm(answer);

                loading = false;
                CheckState();
            });
        }
        private void CloseClear()
        {
            shareMemory.RemoveAttribute(shareQuestionIndex, ShareMemoryAttribute.Running);
            shareMemory.RemoveAttribute(shareQuestionIndex, ShareMemoryAttribute.Closed);
            shareMemory.RemoveAttribute(shareAnswerIndex, ShareMemoryAttribute.Running);
            shareMemory.RemoveAttribute(shareAnswerIndex, ShareMemoryAttribute.Closed);

            Application.ExitThread();
            Application.Exit();
            Process.GetCurrentProcess().Kill();
        }

        private void ReadQuestionMemory()
        {
            try
            {
                byte[] result = shareMemory.ReadValueArray(shareQuestionIndex);
                if (result.Length == 0)
                {
                    snatchQuesionInfo = new SnatchQuestionInfo { Chance = 65535, Name = "snltty", Question = "测试测试", Correct = "A", Option = 5, Cate = SnatchCate.Question, Type = SnatchType.Select };
                }
                else
                {
                    snatchQuesionInfo = SnatchQuestionInfo.DeBytes(result);
                }
            }
            catch (Exception)
            {
                CloseClear();
            }
        }
        private void ReadAnswerMemory()
        {
            try
            {
                byte[] result = shareMemory.ReadValueArray(shareAnswerIndex);
                if (result.Length == 0)
                {
                    snatchAnswerInfo = new SnatchAnswerInfo { Name = "snltty", Result = false, ResultStr = "ABC", State = SnatchState.Ask, };
                }
                else
                {
                    snatchAnswerInfo = SnatchAnswerInfo.DeBytes(result);
                }
            }
            catch (Exception)
            {
                CloseClear();
            }
        }
        private void ReadMemory()
        {
            ReadQuestionMemory();
            ReadAnswerMemory();
        }
        private void WriteAnswerConfirmIfState(SnatchState state, string resultStr)
        {
            if (snatchAnswerInfo.State == state)
            {
                WriteAnswerConfirm(resultStr);
            }
        }
        private void WriteAnswerConfirm(string resultStr)
        {
            snatchAnswerInfo.State = SnatchState.Confirm;
            snatchAnswerInfo.Result = snatchQuesionInfo.Cate == SnatchCate.Vote || resultStr == snatchQuesionInfo.Correct;
            snatchAnswerInfo.ResultStr = resultStr;
            snatchAnswerInfo.Time = (long)(DateTime.UtcNow.Subtract(startTime)).TotalMilliseconds;
            snatchAnswerInfo.Times++;
            shareMemory.Update(shareAnswerIndex, Encoding.UTF8.GetBytes(key), snatchAnswerInfo.ToBytes());
        }


        bool loading = false;
        private void EnableBtn(string text)
        {
            this.Invoke(() =>
            {
                btnConfirm.Text = text;
                btnConfirm.Enabled = true;
            });
        }
        private void DisableBtn(string text)
        {
            this.Invoke(() =>
            {
                btnConfirm.Text = text;
                btnConfirm.Enabled = false;
            });
        }

        private void CheckOptions()
        {
            for (int i = 0; i < options.Length; i++)
            {
                options[i].Visible = i < snatchQuesionInfo.Option;
                options[i].Checked = false;
            }
            if (string.IsNullOrWhiteSpace(snatchAnswerInfo.ResultStr) == false && snatchAnswerInfo.ResultStr.Length <= snatchQuesionInfo.Option)
            {
                for (int i = 0; i < snatchAnswerInfo.ResultStr.Length; i++)
                {
                    char option = snatchAnswerInfo.ResultStr[i];
                    if (option >= 'A' && option <= 'Z' && option - 'A' < options.Length)
                    {
                        options[(option - 'A')].Checked = true;
                    }
                }
            }
            if(snatchAnswerInfo.Times >= snatchQuesionInfo.Chance)
            {
                for (int i = 0; i < options.Length; i++)
                {
                    options[i].Enabled = false;
                }
            }
        }
        private void CheckBtn()
        {
            if (loading)
            {
                DisableBtn("操作中..");
            }
            else if (snatchAnswerInfo.State == SnatchState.Confirm)
            {
                btnConfirm.ForeColor = (snatchAnswerInfo.Result ? Color.Green : Color.Red);
                if (snatchQuesionInfo.Cate == SnatchCate.Question)
                {
                    EnableBtn(snatchAnswerInfo.Result ? "答题正确" : "答题错误");
                }
                else
                {
                    EnableBtn("已投票");
                }
            }
            else if (snatchQuesionInfo.End)
            {
                EnableBtn("已结束");
            }
            else
            {
                EnableBtn("确定提交");
            }
        }
        private void CheckState()
        {
            this.Invoke(() =>
            {
                CheckOptions();

                CheckBtn();

                groupTypeSelect.Visible = false;
                groupTypeInput.Visible = false;
                if (snatchQuesionInfo.Type == SnatchType.Select)
                {
                    groupTypeSelect.Visible = true;
                }
                else if (snatchQuesionInfo.Type == SnatchType.Input)
                {
                    groupTypeInput.Visible = true;
                }

                inputQuestion.Text = snatchQuesionInfo.Question;


                inputJoin.Text = snatchQuesionInfo.Join.ToString();
                inputRight.Text = snatchQuesionInfo.Right.ToString();
                inputWrong.Text = snatchQuesionInfo.Wrong.ToString();

                labelRight.Text = snatchQuesionInfo.Cate == SnatchCate.Question ? "正确" : "已投";
                labelWrong.Text = snatchQuesionInfo.Cate == SnatchCate.Question ? "错误" : "未投";
                groupResult.Text = snatchQuesionInfo.Cate == SnatchCate.Question ? "答题结果" : "投票结果";
                this.Text = snatchQuesionInfo.Cate == SnatchCate.Question ? "互动答题" : "互动投票";

                inputChance.Text = (snatchQuesionInfo.Chance - snatchAnswerInfo.Times).ToString();
            });
        }

        public sealed partial class SnatchQuestionInfo
        {
            public string Name { get; set; }
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
            public bool End { get; set; } = false;
            /// <summary>
            /// 选项数
            /// </summary>
            public int Option { get; set; }
            /// <summary>
            /// 最多答题次数
            /// </summary>
            public ushort Chance { get; set; } = ushort.MaxValue;

            public int Join { get; set; } = 0;
            public int Right { get; set; } = 0;
            public int Wrong { get; set; } = 0;

            public byte[] ToBytes()
            {
                ReadOnlySpan<byte> questionBytes = Question.GetUTF16Bytes();
                ReadOnlySpan<byte> correctBytes = Correct.GetUTF16Bytes();
                ReadOnlySpan<byte> nameBytes = Name.GetUTF16Bytes();

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

                ((ushort)Name.Length).ToBytes(bytes.AsMemory(index));
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
            public static SnatchQuestionInfo DeBytes(Memory<byte> data)
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
                snatchQuestionInfo.Name = span.Slice(index, byteLength).GetUTF16String(strLength);
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
            public string Name { get; set; }
            public SnatchState State { get; set; }
            public bool Result { get; set; }
            public long Time { get; set; }
            public ushort Times { get; set; }
            public string ResultStr { get; set; }

            public byte[] ToBytes()
            {
                ReadOnlySpan<byte> resultBytes = ResultStr.GetUTF16Bytes();
                ReadOnlySpan<byte> nameBytes = Name.GetUTF16Bytes();

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

                ((ushort)Name.Length).ToBytes(bytes.AsMemory(index));
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
            public static SnatchAnswerInfo DeBytes(Memory<byte> data)
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
                snatchAnswerInfo.Name = span.Slice(index, byteLength).GetUTF16String(strLength);
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
}
