namespace cmonitor.snatch.win
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            groupTypeQuestion = new GroupBox();
            groupResult = new GroupBox();
            inputChance = new TextBox();
            label4 = new Label();
            inputWrong = new TextBox();
            labelWrong = new Label();
            inputRight = new TextBox();
            labelRight = new Label();
            inputJoin = new TextBox();
            label1 = new Label();
            groupTypeInput = new GroupBox();
            inputAnswer = new TextBox();
            groupTypeSelect = new GroupBox();
            checkB = new CheckBox();
            checkF = new CheckBox();
            checkA = new CheckBox();
            checkC = new CheckBox();
            checkE = new CheckBox();
            checkD = new CheckBox();
            btnConfirm = new Button();
            panel1 = new Panel();
            inputQuestion = new TextBox();
            groupTypeQuestion.SuspendLayout();
            groupResult.SuspendLayout();
            groupTypeInput.SuspendLayout();
            groupTypeSelect.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // groupTypeQuestion
            // 
            groupTypeQuestion.Controls.Add(groupResult);
            groupTypeQuestion.Controls.Add(groupTypeInput);
            groupTypeQuestion.Controls.Add(groupTypeSelect);
            groupTypeQuestion.Controls.Add(btnConfirm);
            groupTypeQuestion.Controls.Add(panel1);
            groupTypeQuestion.Location = new Point(12, 3);
            groupTypeQuestion.Name = "groupTypeQuestion";
            groupTypeQuestion.Size = new Size(350, 433);
            groupTypeQuestion.TabIndex = 1;
            groupTypeQuestion.TabStop = false;
            groupTypeQuestion.Text = "看题";
            // 
            // groupResult
            // 
            groupResult.Controls.Add(inputChance);
            groupResult.Controls.Add(label4);
            groupResult.Controls.Add(inputWrong);
            groupResult.Controls.Add(labelWrong);
            groupResult.Controls.Add(inputRight);
            groupResult.Controls.Add(labelRight);
            groupResult.Controls.Add(inputJoin);
            groupResult.Controls.Add(label1);
            groupResult.Location = new Point(15, 252);
            groupResult.Name = "groupResult";
            groupResult.Size = new Size(320, 59);
            groupResult.TabIndex = 12;
            groupResult.TabStop = false;
            groupResult.Text = "答题结果";
            // 
            // inputChance
            // 
            inputChance.ForeColor = Color.Red;
            inputChance.Location = new Point(265, 24);
            inputChance.Name = "inputChance";
            inputChance.ReadOnly = true;
            inputChance.Size = new Size(45, 23);
            inputChance.TabIndex = 7;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.ForeColor = Color.Black;
            label4.Location = new Point(231, 27);
            label4.Name = "label4";
            label4.Size = new Size(32, 17);
            label4.TabIndex = 6;
            label4.Text = "机会";
            // 
            // inputWrong
            // 
            inputWrong.ForeColor = Color.Red;
            inputWrong.Location = new Point(193, 24);
            inputWrong.Name = "inputWrong";
            inputWrong.ReadOnly = true;
            inputWrong.Size = new Size(35, 23);
            inputWrong.TabIndex = 5;
            // 
            // labelWrong
            // 
            labelWrong.AutoSize = true;
            labelWrong.ForeColor = Color.Red;
            labelWrong.Location = new Point(159, 27);
            labelWrong.Name = "labelWrong";
            labelWrong.Size = new Size(32, 17);
            labelWrong.TabIndex = 4;
            labelWrong.Text = "错误";
            // 
            // inputRight
            // 
            inputRight.ForeColor = Color.Green;
            inputRight.Location = new Point(117, 24);
            inputRight.Name = "inputRight";
            inputRight.ReadOnly = true;
            inputRight.Size = new Size(38, 23);
            inputRight.TabIndex = 3;
            // 
            // labelRight
            // 
            labelRight.AutoSize = true;
            labelRight.ForeColor = Color.Green;
            labelRight.Location = new Point(81, 27);
            labelRight.Name = "labelRight";
            labelRight.Size = new Size(32, 17);
            labelRight.TabIndex = 2;
            labelRight.Text = "正确";
            // 
            // inputJoin
            // 
            inputJoin.Location = new Point(39, 24);
            inputJoin.Name = "inputJoin";
            inputJoin.ReadOnly = true;
            inputJoin.Size = new Size(39, 23);
            inputJoin.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(4, 27);
            label1.Name = "label1";
            label1.Size = new Size(32, 17);
            label1.TabIndex = 0;
            label1.Text = "参与";
            // 
            // groupTypeInput
            // 
            groupTypeInput.Controls.Add(inputAnswer);
            groupTypeInput.Location = new Point(15, 320);
            groupTypeInput.Name = "groupTypeInput";
            groupTypeInput.Size = new Size(320, 56);
            groupTypeInput.TabIndex = 11;
            groupTypeInput.TabStop = false;
            groupTypeInput.Text = "输入你的答案";
            // 
            // inputAnswer
            // 
            inputAnswer.Location = new Point(12, 22);
            inputAnswer.Name = "inputAnswer";
            inputAnswer.Size = new Size(298, 23);
            inputAnswer.TabIndex = 0;
            // 
            // groupTypeSelect
            // 
            groupTypeSelect.Controls.Add(checkB);
            groupTypeSelect.Controls.Add(checkF);
            groupTypeSelect.Controls.Add(checkA);
            groupTypeSelect.Controls.Add(checkC);
            groupTypeSelect.Controls.Add(checkE);
            groupTypeSelect.Controls.Add(checkD);
            groupTypeSelect.Location = new Point(15, 317);
            groupTypeSelect.Name = "groupTypeSelect";
            groupTypeSelect.Size = new Size(320, 56);
            groupTypeSelect.TabIndex = 10;
            groupTypeSelect.TabStop = false;
            groupTypeSelect.Text = "选择你的答案";
            // 
            // checkB
            // 
            checkB.AutoSize = true;
            checkB.Location = new Point(65, 25);
            checkB.Name = "checkB";
            checkB.Size = new Size(35, 21);
            checkB.TabIndex = 5;
            checkB.Text = "B";
            checkB.UseVisualStyleBackColor = true;
            // 
            // checkF
            // 
            checkF.AutoSize = true;
            checkF.Location = new Point(277, 25);
            checkF.Name = "checkF";
            checkF.Size = new Size(33, 21);
            checkF.TabIndex = 9;
            checkF.Text = "F";
            checkF.UseVisualStyleBackColor = true;
            // 
            // checkA
            // 
            checkA.AutoSize = true;
            checkA.Location = new Point(12, 25);
            checkA.Name = "checkA";
            checkA.Size = new Size(35, 21);
            checkA.TabIndex = 4;
            checkA.Text = "A";
            checkA.UseVisualStyleBackColor = true;
            // 
            // checkC
            // 
            checkC.AutoSize = true;
            checkC.Location = new Point(118, 25);
            checkC.Name = "checkC";
            checkC.Size = new Size(35, 21);
            checkC.TabIndex = 8;
            checkC.Text = "C";
            checkC.UseVisualStyleBackColor = true;
            // 
            // checkE
            // 
            checkE.AutoSize = true;
            checkE.Location = new Point(225, 25);
            checkE.Name = "checkE";
            checkE.Size = new Size(34, 21);
            checkE.TabIndex = 6;
            checkE.Text = "E";
            checkE.UseVisualStyleBackColor = true;
            // 
            // checkD
            // 
            checkD.AutoSize = true;
            checkD.Location = new Point(170, 25);
            checkD.Name = "checkD";
            checkD.Size = new Size(36, 21);
            checkD.TabIndex = 7;
            checkD.Text = "D";
            checkD.UseVisualStyleBackColor = true;
            // 
            // btnConfirm
            // 
            btnConfirm.Location = new Point(125, 387);
            btnConfirm.Name = "btnConfirm";
            btnConfirm.Size = new Size(99, 32);
            btnConfirm.TabIndex = 2;
            btnConfirm.Text = "确定提交";
            btnConfirm.UseVisualStyleBackColor = true;
            btnConfirm.Click += OnConfirm;
            // 
            // panel1
            // 
            panel1.Controls.Add(inputQuestion);
            panel1.Location = new Point(15, 28);
            panel1.Name = "panel1";
            panel1.Size = new Size(320, 218);
            panel1.TabIndex = 3;
            // 
            // inputQuestion
            // 
            inputQuestion.Dock = DockStyle.Fill;
            inputQuestion.Location = new Point(0, 0);
            inputQuestion.Multiline = true;
            inputQuestion.Name = "inputQuestion";
            inputQuestion.ReadOnly = true;
            inputQuestion.Size = new Size(320, 218);
            inputQuestion.TabIndex = 0;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(374, 448);
            Controls.Add(groupTypeQuestion);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            Text = "互动答题";
            Load += OnLoad;
            groupTypeQuestion.ResumeLayout(false);
            groupResult.ResumeLayout(false);
            groupResult.PerformLayout();
            groupTypeInput.ResumeLayout(false);
            groupTypeInput.PerformLayout();
            groupTypeSelect.ResumeLayout(false);
            groupTypeSelect.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private GroupBox groupTypeQuestion;
        private Button btnConfirm;
        private Panel panel1;
        private TextBox inputQuestion;
        private CheckBox checkA;
        private CheckBox checkB;
        private CheckBox checkC;
        private CheckBox checkD;
        private CheckBox checkE;
        private CheckBox checkF;
        private GroupBox groupTypeSelect;
        private GroupBox groupTypeInput;
        private TextBox inputAnswer;
        private GroupBox groupResult;
        private Label label1;
        private TextBox inputWrong;
        private Label labelWrong;
        private TextBox inputRight;
        private Label labelRight;
        private TextBox inputJoin;
        private TextBox inputChance;
        private Label label4;
    }
}
