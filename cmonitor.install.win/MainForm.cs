using common.libs;
using System.Diagnostics;

namespace cmonitor.install.win
{
    public partial class MainForm : Form
    {

        Config config = new Config();
        public MainForm()
        {
            InitializeComponent();

            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            LoadConfig();
            SaveConfig();

            CheckInstall();
            CheckRunning();
        }

        private void SaveConfig()
        {
            config.Save();

        }
        private void LoadConfig()
        {
            modeClient.Checked = config.Common.Modes.Contains("client");
            modeServer.Checked = config.Common.Modes.Contains("server");

            machineName.Text = config.Client.Name;
            serverIP.Text = config.Client.Server;

            shareKey.Text = config.Client.ShareMemoryKey;
            shareLen.Text = config.Client.ShareMemoryCount.ToString();
            shareItemLen.Text = config.Client.ShareMemorySize.ToString();

            serverPort.Text = config.Server.ServicePort.ToString();
            apiPort.Text = config.Server.ApiPort.ToString();
            webPort.Text = config.Server.WebPort.ToString();
        }

        private bool loading = false;
        private bool installed = false;
        private bool running = false;
        private string serviceName = "cmonitor.sas.service";
        private string exeName = "cmonitor.sas.service.exe";
        private void OnInstallClick(object sender, EventArgs e)
        {
            if (loading)
            {
                return;
            }

            bool result = CheckMode();
            if (result == false) return;
            result = CheckIPAndPort();
            if (result == false) return;
            result = CheckShare();
            if (result == false) return;

            CheckLoading(true);
            SaveConfig();

            string filename = Process.GetCurrentProcess().MainModule.FileName;
            string dir = Path.GetDirectoryName(filename);
            string sasPath = Path.Combine(dir, exeName);

            string sasIndexStr = "4";

            string shareKeyStr = shareKey.Text;
            string shareLenStr = shareLen.Text;
            string shareItemLenStr = shareItemLen.Text;

            Task.Run(async () =>
            {
                if (installed == false)
                {
                    string taskStr = $"sc create \"{serviceName}\" binpath= \"{sasPath} {shareKeyStr} {shareLenStr} {shareItemLenStr} {sasIndexStr} \" start= AUTO";
                    CommandHelper.Windows(string.Empty, new string[] {
                        taskStr,
                        $"net start {serviceName}",
                    });
                }
                else
                {
                    while (running)
                    {
                        Stop();
                        await Task.Delay(1000);
                        CheckRunning();
                    }
                    string resultStr = CommandHelper.Windows(string.Empty, new string[] {
                        "schtasks /delete /TN \"cmonitorService\" /f",
                        $"net stop {serviceName}",
                        $"sc delete {serviceName}",
                    });
                }

                CheckLoading(false);
                CheckInstall();
                CheckRunning();
            });
        }

        private bool CheckMode()
        {
            if (modeClient.Checked == false && modeServer.Checked == false)
            {
                MessageBox.Show("客户端和服务端必须选择一样！");
                return false;
            }
            List<string> modeStr = new List<string>();
            if (modeClient.Checked)
            {
                modeStr.Add("client");
            }
            if (modeServer.Checked)
            {
                modeStr.Add("server");
            }
            config.Common.Modes = modeStr.ToArray();

            return true;
        }
        private bool CheckIPAndPort()
        {
            if (string.IsNullOrWhiteSpace(serverIP.Text))
            {
                MessageBox.Show("服务器ip必填");
                return false;
            }
            if (string.IsNullOrWhiteSpace(serverPort.Text))
            {
                MessageBox.Show("服务器端口必填");
                return false;
            }
            if (string.IsNullOrWhiteSpace(apiPort.Text))
            {
                MessageBox.Show("管理端口必填");
                return false;
            }
            if (string.IsNullOrWhiteSpace(webPort.Text))
            {
                MessageBox.Show("web端口必填");
                return false;
            }
            config.Client.Server = serverIP.Text;
            config.Server.WebPort = int.Parse(webPort.Text);
            config.Server.ApiPort = int.Parse(apiPort.Text);
            config.Server.ServicePort = int.Parse(serverPort.Text);
            return true;
        }
       
        private bool CheckShare()
        {
            if (string.IsNullOrWhiteSpace(shareKey.Text))
            {
                MessageBox.Show("共享数据键必填");
                return false;
            }
            if (string.IsNullOrWhiteSpace(shareLen.Text))
            {
                MessageBox.Show("共享数量必填");
                return false;
            }
            if (string.IsNullOrWhiteSpace(shareItemLen.Text))
            {
                MessageBox.Show("共享每项数据长度必填");
                return false;
            }
            config.Client.ShareMemoryKey = shareKey.Text;
            config.Client.ShareMemoryCount = int.Parse(shareLen.Text);
            config.Client.ShareMemorySize = int.Parse(shareItemLen.Text);
            return true;
        }

        private void CheckLoading(bool state)
        {
            loading = state;
            this.Invoke(new EventHandler(delegate
            {
                if (loading)
                {
                    installBtn.Text = "操作中..";
                    runBtn.Text = "操作中..";
                    checkStateBtn.Text = "操作中..";
                }
                else
                {
                    checkStateBtn.Text = "检查状态";
                    if (installed)
                    {
                        installBtn.ForeColor = Color.Red;
                        installBtn.Text = "解除自启动";
                        runBtn.Enabled = true;
                    }
                    else
                    {
                        installBtn.ForeColor = Color.Black;
                        installBtn.Text = "安装自启动";
                        runBtn.Enabled = false;
                    }

                    if (running)
                    {
                        runBtn.ForeColor = Color.Red;
                        runBtn.Text = "停止运行";
                    }
                    else
                    {
                        runBtn.ForeColor = Color.Black;
                        runBtn.Text = "启动";
                    }
                }
            }));
        }
        private void CheckInstall()
        {
            Task.Run(() =>
            {
                string result = CommandHelper.Windows(string.Empty, new string[] { $"sc query {serviceName}" });
                installed = result.Contains($"SERVICE_NAME: {serviceName}");
                CheckLoading(loading);
            });
        }

        private void RunClick(object sender, EventArgs e)
        {
            if (loading) return;

            CheckLoading(true);

            Task.Run(async () =>
            {
                if (running)
                {
                    Stop();
                    while (running)
                    {
                        CheckRunning();
                        await Task.Delay(1000);
                    }
                }
                else
                {
                    Run();
                    for (int i = 0; i < 15 && running == false; i++)
                    {
                        CheckRunning();
                        await Task.Delay(1000);
                    }
                }
                CheckLoading(false);
            });
        }
        private void Run()
        {
            CommandHelper.Windows(string.Empty, new string[] {
                    "schtasks /run /I /TN \"cmonitorService\"",
                    $"net stop {serviceName}",
                    $"net start {serviceName}",
            });
        }
        private void Stop()
        {
            CommandHelper.Windows(string.Empty, new string[] { $"net stop \"{serviceName}\"", });
        }

        private void CheckRunning()
        {
            Task.Run(() =>
            {
                string result = CommandHelper.Windows(string.Empty, new string[] { $"sc query {serviceName}" });
                running = result.Contains(": 4  RUNNING");
                CheckLoading(loading);
            });
        }

        private void checkStateBtn_Click(object sender, EventArgs e)
        {
            if (loading) return;

            CheckLoading(loading);
            CheckInstall();
            CheckRunning();
        }

    }
}
