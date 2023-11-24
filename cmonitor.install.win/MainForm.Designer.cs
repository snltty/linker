namespace cmonitor.install.win
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
            serverIP = new TextBox();
            label1 = new Label();
            label2 = new Label();
            serverPort = new TextBox();
            label3 = new Label();
            webPort = new TextBox();
            label4 = new Label();
            apiPort = new TextBox();
            modeClient = new CheckBox();
            modeServer = new CheckBox();
            label5 = new Label();
            shareLen = new TextBox();
            label6 = new Label();
            shareKey = new TextBox();
            label7 = new Label();
            wallpaperIndex = new TextBox();
            label8 = new Label();
            keyboardIndex = new TextBox();
            label9 = new Label();
            llockIndex = new TextBox();
            label10 = new Label();
            sasIndex = new TextBox();
            label11 = new Label();
            machineName = new TextBox();
            label12 = new Label();
            screenDelay = new TextBox();
            label13 = new Label();
            reportDelay = new TextBox();
            label14 = new Label();
            screenScale = new TextBox();
            installBtn = new Button();
            label15 = new Label();
            runBtn = new Button();
            checkStateBtn = new Button();
            label16 = new Label();
            shareItemLen = new TextBox();
            SuspendLayout();
            // 
            // serverIP
            // 
            serverIP.Location = new Point(106, 76);
            serverIP.Name = "serverIP";
            serverIP.Size = new Size(100, 23);
            serverIP.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(32, 80);
            label1.Name = "label1";
            label1.Size = new Size(55, 17);
            label1.TabIndex = 1;
            label1.Text = "服务端IP";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(223, 80);
            label2.Name = "label2";
            label2.Size = new Size(68, 17);
            label2.TabIndex = 3;
            label2.Text = "服务端端口";
            // 
            // serverPort
            // 
            serverPort.Location = new Point(294, 76);
            serverPort.Name = "serverPort";
            serverPort.Size = new Size(100, 23);
            serverPort.TabIndex = 2;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(232, 106);
            label3.Name = "label3";
            label3.Size = new Size(56, 17);
            label3.TabIndex = 7;
            label3.Text = "web端口";
            // 
            // webPort
            // 
            webPort.Location = new Point(294, 102);
            webPort.Name = "webPort";
            webPort.Size = new Size(100, 23);
            webPort.TabIndex = 6;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(23, 106);
            label4.Name = "label4";
            label4.Size = new Size(80, 17);
            label4.TabIndex = 5;
            label4.Text = "管理接口端口";
            // 
            // apiPort
            // 
            apiPort.Location = new Point(106, 102);
            apiPort.Name = "apiPort";
            apiPort.Size = new Size(100, 23);
            apiPort.TabIndex = 4;
            // 
            // modeClient
            // 
            modeClient.AutoSize = true;
            modeClient.Location = new Point(156, 21);
            modeClient.Name = "modeClient";
            modeClient.Size = new Size(63, 21);
            modeClient.TabIndex = 8;
            modeClient.Text = "客户端";
            modeClient.UseVisualStyleBackColor = true;
            // 
            // modeServer
            // 
            modeServer.AutoSize = true;
            modeServer.Location = new Point(219, 22);
            modeServer.Name = "modeServer";
            modeServer.Size = new Size(63, 21);
            modeServer.TabIndex = 9;
            modeServer.Text = "服务端";
            modeServer.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(41, 253);
            label5.Name = "label5";
            label5.Size = new Size(56, 17);
            label5.TabIndex = 14;
            label5.Text = "数据数量";
            // 
            // shareLen
            // 
            shareLen.Location = new Point(106, 249);
            shareLen.Name = "shareLen";
            shareLen.Size = new Size(100, 23);
            shareLen.TabIndex = 13;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(27, 226);
            label6.Name = "label6";
            label6.Size = new Size(68, 17);
            label6.TabIndex = 12;
            label6.Text = "共享数据键";
            // 
            // shareKey
            // 
            shareKey.Location = new Point(106, 222);
            shareKey.Name = "shareKey";
            shareKey.Size = new Size(100, 23);
            shareKey.TabIndex = 11;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(225, 280);
            label7.Name = "label7";
            label7.Size = new Size(68, 17);
            label7.TabIndex = 18;
            label7.Text = "壁纸键下标";
            // 
            // wallpaperIndex
            // 
            wallpaperIndex.Location = new Point(294, 276);
            wallpaperIndex.Name = "wallpaperIndex";
            wallpaperIndex.Size = new Size(100, 23);
            wallpaperIndex.TabIndex = 17;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(27, 280);
            label8.Name = "label8";
            label8.Size = new Size(68, 17);
            label8.TabIndex = 16;
            label8.Text = "键盘键下标";
            // 
            // keyboardIndex
            // 
            keyboardIndex.Location = new Point(106, 276);
            keyboardIndex.Name = "keyboardIndex";
            keyboardIndex.Size = new Size(100, 23);
            keyboardIndex.TabIndex = 15;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(27, 307);
            label9.Name = "label9";
            label9.Size = new Size(68, 17);
            label9.TabIndex = 20;
            label9.Text = "锁屏键下标";
            // 
            // llockIndex
            // 
            llockIndex.Location = new Point(106, 303);
            llockIndex.Name = "llockIndex";
            llockIndex.Size = new Size(100, 23);
            llockIndex.TabIndex = 19;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(227, 306);
            label10.Name = "label10";
            label10.Size = new Size(66, 17);
            label10.TabIndex = 22;
            label10.Text = "SAS键下标";
            // 
            // sasIndex
            // 
            sasIndex.Location = new Point(294, 302);
            sasIndex.Name = "sasIndex";
            sasIndex.Size = new Size(100, 23);
            sasIndex.TabIndex = 21;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(36, 53);
            label11.Name = "label11";
            label11.Size = new Size(44, 17);
            label11.TabIndex = 24;
            label11.Text = "机器名";
            // 
            // machineName
            // 
            machineName.Location = new Point(106, 49);
            machineName.Name = "machineName";
            machineName.Size = new Size(100, 23);
            machineName.TabIndex = 23;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(220, 154);
            label12.Name = "label12";
            label12.Size = new Size(73, 17);
            label12.TabIndex = 28;
            label12.Text = "截屏间隔ms";
            // 
            // screenDelay
            // 
            screenDelay.Location = new Point(294, 150);
            screenDelay.Name = "screenDelay";
            screenDelay.Size = new Size(100, 23);
            screenDelay.TabIndex = 27;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new Point(27, 154);
            label13.Name = "label13";
            label13.Size = new Size(73, 17);
            label13.TabIndex = 26;
            label13.Text = "报告间隔ms";
            // 
            // reportDelay
            // 
            reportDelay.Location = new Point(106, 150);
            reportDelay.Name = "reportDelay";
            reportDelay.Size = new Size(100, 23);
            reportDelay.TabIndex = 25;
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new Point(20, 181);
            label14.Name = "label14";
            label14.Size = new Size(80, 17);
            label14.TabIndex = 30;
            label14.Text = "截屏缩放比例";
            // 
            // screenScale
            // 
            screenScale.Location = new Point(106, 177);
            screenScale.Name = "screenScale";
            screenScale.Size = new Size(100, 23);
            screenScale.TabIndex = 29;
            // 
            // installBtn
            // 
            installBtn.Location = new Point(201, 372);
            installBtn.Name = "installBtn";
            installBtn.Size = new Size(81, 35);
            installBtn.TabIndex = 31;
            installBtn.Text = "安装自启动";
            installBtn.UseVisualStyleBackColor = true;
            installBtn.Click += OnInstallClick;
            // 
            // label15
            // 
            label15.Location = new Point(22, 337);
            label15.Name = "label15";
            label15.Size = new Size(372, 23);
            label15.TabIndex = 32;
            label15.Text = "每项255长度，0项保留不可用";
            label15.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // runBtn
            // 
            runBtn.Location = new Point(114, 372);
            runBtn.Name = "runBtn";
            runBtn.Size = new Size(81, 35);
            runBtn.TabIndex = 33;
            runBtn.Text = "停止运行";
            runBtn.UseVisualStyleBackColor = true;
            runBtn.Click += RunClick;
            // 
            // checkStateBtn
            // 
            checkStateBtn.Location = new Point(331, 378);
            checkStateBtn.Name = "checkStateBtn";
            checkStateBtn.Size = new Size(75, 23);
            checkStateBtn.TabIndex = 34;
            checkStateBtn.Text = "检查状态";
            checkStateBtn.UseVisualStyleBackColor = true;
            checkStateBtn.Click += checkStateBtn_Click;
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Location = new Point(211, 253);
            label16.Name = "label16";
            label16.Size = new Size(80, 17);
            label16.TabIndex = 36;
            label16.Text = "每项数据长度";
            // 
            // shareItemLen
            // 
            shareItemLen.Location = new Point(294, 249);
            shareItemLen.Name = "shareItemLen";
            shareItemLen.Size = new Size(100, 23);
            shareItemLen.TabIndex = 35;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(418, 420);
            Controls.Add(label16);
            Controls.Add(shareItemLen);
            Controls.Add(checkStateBtn);
            Controls.Add(runBtn);
            Controls.Add(label15);
            Controls.Add(installBtn);
            Controls.Add(label14);
            Controls.Add(screenScale);
            Controls.Add(label12);
            Controls.Add(screenDelay);
            Controls.Add(label13);
            Controls.Add(reportDelay);
            Controls.Add(label11);
            Controls.Add(machineName);
            Controls.Add(label10);
            Controls.Add(sasIndex);
            Controls.Add(label9);
            Controls.Add(llockIndex);
            Controls.Add(label7);
            Controls.Add(wallpaperIndex);
            Controls.Add(label8);
            Controls.Add(keyboardIndex);
            Controls.Add(label5);
            Controls.Add(shareLen);
            Controls.Add(label6);
            Controls.Add(shareKey);
            Controls.Add(modeServer);
            Controls.Add(modeClient);
            Controls.Add(label3);
            Controls.Add(webPort);
            Controls.Add(label4);
            Controls.Add(apiPort);
            Controls.Add(label2);
            Controls.Add(serverPort);
            Controls.Add(label1);
            Controls.Add(serverIP);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            Text = "cmonitor安装工具";
            Load += OnLoad;
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.TextBox serverIP;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox serverPort;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox webPort;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox apiPort;
        private System.Windows.Forms.CheckBox modeClient;
        private System.Windows.Forms.CheckBox modeServer;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox shareLen;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox shareKey;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox wallpaperIndex;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox keyboardIndex;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox llockIndex;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox sasIndex;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox machineName;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox screenDelay;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox reportDelay;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox screenScale;
        private System.Windows.Forms.Button installBtn;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Button runBtn;
        private System.Windows.Forms.Button checkStateBtn;

        #endregion

        private Label label16;
        private TextBox shareItemLen;
    }
}
