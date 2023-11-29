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
            resources.ApplyResources(serverIP, "serverIP");
            serverIP.Name = "serverIP";
            // 
            // label1
            // 
            resources.ApplyResources(label1, "label1");
            label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(label2, "label2");
            label2.Name = "label2";
            // 
            // serverPort
            // 
            resources.ApplyResources(serverPort, "serverPort");
            serverPort.Name = "serverPort";
            // 
            // label3
            // 
            resources.ApplyResources(label3, "label3");
            label3.Name = "label3";
            // 
            // webPort
            // 
            resources.ApplyResources(webPort, "webPort");
            webPort.Name = "webPort";
            // 
            // label4
            // 
            resources.ApplyResources(label4, "label4");
            label4.Name = "label4";
            // 
            // apiPort
            // 
            resources.ApplyResources(apiPort, "apiPort");
            apiPort.Name = "apiPort";
            // 
            // modeClient
            // 
            resources.ApplyResources(modeClient, "modeClient");
            modeClient.Name = "modeClient";
            modeClient.UseVisualStyleBackColor = true;
            // 
            // modeServer
            // 
            resources.ApplyResources(modeServer, "modeServer");
            modeServer.Name = "modeServer";
            modeServer.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            resources.ApplyResources(label5, "label5");
            label5.Name = "label5";
            // 
            // shareLen
            // 
            resources.ApplyResources(shareLen, "shareLen");
            shareLen.Name = "shareLen";
            // 
            // label6
            // 
            resources.ApplyResources(label6, "label6");
            label6.Name = "label6";
            // 
            // shareKey
            // 
            resources.ApplyResources(shareKey, "shareKey");
            shareKey.Name = "shareKey";
            // 
            // label7
            // 
            resources.ApplyResources(label7, "label7");
            label7.Name = "label7";
            // 
            // wallpaperIndex
            // 
            resources.ApplyResources(wallpaperIndex, "wallpaperIndex");
            wallpaperIndex.Name = "wallpaperIndex";
            // 
            // label8
            // 
            resources.ApplyResources(label8, "label8");
            label8.Name = "label8";
            // 
            // keyboardIndex
            // 
            resources.ApplyResources(keyboardIndex, "keyboardIndex");
            keyboardIndex.Name = "keyboardIndex";
            // 
            // label9
            // 
            resources.ApplyResources(label9, "label9");
            label9.Name = "label9";
            // 
            // llockIndex
            // 
            resources.ApplyResources(llockIndex, "llockIndex");
            llockIndex.Name = "llockIndex";
            // 
            // label10
            // 
            resources.ApplyResources(label10, "label10");
            label10.Name = "label10";
            // 
            // sasIndex
            // 
            resources.ApplyResources(sasIndex, "sasIndex");
            sasIndex.Name = "sasIndex";
            // 
            // label11
            // 
            resources.ApplyResources(label11, "label11");
            label11.Name = "label11";
            // 
            // machineName
            // 
            resources.ApplyResources(machineName, "machineName");
            machineName.Name = "machineName";
            // 
            // label12
            // 
            resources.ApplyResources(label12, "label12");
            label12.Name = "label12";
            // 
            // screenDelay
            // 
            resources.ApplyResources(screenDelay, "screenDelay");
            screenDelay.Name = "screenDelay";
            // 
            // label13
            // 
            resources.ApplyResources(label13, "label13");
            label13.Name = "label13";
            // 
            // reportDelay
            // 
            resources.ApplyResources(reportDelay, "reportDelay");
            reportDelay.Name = "reportDelay";
            // 
            // label14
            // 
            resources.ApplyResources(label14, "label14");
            label14.Name = "label14";
            // 
            // screenScale
            // 
            resources.ApplyResources(screenScale, "screenScale");
            screenScale.Name = "screenScale";
            // 
            // installBtn
            // 
            resources.ApplyResources(installBtn, "installBtn");
            installBtn.Name = "installBtn";
            installBtn.UseVisualStyleBackColor = true;
            installBtn.Click += OnInstallClick;
            // 
            // label15
            // 
            resources.ApplyResources(label15, "label15");
            label15.Name = "label15";
            // 
            // runBtn
            // 
            resources.ApplyResources(runBtn, "runBtn");
            runBtn.Name = "runBtn";
            runBtn.UseVisualStyleBackColor = true;
            runBtn.Click += RunClick;
            // 
            // checkStateBtn
            // 
            resources.ApplyResources(checkStateBtn, "checkStateBtn");
            checkStateBtn.Name = "checkStateBtn";
            checkStateBtn.UseVisualStyleBackColor = true;
            checkStateBtn.Click += checkStateBtn_Click;
            // 
            // label16
            // 
            resources.ApplyResources(label16, "label16");
            label16.Name = "label16";
            // 
            // shareItemLen
            // 
            resources.ApplyResources(shareItemLen, "shareItemLen");
            shareItemLen.Name = "shareItemLen";
            // 
            // MainForm
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = AutoScaleMode.Font;
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
            Name = "MainForm";
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
