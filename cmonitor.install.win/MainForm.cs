using common.libs;
using common.libs.extends;
using Microsoft.Win32;
using System.Diagnostics;
using System.Net;

namespace cmonitor.install.win
{
    public partial class MainForm : Form
    {
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
            RegistryKey key = CheckRegistryKey();
            key.SetValue("installParams", new ConfigInfo
            {
                Client = modeClient.Checked,
                Server = modeServer.Checked,
                MachineName = machineName.Text,
                ServerEndpoint = new IPEndPoint(IPAddress.Parse(serverIP.Text), int.Parse(serverPort.Text)).ToString(),
                ApiPort = int.Parse(apiPort.Text),
                WebPort = int.Parse(webPort.Text),
                ReportDelay = int.Parse(reportDelay.Text),
                ScreenDelay = int.Parse(screenDelay.Text),
                ScreenScale = double.Parse(screenScale.Text),
                ShareKey = shareKey.Text,
                ShareLen = int.Parse(shareLen.Text),
                ShareSize = int.Parse(shareItemLen.Text),
            }.ToJson());

        }
        private void LoadConfig()
        {
            RegistryKey key = CheckRegistryKey();


            ConfigInfo config = key.GetValue("installParams", "{}").ToString().DeJson<ConfigInfo>();

            modeClient.Checked = config.Client;
            modeServer.Checked = config.Server;

            machineName.Text = config.MachineName;

            IPEndPoint ep = IPEndPoint.Parse(config.ServerEndpoint);
            serverIP.Text = ep.Address.ToString();
            serverPort.Text = ep.Port.ToString();
            apiPort.Text = config.ApiPort.ToString();
            webPort.Text = config.WebPort.ToString();

            reportDelay.Text = config.ReportDelay.ToString();
            screenDelay.Text = config.ScreenDelay.ToString();
            screenScale.Text = config.ScreenScale.ToString();

            shareKey.Text = config.ShareKey;
            shareLen.Text = config.ShareLen.ToString();
            shareItemLen.Text = config.ShareSize.ToString();

        }
        private RegistryKey CheckRegistryKey()
        {
            Registry.SetValue("HKEY_CURRENT_USER\\SOFTWARE\\cmonitor", "test", 1);

            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software");
            return key.OpenSubKey("cmonitor", true);
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

            List<string> installParams = new List<string>();

            bool result = CheckMode(installParams);
            if (result == false) return;
            result = CheckIPAndPort(installParams);
            if (result == false) return;
            result = CheckDelay(installParams);
            if (result == false) return;
            result = CheckShare(installParams);
            if (result == false) return;

            CheckLoading(true);
            SaveConfig();

            string paramStr = string.Join(" ", installParams);

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
                    string taskStr = $"sc create \"{serviceName}\" binpath= \"{sasPath} {shareKeyStr} {shareLenStr} {shareItemLenStr} {sasIndexStr} \\\"{paramStr}\\\"\" start= AUTO";
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

        private bool CheckMode(List<string> installParams)
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
            installParams.Add($"--mode {string.Join(",", modeStr)}");

            return true;
        }
        private bool CheckIPAndPort(List<string> installParams)
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
            installParams.Add($"--server {serverIP.Text}");
            installParams.Add($"--service {serverPort.Text}");
            installParams.Add($"--api {apiPort.Text}");
            installParams.Add($"--web {webPort.Text}");

            return true;
        }
        private bool CheckDelay(List<string> installParams)
        {
            if (string.IsNullOrWhiteSpace(reportDelay.Text))
            {
                MessageBox.Show("报告间隔时间必填");
                return false;
            }
            if (string.IsNullOrWhiteSpace(screenDelay.Text))
            {
                MessageBox.Show("截屏间隔时间必填");
                return false;
            }
            if (string.IsNullOrWhiteSpace(screenScale.Text))
            {
                MessageBox.Show("截屏缩放比例必填");
                return false;
            }
            installParams.Add($"--report-delay {reportDelay.Text}");
            installParams.Add($"--screen-delay {screenDelay.Text}");
            installParams.Add($"--screen-scale {screenScale.Text}");

            return true;
        }
        private bool CheckShare(List<string> installParams)
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

            installParams.Add($"--share-key {shareKey.Text}");
            installParams.Add($"--share-len {shareLen.Text}");
            installParams.Add($"--share-item-len {shareItemLen.Text}");

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

        public sealed class ConfigInfo
        {
            public bool Client { get; set; }
            public bool Server { get; set; }
            public string MachineName { get; set; } = Dns.GetHostName();
            public string ServerEndpoint { get; set; } = new IPEndPoint(IPAddress.Loopback, 1802).ToString();
            public int ApiPort { get; set; } = 1801;
            public int WebPort { get; set; } = 1800;
            public int ReportDelay { get; set; } = 30;
            public int ScreenDelay { get; set; } = 200;
            public double ScreenScale { get; set; } = 0.2;
            public string ShareKey { get; set; } = "cmonitor/share";
            public int ShareLen { get; set; } = 100;
            public int ShareSize { get; set; } = 1024;

        }
    }
}
