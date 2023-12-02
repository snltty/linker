using cmonitor.libs;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

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

        SnatchQuestionInfo snatchQuesionInfo;
        SnatchAnswerInfo snatchAnswerInfo;
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
            TopMost = true;
            InitializeComponent();

            this.shareQuestionIndex = shareQuestionIndex;
            this.shareAnswerIndex = shareAnswerIndex;
            this.shareMemory = new ShareMemory(shareMkey, shareMLength, shareItemMLength);

        }

        CheckBox[] options = Array.Empty<CheckBox>();
        private void OnLoad(object sender, EventArgs e)
        {
            options = new CheckBox[] { checkA, checkB, checkC, checkD, checkE, checkF };

            shareMemory.InitLocal();
            shareMemory.WriteRunning(shareQuestionIndex, true);
            shareMemory.WriteClosed(shareQuestionIndex, false);
            shareMemory.WriteRunning(shareAnswerIndex, true);
            shareMemory.WriteClosed(shareAnswerIndex, false);
            shareMemory.StateAction((index, state) =>
            {
                if (shareQuestionIndex == index)
                {
                    if ((state & ShareMemoryState.Updated) == ShareMemoryState.Updated)
                    {
                        ReadQuestionMemory();
                        CheckState();
                    }
                    if ((state & ShareMemoryState.Closed) == ShareMemoryState.Closed)
                    {
                        WriteAnswerConfirmIfState(SnatchState.Ask, false, string.Empty);
                    }
                }
                else if (shareAnswerIndex == index)
                {
                    if ((state & ShareMemoryState.Updated) == ShareMemoryState.Updated)
                    {
                        ReadAnswerMemory();
                        CheckState();
                    }
                }
            });
            shareMemory.Loop();
            ReadMemory();
            CheckState();
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
            if (snatchAnswerInfo.State == SnatchState.Confirm && snatchQuesionInfo.Repeat == false)
            {
                MessageBox.Show($"已答题，不可重复作答");
                return;
            }
            if (snatchAnswerInfo.Times > snatchQuesionInfo.Max)
            {
                MessageBox.Show($"超过重复答题次数上线");
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
                WriteAnswerConfirm(answer == snatchQuesionInfo.Correct, answer);

                loading = false;
                CheckState();
            });
        }
        private void CloseClear()
        {
            shareMemory.WriteRunning(shareQuestionIndex, false);
            shareMemory.WriteClosed(shareQuestionIndex, true);
            shareMemory.WriteRunning(shareAnswerIndex, false);
            shareMemory.WriteClosed(shareAnswerIndex, true);

            Application.ExitThread();
            Application.Exit();
            Process.GetCurrentProcess().Kill();
        }

        private void ReadQuestionMemory()
        {
            try
            {
                string result = shareMemory.GetItemValue(shareQuestionIndex);
                if (string.IsNullOrWhiteSpace(result))
                {
                    snatchQuesionInfo = new SnatchQuestionInfo { Question = "测试测试", Correct = "ABC", Option = 5, Type = SnatchType.Select };
                }
                else
                {
                    snatchQuesionInfo = JsonSerializer.Deserialize<SnatchQuestionInfo>(result);
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
                string result = shareMemory.GetItemValue(shareAnswerIndex);
                if (string.IsNullOrWhiteSpace(result))
                {
                    snatchAnswerInfo = new SnatchAnswerInfo { Result = false, ResultStr = "ABC", State = SnatchState.Ask, };
                }
                else
                {
                    snatchAnswerInfo = JsonSerializer.Deserialize<SnatchAnswerInfo>(result);
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
        private void WriteAnswerConfirmIfState(SnatchState state, bool result, string resultStr)
        {
            if (snatchAnswerInfo.State == state)
            {
                WriteAnswerConfirm(result, resultStr);
            }
        }
        private void WriteAnswerConfirm(bool result, string resultStr)
        {
            snatchAnswerInfo.State = SnatchState.Confirm;
            snatchAnswerInfo.Result = result;
            snatchAnswerInfo.ResultStr = resultStr;
            snatchAnswerInfo.Time = (long)(DateTime.UtcNow.Subtract(startTime)).TotalMilliseconds;
            snatchAnswerInfo.Times++;
            shareMemory.Update(shareAnswerIndex, key, JsonSerializer.Serialize(snatchAnswerInfo));
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
                EnableBtn(snatchAnswerInfo.Result ? "答题正确" : "答题错误");
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
            });
        }

        sealed class SnatchQuestionInfo
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
            /// 最多答题数
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
        sealed class SnatchAnswerInfo
        {
            public SnatchState State { get; set; }
            public bool Result { get; set; }
            public long Time { get; set; }
            public int Times { get; set; }
            public string ResultStr { get; set; }
        }
        enum SnatchState : byte
        {
            None = 0,
            Ask = 1,
            Confirm = 2,
        }
        enum SnatchType : byte
        {
            None = 0,
            Select = 1,
            Input = 2,
        }
    }
}
