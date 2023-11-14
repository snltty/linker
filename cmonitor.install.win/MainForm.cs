using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace cmonitor.install.win
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            this.FormBorderStyle =  FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            keyboardIndex.ReadOnly = true;
            wallpaperIndex.ReadOnly = true;
            llockIndex.ReadOnly = true;
            sasIndex.ReadOnly = true;

            LoadConfig();
            SaveConfig();

            CheckInstall();
            CheckRunning();
        }

        private void SaveConfig()
        {
            RegistryKey key = CheckRegistryKey();
            key.SetValue("modeClient", modeClient.Checked ? "1" : "0");
            key.SetValue("modeServer", modeServer.Checked ? "1" : "0");

            key.SetValue("machineName", machineName.Text);
            key.SetValue("sasService", sasService.Checked ? "1" : "0");

            key.SetValue("serverIP", serverIP.Text);
            key.SetValue("serverPort", serverPort.Text);
            key.SetValue("apiPort", apiPort.Text);
            key.SetValue("webPort", webPort.Text);

            key.SetValue("reportDelay", reportDelay.Text);
            key.SetValue("screenDelay", screenDelay.Text);
            key.SetValue("screenScale", screenScale.Text);

            key.SetValue("shareKey", shareKey.Text);
            key.SetValue("shareLen", shareLen.Text);

            key.SetValue("keyboardIndex", keyboardIndex.Text);
            key.SetValue("wallpaperIndex", wallpaperIndex.Text);
            key.SetValue("llockIndex", llockIndex.Text);
            key.SetValue("sasIndex", sasIndex.Text);


        }
        private void LoadConfig()
        {
            RegistryKey key = CheckRegistryKey();

            string hostname = Dns.GetHostName();

            modeClient.Checked = key.GetValue("modeClient", "0").ToString() == "1";
            modeServer.Checked = key.GetValue("modeServer", "0").ToString() == "1";

            machineName.Text = key.GetValue("machineName", hostname).ToString();
            sasService.Checked = key.GetValue("sasService", "0").ToString() == "1";

            serverIP.Text = key.GetValue("serverIP", "127.0.0.1").ToString();
            serverPort.Text = key.GetValue("serverPort", "1802").ToString();
            apiPort.Text = key.GetValue("apiPort", "1801").ToString();
            webPort.Text = key.GetValue("webPort", "1800").ToString();

            reportDelay.Text = key.GetValue("reportDelay", "30").ToString();
            screenDelay.Text = key.GetValue("screenDelay", "200").ToString();
            screenScale.Text = key.GetValue("screenScale", "0.2").ToString();

            shareKey.Text = key.GetValue("shareKey", "cmonitor/share").ToString();
            shareLen.Text = key.GetValue("shareLen", "10").ToString();

            keyboardIndex.Text = key.GetValue("keyboardIndex", "0").ToString();
            wallpaperIndex.Text = key.GetValue("wallpaperIndex", "1").ToString();
            llockIndex.Text = key.GetValue("llockIndex", "2").ToString();
            sasIndex.Text = key.GetValue("sasIndex", "3").ToString();
        }

        private RegistryKey CheckRegistryKey()
        {
            Registry.SetValue("HKEY_CURRENT_USER\\SOFTWARE\\Cmonitor", "test", 1);

            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software");
            return key.OpenSubKey("cmonitor", true);
        }


        bool loading = false;
        bool installed = false;
        bool running = false;
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
            bool installSas = sasService.Checked;

            string filename = Process.GetCurrentProcess().MainModule.FileName;
            string dir = Path.GetDirectoryName(filename);
            string exePath = Path.Combine(dir, "cmonitor.win.exe");
            string sasPath = Path.Combine(dir, "cmonitor.sas.service.exe");

            Task.Run(() =>
            {
                if (installed == false)
                {
                    string str = CommandHelper.Windows(string.Empty, new string[] {
                        $"schtasks.exe /create /tn \"cmonitorService\" /rl highest /sc ONLOGON /delay 0000:30 /tr \"\"{exePath}\" {paramStr}\" "
                    });
                    if (installSas)
                    {
                        str = CommandHelper.Windows(string.Empty, new string[] {
                        $"sc create \"cmonitor.sas.service\" binpath=\"{sasPath}\" start=AUTO",
                        "net start cmonitor.sas.service",
                        });
                    }
                }
                else
                {
                    while (running)
                    {
                        Stop();
                        System.Threading.Thread.Sleep(1000);
                        CheckRunning();
                    }
                    string resultStr = CommandHelper.Windows(string.Empty, new string[] {
                        "schtasks /delete /TN \"cmonitorService\" /f",
                        "net stop cmonitor.sas.service",
                        "sc delete cmonitor.sas.service",
                    });
                }

                CheckLoading(false);
                CheckInstall();
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
            if (string.IsNullOrWhiteSpace(keyboardIndex.Text))
            {
                MessageBox.Show("键盘键下标必填");
                return false;
            }
            if (string.IsNullOrWhiteSpace(wallpaperIndex.Text))
            {
                MessageBox.Show("壁纸键下标必填");
                return false;
            }
            if (string.IsNullOrWhiteSpace(llockIndex.Text))
            {
                MessageBox.Show("锁屏键下标必填");
                return false;
            }
            if (string.IsNullOrWhiteSpace(sasIndex.Text))
            {
                MessageBox.Show("sas键下标必填");
                return false;
            }
            installParams.Add($"--share-key {shareKey.Text}");
            installParams.Add($"--share-len {shareLen.Text}");

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
                }
                else
                {
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
                string result = CommandHelper.Windows(string.Empty, new string[] { "schtasks.exe /query /fo TABLE|findstr \"cmonitor\"" });
                installed = result.Contains("cmonitorService");
                CheckLoading(loading);
            });
        }

        private void RunClick(object sender, EventArgs e)
        {
            if (loading) return;

            CheckLoading(true);

            Task.Run(() =>
            {
                if (running)
                {
                    Stop();
                    while (running)
                    {
                        CheckRunning();
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                else
                {
                    Run();
                    for (int i = 0; i < 15 && running==false; i++)
                    {
                        CheckRunning();
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                CheckLoading(false);
            });
        }
        private void Run()
        {
            CommandHelper.Windows(string.Empty, new string[] {
                    "schtasks /run /I /TN \"cmonitorService\"",
                    "net start cmonitor.sas.service",
            });
        }
        private void Stop()
        {
            CommandHelper.Windows(string.Empty, new string[] {
                    "taskkill /f /im \"cmonitor.win.exe\"",
                    "taskkill /f /im \"cmonitor.exe\"",
                    "taskkill /f /im \"wallpaper.win.exe\"",
                    "taskkill /f /im \"llock.win.exe\"",
                    "taskkill /f /im \"message.win.exe\"",
                    "taskkill /f /im \"notify.win.exe\"",
                    "net stop \"cmonitor.sas.service\"",
            });
        }

        private void CheckRunning()
        {
            running = Process.GetProcessesByName("cmonitor").Length > 0;
            CheckLoading(loading);
        }
    }
}
