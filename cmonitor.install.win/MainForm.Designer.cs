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
            label11 = new Label();
            machineName = new TextBox();
            installBtn = new Button();
            runBtn = new Button();
            checkStateBtn = new Button();
            label16 = new Label();
            shareItemLen = new TextBox();
            gbClient = new GroupBox();
            gbServer = new GroupBox();
            cbBlueProtect = new CheckBox();
            gbClient.SuspendLayout();
            gbServer.SuspendLayout();
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
            // installBtn
            // 
            resources.ApplyResources(installBtn, "installBtn");
            installBtn.Name = "installBtn";
            installBtn.UseVisualStyleBackColor = true;
            installBtn.Click += OnInstallClick;
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
            // gbClient
            // 
            resources.ApplyResources(gbClient, "gbClient");
            gbClient.Controls.Add(machineName);
            gbClient.Controls.Add(label16);
            gbClient.Controls.Add(serverIP);
            gbClient.Controls.Add(shareItemLen);
            gbClient.Controls.Add(label1);
            gbClient.Controls.Add(label11);
            gbClient.Controls.Add(shareLen);
            gbClient.Controls.Add(shareKey);
            gbClient.Controls.Add(label5);
            gbClient.Controls.Add(label6);
            gbClient.Name = "gbClient";
            gbClient.TabStop = false;
            // 
            // gbServer
            // 
            resources.ApplyResources(gbServer, "gbServer");
            gbServer.Controls.Add(apiPort);
            gbServer.Controls.Add(serverPort);
            gbServer.Controls.Add(label2);
            gbServer.Controls.Add(label4);
            gbServer.Controls.Add(webPort);
            gbServer.Controls.Add(label3);
            gbServer.Name = "gbServer";
            gbServer.TabStop = false;
            // 
            // cbBlueProtect
            // 
            resources.ApplyResources(cbBlueProtect, "cbBlueProtect");
            cbBlueProtect.Name = "cbBlueProtect";
            cbBlueProtect.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(cbBlueProtect);
            Controls.Add(gbServer);
            Controls.Add(gbClient);
            Controls.Add(checkStateBtn);
            Controls.Add(runBtn);
            Controls.Add(installBtn);
            Controls.Add(modeServer);
            Controls.Add(modeClient);
            Name = "MainForm";
            Load += OnLoad;
            gbClient.ResumeLayout(false);
            gbClient.PerformLayout();
            gbServer.ResumeLayout(false);
            gbServer.PerformLayout();
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
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox machineName;
        private System.Windows.Forms.Button installBtn;
        private System.Windows.Forms.Button runBtn;
        private System.Windows.Forms.Button checkStateBtn;

        #endregion

        private Label label16;
        private TextBox shareItemLen;
        private GroupBox gbClient;
        private GroupBox gbServer;
        private CheckBox cbBlueProtect;
    }
}
