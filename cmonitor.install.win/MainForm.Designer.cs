namespace cmonitor.install.win
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.serverIP = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.serverPort = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.webPort = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.apiPort = new System.Windows.Forms.TextBox();
            this.modeClient = new System.Windows.Forms.CheckBox();
            this.modeServer = new System.Windows.Forms.CheckBox();
            this.sasService = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.shareLen = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.shareKey = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.wallpaperIndex = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.keyboardIndex = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.llockIndex = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.sasIndex = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.machineName = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.screenDelay = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.reportDelay = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.screenScale = new System.Windows.Forms.TextBox();
            this.installBtn = new System.Windows.Forms.Button();
            this.label15 = new System.Windows.Forms.Label();
            this.runBtn = new System.Windows.Forms.Button();
            this.sasStart = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // serverIP
            // 
            this.serverIP.Location = new System.Drawing.Point(106, 76);
            this.serverIP.Name = "serverIP";
            this.serverIP.Size = new System.Drawing.Size(100, 21);
            this.serverIP.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(32, 80);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "服务端IP";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(223, 80);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 12);
            this.label2.TabIndex = 3;
            this.label2.Text = "服务端端口";
            // 
            // serverPort
            // 
            this.serverPort.Location = new System.Drawing.Point(294, 76);
            this.serverPort.Name = "serverPort";
            this.serverPort.Size = new System.Drawing.Size(100, 21);
            this.serverPort.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(232, 106);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(47, 12);
            this.label3.TabIndex = 7;
            this.label3.Text = "web端口";
            // 
            // webPort
            // 
            this.webPort.Location = new System.Drawing.Point(294, 102);
            this.webPort.Name = "webPort";
            this.webPort.Size = new System.Drawing.Size(100, 21);
            this.webPort.TabIndex = 6;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(23, 106);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(77, 12);
            this.label4.TabIndex = 5;
            this.label4.Text = "管理接口端口";
            // 
            // apiPort
            // 
            this.apiPort.Location = new System.Drawing.Point(106, 102);
            this.apiPort.Name = "apiPort";
            this.apiPort.Size = new System.Drawing.Size(100, 21);
            this.apiPort.TabIndex = 4;
            // 
            // modeClient
            // 
            this.modeClient.AutoSize = true;
            this.modeClient.Location = new System.Drawing.Point(157, 23);
            this.modeClient.Name = "modeClient";
            this.modeClient.Size = new System.Drawing.Size(60, 16);
            this.modeClient.TabIndex = 8;
            this.modeClient.Text = "客户端";
            this.modeClient.UseVisualStyleBackColor = true;
            // 
            // modeServer
            // 
            this.modeServer.AutoSize = true;
            this.modeServer.Location = new System.Drawing.Point(219, 22);
            this.modeServer.Name = "modeServer";
            this.modeServer.Size = new System.Drawing.Size(60, 16);
            this.modeServer.TabIndex = 9;
            this.modeServer.Text = "服务端";
            this.modeServer.UseVisualStyleBackColor = true;
            // 
            // sasService
            // 
            this.sasService.AutoSize = true;
            this.sasService.Location = new System.Drawing.Point(261, 53);
            this.sasService.Name = "sasService";
            this.sasService.Size = new System.Drawing.Size(66, 16);
            this.sasService.TabIndex = 10;
            this.sasService.Text = "SAS服务";
            this.sasService.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(229, 226);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(53, 12);
            this.label5.TabIndex = 14;
            this.label5.Text = "数据数量";
            // 
            // shareLen
            // 
            this.shareLen.Location = new System.Drawing.Point(294, 222);
            this.shareLen.Name = "shareLen";
            this.shareLen.Size = new System.Drawing.Size(100, 21);
            this.shareLen.TabIndex = 13;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(27, 226);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(65, 12);
            this.label6.TabIndex = 12;
            this.label6.Text = "共享数据键";
            // 
            // shareKey
            // 
            this.shareKey.Location = new System.Drawing.Point(106, 222);
            this.shareKey.Name = "shareKey";
            this.shareKey.Size = new System.Drawing.Size(100, 21);
            this.shareKey.TabIndex = 11;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(225, 253);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(65, 12);
            this.label7.TabIndex = 18;
            this.label7.Text = "壁纸键下标";
            // 
            // wallpaperIndex
            // 
            this.wallpaperIndex.Location = new System.Drawing.Point(294, 249);
            this.wallpaperIndex.Name = "wallpaperIndex";
            this.wallpaperIndex.Size = new System.Drawing.Size(100, 21);
            this.wallpaperIndex.TabIndex = 17;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(27, 253);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(65, 12);
            this.label8.TabIndex = 16;
            this.label8.Text = "键盘键下标";
            // 
            // keyboardIndex
            // 
            this.keyboardIndex.Location = new System.Drawing.Point(106, 249);
            this.keyboardIndex.Name = "keyboardIndex";
            this.keyboardIndex.Size = new System.Drawing.Size(100, 21);
            this.keyboardIndex.TabIndex = 15;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(27, 280);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(65, 12);
            this.label9.TabIndex = 20;
            this.label9.Text = "锁屏键下标";
            // 
            // llockIndex
            // 
            this.llockIndex.Location = new System.Drawing.Point(106, 276);
            this.llockIndex.Name = "llockIndex";
            this.llockIndex.Size = new System.Drawing.Size(100, 21);
            this.llockIndex.TabIndex = 19;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(227, 279);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(59, 12);
            this.label10.TabIndex = 22;
            this.label10.Text = "SAS键下标";
            // 
            // sasIndex
            // 
            this.sasIndex.Location = new System.Drawing.Point(294, 275);
            this.sasIndex.Name = "sasIndex";
            this.sasIndex.Size = new System.Drawing.Size(100, 21);
            this.sasIndex.TabIndex = 21;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(36, 53);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(41, 12);
            this.label11.TabIndex = 24;
            this.label11.Text = "机器名";
            // 
            // machineName
            // 
            this.machineName.Location = new System.Drawing.Point(106, 49);
            this.machineName.Name = "machineName";
            this.machineName.Size = new System.Drawing.Size(100, 21);
            this.machineName.TabIndex = 23;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(229, 154);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(65, 12);
            this.label12.TabIndex = 28;
            this.label12.Text = "截屏间隔ms";
            // 
            // screenDelay
            // 
            this.screenDelay.Location = new System.Drawing.Point(294, 150);
            this.screenDelay.Name = "screenDelay";
            this.screenDelay.Size = new System.Drawing.Size(100, 21);
            this.screenDelay.TabIndex = 27;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(27, 154);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(65, 12);
            this.label13.TabIndex = 26;
            this.label13.Text = "报告间隔ms";
            // 
            // reportDelay
            // 
            this.reportDelay.Location = new System.Drawing.Point(106, 150);
            this.reportDelay.Name = "reportDelay";
            this.reportDelay.Size = new System.Drawing.Size(100, 21);
            this.reportDelay.TabIndex = 25;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(20, 181);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(77, 12);
            this.label14.TabIndex = 30;
            this.label14.Text = "截屏缩放比例";
            // 
            // screenScale
            // 
            this.screenScale.Location = new System.Drawing.Point(106, 177);
            this.screenScale.Name = "screenScale";
            this.screenScale.Size = new System.Drawing.Size(100, 21);
            this.screenScale.TabIndex = 29;
            // 
            // installBtn
            // 
            this.installBtn.Location = new System.Drawing.Point(219, 345);
            this.installBtn.Name = "installBtn";
            this.installBtn.Size = new System.Drawing.Size(81, 35);
            this.installBtn.TabIndex = 31;
            this.installBtn.Text = "安装自启动";
            this.installBtn.UseVisualStyleBackColor = true;
            this.installBtn.Click += new System.EventHandler(this.OnInstallClick);
            // 
            // label15
            // 
            this.label15.Location = new System.Drawing.Point(22, 310);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(372, 23);
            this.label15.TabIndex = 32;
            this.label15.Text = "每项255长度，最后一项保留不可用";
            this.label15.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // runBtn
            // 
            this.runBtn.Location = new System.Drawing.Point(132, 345);
            this.runBtn.Name = "runBtn";
            this.runBtn.Size = new System.Drawing.Size(81, 35);
            this.runBtn.TabIndex = 33;
            this.runBtn.Text = "停止运行";
            this.runBtn.UseVisualStyleBackColor = true;
            this.runBtn.Click += new System.EventHandler(this.RunClick);
            // 
            // sasStart
            // 
            this.sasStart.AutoSize = true;
            this.sasStart.Location = new System.Drawing.Point(328, 53);
            this.sasStart.Name = "sasStart";
            this.sasStart.Size = new System.Drawing.Size(66, 16);
            this.sasStart.TabIndex = 34;
            this.sasStart.Text = "SAS启动";
            this.sasStart.UseVisualStyleBackColor = true;
            this.sasStart.CheckedChanged += new System.EventHandler(this.sasStart_CheckedChanged);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(411, 401);
            this.Controls.Add(this.sasStart);
            this.Controls.Add(this.runBtn);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.installBtn);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.screenScale);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.screenDelay);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.reportDelay);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.machineName);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.sasIndex);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.llockIndex);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.wallpaperIndex);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.keyboardIndex);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.shareLen);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.shareKey);
            this.Controls.Add(this.sasService);
            this.Controls.Add(this.modeServer);
            this.Controls.Add(this.modeClient);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.webPort);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.apiPort);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.serverPort);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.serverIP);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "班长安装工具";
            this.Load += new System.EventHandler(this.OnLoad);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

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
        private System.Windows.Forms.CheckBox sasService;
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
        private System.Windows.Forms.CheckBox sasStart;
    }
}

