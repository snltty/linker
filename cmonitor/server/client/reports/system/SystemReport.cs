using cmonitor.server.client.reports.screen;
using cmonitor.server.client.reports.screen.winapis;
using cmonitor.server.client.reports.share;
using common.libs;
using MemoryPack;
using Microsoft.Win32;

namespace cmonitor.server.client.reports.system
{
    public sealed class SystemReport : IReport
    {
        public string Name => "System";

        private readonly SystemReportInfo systemReportInfo = new SystemReportInfo();
        private readonly RegistryOptionHelper registryOptionHelper = new RegistryOptionHelper();
        private readonly RegistryCplHelper registryCplHelper = new RegistryCplHelper();
        private double lastCpu;
        private double lastMemory;


        private ReportDriveInfo[] drives;
        private Dictionary<string, RegistryOptionHelper.RegistryOptionKeyInfo> registryKeys;

        private readonly ShareReport shareReport;
        private readonly Config config;
        public SystemReport(ShareReport shareReport, Config config)
        {
            this.shareReport = shareReport;
            this.config = config;
            drives = WindowsDrive.GetAllDrives();
            registryKeys = registryOptionHelper.GetKeys();
            ReportTask();
        }


        long ticks = DateTime.UtcNow.Ticks;
        public object GetReports(ReportType reportType)
        {
            ticks = DateTime.UtcNow.Ticks;
            if (reportType == ReportType.Full)
            {
                systemReportInfo.Drives = drives;
                systemReportInfo.RegKeys = registryKeys;
            }
            else
            {
                systemReportInfo.Drives = null;
                systemReportInfo.RegKeys = null;
            }

            if (reportType == ReportType.Full || systemReportInfo.Cpu != lastCpu || systemReportInfo.Memory != lastMemory)
            {
                lastCpu = systemReportInfo.Cpu;
                lastMemory = systemReportInfo.Memory;
                return systemReportInfo;
            }
            return null;
        }

        public bool Password(PasswordInputInfo command)
        {
            return NetApi32.ChangePassword(null, Environment.UserName, command.OldPassword, command.NewPassword);
        }


        int registryUpdated = 0;
        int registryUpdateFlag = 1;
        public bool RegistryOptions(RegistryUpdateInfo registryUpdateInfo)
        {
            Interlocked.Exchange(ref registryUpdated, 1);
            return registryOptionHelper.UpdateValue(registryUpdateInfo.Name, registryUpdateInfo.Value);
        }
        private void RegistryForce()
        {
            if (registryUpdated == 1 && registryUpdated == 1)
            {
                Interlocked.CompareExchange(ref registryUpdated, 0, 1);
                Interlocked.CompareExchange(ref registryUpdateFlag, 0, 1);
                Task.Run(() =>
                {
                    CommandHelper.Windows(string.Empty, new string[] { "gpupdate /force" });
                    Interlocked.CompareExchange(ref registryUpdateFlag, 1, 0);
                });
            }
        }

        private void ReportTask()
        {
            CPUTime oldTime = WindowsCPU.GetCPUTime();
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if ((DateTime.UtcNow.Ticks - ticks) / TimeSpan.TicksPerMillisecond < 1000)
                    {
                        CPUTime newTime = WindowsCPU.GetCPUTime();
                        systemReportInfo.Cpu = CPUHelper.CalculateCPULoad(oldTime, newTime);
                        oldTime = newTime;

                        systemReportInfo.Memory = WindowsMemory.GetMemoryUsage();

                        systemReportInfo.RegValues = registryOptionHelper.GetValues();
                        //systemReportInfo.Disk = WindowsDrive.GetDiskUsage();
                    }
                    RegistryForce();


                    Thread.Sleep(1000);
                }
            }, TaskCreationOptions.LongRunning);


            /*
            ShareItemInfo shareItemInfo = new ShareItemInfo
            {
                Index = config.ShareMemoryLength - 1,
                Key = "System"
            };
            long lastTime = 0;
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    long time = (long)(DateTime.UtcNow.Subtract(startTime)).TotalMilliseconds;
                    if (time - lastTime >= 300)
                    {
                        shareItemInfo.Value = time.ToString();
                        shareReport.Update(shareItemInfo);
                        lastTime = time;
                    }
                    Thread.Sleep(100);
                }
            }, TaskCreationOptions.LongRunning);
            */
        }
    }

    public sealed class SystemReportInfo
    {
        public double Cpu { get; set; }
        public double Memory { get; set; }
        public float Disk { get; set; }
        public ReportDriveInfo[] Drives { get; set; }

        public string RegValues { get; set; }
        public Dictionary<string, RegistryOptionHelper.RegistryOptionKeyInfo> RegKeys { get; set; }
    }

    [MemoryPackable]
    public sealed partial class PasswordInputInfo
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }

    [MemoryPackable]
    public sealed partial class RegistryUpdateInfo
    {
        public string Name { get; set; }
        public bool Value { get; set; }
    }

    public sealed class RegistryOptionHelper
    {
        private string currentUserSid = string.Empty;
        char[] values;
        public RegistryOptionHelper()
        {
            values = string.Empty.PadLeft(Infos.Length, '0').ToCharArray();
            currentUserSid = Win32Interop.GetCurrentUserSid();
        }
        public string GetValues()
        {
            if (OperatingSystem.IsWindows())
            {
                for (int i = 0; i < Infos.Length; i++)
                {
                    RegistryOptionInfo item = Infos[i];
                    foreach (RegistryOptionPathInfo pathItem in item.Paths)
                    {
                        string path = ReplaceRegistryPath(pathItem.Path);
                        if (string.IsNullOrWhiteSpace(path))
                        {
                            continue;
                        }
                        object obj = Registry.GetValue(path, pathItem.Key, null);

                        values[i] = '0';
                        if (obj != null)
                        {
                            values[i] = (obj.ToString() == pathItem.DisallowValue ? '1' : '0');
                        }
                    }
                }
            }
            return new string(values);
        }
        public bool UpdateValue(string name, bool value)
        {
            RegistryOptionInfo info = Infos.FirstOrDefault(c => c.Key == name);
            if (info == null) return false;

            if (OperatingSystem.IsWindows())
            {
                foreach (RegistryOptionPathInfo pathItem in info.Paths)
                {
                    string path = ReplaceRegistryPath(pathItem.Path);
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }
                    Registry.SetValue(path, pathItem.Key, int.Parse(value ? pathItem.DisallowValue : pathItem.AllowValue), RegistryValueKind.DWord);
                }
                return true;
            }
            return false;
        }
        private string ReplaceRegistryPath(string path)
        {
            if (!string.IsNullOrWhiteSpace(currentUserSid))
            {
                return path.Replace("HKEY_CURRENT_USER", $"HKEY_USERS\\{currentUserSid}");
            }
            return string.Empty;
        }


        public Dictionary<string, RegistryOptionKeyInfo> GetKeys()
        {
            Dictionary<string, RegistryOptionKeyInfo> keys = new Dictionary<string, RegistryOptionKeyInfo>();

            for (int i = 0; i < Infos.Length; i++)
            {
                RegistryOptionInfo item = Infos[i];
                keys[item.Key] = new RegistryOptionKeyInfo { Desc = item.Desc, Index = (ushort)i };
            }

            return keys;
        }
        //Desc为空则不显示
        private RegistryOptionInfo[] Infos = new RegistryOptionInfo[] {
            new RegistryOptionInfo{
                Key="LockTaskbar",
                 Desc="任务栏锁定",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", Key="LockTaskbar", DisallowValue="1", AllowValue="0" }
                }
            },
            new RegistryOptionInfo{
                Key="TaskbarLockAll",
                Desc="任务栏设置",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", Key="TaskbarLockAll", DisallowValue="1", AllowValue="0" }
                }
            },
            new RegistryOptionInfo{
                Key="NoTrayContextMenu",
                Desc="任务栏菜单",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", Key="NoTrayContextMenu", DisallowValue="1", AllowValue="0" }
                }
            },
            new RegistryOptionInfo{
                Key="DisableTaskMgr",
                Desc="任务管理器",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", Key="DisableTaskMgr", DisallowValue="1", AllowValue="0" }
                }
            },
            
            new RegistryOptionInfo{
                Key="DisableRegistryTools",
                Desc="注册表编辑",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", Key="DisableRegistryTools", DisallowValue="0", AllowValue="0" }
                }
            },
            
            new RegistryOptionInfo{
                Key="RestrictRun",
                Desc="所有运行",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", Key="RestrictRun", DisallowValue="1", AllowValue="0" }
                }
            },
            
            new RegistryOptionInfo{
                Key="NoControlPanel",
                Desc="系统设置",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", Key="NoControlPanel", DisallowValue="1", AllowValue="0" }
                }
            },
            new RegistryOptionInfo{
                Key="NoSaveSettings",
                Desc="保存设置",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", Key="NoSaveSettings", DisallowValue="1", AllowValue="0" }
                }
            },

            new RegistryOptionInfo{
                Key="NoThemesTab",
                Desc="修改主题",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", Key="NoThemesTab", DisallowValue="1", AllowValue="0" }
                }
            },
            new RegistryOptionInfo{
                Key="NoChangingWallPaper",
                Desc="修改壁纸",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\ActiveDesktop", Key="NoChangingWallPaper", DisallowValue="1", AllowValue="0" }
                }
            },
            new RegistryOptionInfo{
                Key="NoDispAppearancePage",
                Desc="颜色外观",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", Key="NoDispAppearancePage", DisallowValue="1", AllowValue="0" }
                }
            },
            new RegistryOptionInfo{
                Key="NoDispBackgroundPage",
                Desc="桌面图标",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", Key="NoDispBackgroundPage", DisallowValue="1", AllowValue="0" }
                }
            },
            new RegistryOptionInfo{
                Key="ScreenSaveActive",
                Desc="屏幕保护",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_CURRENT_USER\\Control Panel\\Desktop", Key="ScreenSaveActive", DisallowValue="0", AllowValue="1" }
                }
            },
            new RegistryOptionInfo{
                Key="ScreenSaverIsSecure",
                Desc="唤醒登录",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_CURRENT_USER\\Control Panel\\Desktop", Key="ScreenSaverIsSecure", DisallowValue="0", AllowValue="1" }
                }
            },
            new RegistryOptionInfo{
                Key="AutoLock",
                Desc="关屏锁屏",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\7516b95f-f776-4464-8c53-06167f40cc99\\8EC4B3A5-6868-48c2-BE75-4F3044BE88A7", Key="Attributes", DisallowValue="0", AllowValue="1" }
                }
            },
            new RegistryOptionInfo{
                Key="NoClose",
                Desc="关机按钮",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", Key="NoClose", DisallowValue="1", AllowValue="0" },
                     new RegistryOptionPathInfo{ Path="HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", Key="ShutdownWithoutLogon", DisallowValue="1", AllowValue="0" },
                }
            },
            new RegistryOptionInfo{
                Key="NoLogOff",
                Desc="注销按钮",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", Key="StartMenuLogOff", DisallowValue="1", AllowValue="0" },
                     new RegistryOptionPathInfo{ Path="HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", Key="NoLogOff", DisallowValue="1", AllowValue="0" },
                }
            },
            new RegistryOptionInfo{
                Key="DisableLockWorkstation",
                Desc="锁定按钮",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", Key="DisableLockWorkstation", DisallowValue="1", AllowValue="0" }
                }
            },
            new RegistryOptionInfo{
                Key="DisableChangePassword",
                Desc="修改密码",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", Key="DisableChangePassword", DisallowValue="1", AllowValue="0" }
                }
            },
            new RegistryOptionInfo{
                Key="HideFastUserSwitching",
                Desc="切换用户",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", Key="HideFastUserSwitching", DisallowValue="1", AllowValue="0" }
                }
            },
            new RegistryOptionInfo{
                Key="SoftwareSASGeneration",
                Desc="安全SAS",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", Key="SoftwareSASGeneration", DisallowValue="0", AllowValue="3" }
                }
            },
            new RegistryOptionInfo{
                Key="USBSTOR",
                Desc="U盘",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\USBSTOR", Key="USBSTOR", DisallowValue="4", AllowValue="3" }
                }
            }
        };
        sealed class RegistryOptionInfo
        {
            public string Key { get; set; } = string.Empty;
            public string Desc { get; set; } = string.Empty;
            public RegistryOptionPathInfo[] Paths { get; set; }
        }
        sealed class RegistryOptionPathInfo
        {
            public string Path { get; set; }
            public string Key { get; set; }
            public string DisallowValue { get; set; }
            public string AllowValue { get; set; }
        }
        public sealed class RegistryOptionKeyInfo
        {
            public string Desc { get; set; }
            public ushort Index { get; set; }
        }



    }

    public sealed class RegistryCplHelper
    {
        private string currentUserSid = string.Empty;
        private char[] values;
        private string valuesPath = string.Empty;//"Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\DisallowCpl";
        private string switchPath = string.Empty;//"Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer";

        public RegistryCplHelper()
        {
            values = string.Empty.PadLeft(Infos.Length, '0').ToCharArray();
            currentUserSid = Win32Interop.GetCurrentUserSid();
            SwitchOn();
        }
        public string GetValues()
        {
            if (OperatingSystem.IsWindows())
            {
                RegistryKey key = GetRegistryValuesKey(valuesPath);
                if (key != null)
                {
                    for (int i = 0; i < Infos.Length; i++)
                    {
                        RegistryCplInfo item = Infos[i];
                        object value = key.GetValue(item.Key);
                        values[i] = (value == null ? '0' : '1');
                    }
                }
            }
            return new string(values);
        }
        public bool UpdateValue(string name, bool value)
        {
            RegistryCplInfo info = Infos.FirstOrDefault(c => c.Key == name);
            if (info == null) return false;

            if (OperatingSystem.IsWindows())
            {
                RegistryKey key = GetRegistryValuesKey(valuesPath);
                if (key == null)
                {
                    return false;
                }
                if (value)
                {
                    key.SetValue(info.Key, info.Key, RegistryValueKind.String);
                }
                else
                {
                    key.DeleteValue(info.Key);
                }
                key.Close();
                return true;
            }
            return false;
        }
        private RegistryKey GetRegistryValuesKey(string path)
        {
            if (OperatingSystem.IsWindows())
            {
                if (!string.IsNullOrWhiteSpace(currentUserSid))
                {
                    Registry.Users.OpenSubKey(currentUserSid, true);
                }
            }

            return null;
        }
        private void SwitchOn()
        {
            if (OperatingSystem.IsWindows())
            {
                RegistryKey key = GetRegistryValuesKey(switchPath);
                if (key != null)
                {
                    key.SetValue("DisallowCpl", 1, RegistryValueKind.DWord);
                    key.Close();
                }
            }
        }

        public Dictionary<string, RegistryCplKeyInfo> GetKeys()
        {
            Dictionary<string, RegistryCplKeyInfo> keys = new Dictionary<string, RegistryCplKeyInfo>();

            for (int i = 0; i < Infos.Length; i++)
            {
                RegistryCplInfo item = Infos[i];
                keys[item.Key] = new RegistryCplKeyInfo { Desc = item.Desc, Index = (ushort)i };
            }

            return keys;
        }
        //Desc为空则不显示
        private RegistryCplInfo[] Infos = new RegistryCplInfo[] {
            /*
            new RegistryCplInfo{ Key="Microsoft.ActionCenter", Desc="操作中心" },
            new RegistryCplInfo{ Key="Microsoft.AdministrativeTools", Desc="管理工具" },
            new RegistryCplInfo{ Key="Microsoft.AutoPlay", Desc="自动播放" },
            new RegistryCplInfo{ Key="Microsoft.BiometricDevices", Desc="生物识别设备" },
            new RegistryCplInfo{ Key="Microsoft.BitLockerDriveEncryption", Desc="BitLocker 驱动器加密" },
            new RegistryCplInfo{ Key="Microsoft.ColorManagement", Desc="颜色管理" },
            new RegistryCplInfo{ Key="Microsoft.CredentialManager", Desc="凭据管理器" },
            new RegistryCplInfo{ Key="Microsoft.DateAndTime", Desc="日期和时间" },
            new RegistryCplInfo{ Key="Microsoft.DefaultPrograms", Desc="默认程序" },
            new RegistryCplInfo{ Key="Microsoft.DeviceManager", Desc="设备管理器" },
            new RegistryCplInfo{ Key="Microsoft.DevicesAndPrinters", Desc="设备和打印机" },
            new RegistryCplInfo{ Key="Microsoft.Display", Desc="显示" },
            new RegistryCplInfo{ Key="Microsoft.EaseOfAccessCenter", Desc="轻松访问中心" },
            new RegistryCplInfo{ Key="Microsoft.ParentalControls", Desc="家庭安全" },
            new RegistryCplInfo{ Key="Microsoft.FileHistory", Desc="文件历史记录" },
            new RegistryCplInfo{ Key="Microsoft.FolderOptions", Desc="文件夹选项" },
            new RegistryCplInfo{ Key="Microsoft.Fonts", Desc="字体" },
            new RegistryCplInfo{ Key="Microsoft.HomeGroup", Desc="家庭组" },
            new RegistryCplInfo{ Key="Microsoft.IndexingOptions", Desc="索引选项" },
            new RegistryCplInfo{ Key="Microsoft.Infrared", Desc="红外线" },
            new RegistryCplInfo{ Key="Microsoft.InternetOptions", Desc="Internet 选项" },
            new RegistryCplInfo{ Key="Microsoft.iSCSIInitiator", Desc="iSCSI 发起程序" },
            new RegistryCplInfo{ Key="Microsoft.iSNSServer", Desc="iSNS 服务器" },
            new RegistryCplInfo{ Key="Microsoft.Keyboard", Desc="键盘" },
            new RegistryCplInfo{ Key="Microsoft.LocationSettings", Desc="位置设置" },
            new RegistryCplInfo{ Key="Microsoft.Mouse", Desc="鼠标" },
            new RegistryCplInfo{ Key="Microsoft.NetworkAndSharingCenter", Desc="网络和共享中心" },
            new RegistryCplInfo{ Key="Microsoft.NotificationAreaIcons", Desc="通知区域图标" },
            new RegistryCplInfo{ Key="Microsoft.PenAndTouch", Desc="触控笔和触控" },
            new RegistryCplInfo{ Key="Microsoft.Personalization", Desc="个性化" },
            new RegistryCplInfo{ Key="Microsoft.PowerOptions", Desc="电源选项" },
            new RegistryCplInfo{ Key="Microsoft.ProgramsAndFeatures", Desc="程序和功能" },
            new RegistryCplInfo{ Key="Microsoft.Recovery", Desc="恢复" },
            new RegistryCplInfo{ Key="Microsoft.RegionAndLanguage", Desc="区域" },
            new RegistryCplInfo{ Key="Microsoft.RemoteAppAndDesktopConnections", Desc="桌面连接" },
            new RegistryCplInfo{ Key="Microsoft.Sound", Desc="声音" },
            new RegistryCplInfo{ Key="Microsoft.SpeechRecognition", Desc="语音识别" },
            new RegistryCplInfo{ Key="Microsoft.StorageSpaces", Desc="存储空间" },
            new RegistryCplInfo{ Key="Microsoft.SyncCenter", Desc="同步中心" },
            new RegistryCplInfo{ Key="Microsoft.System", Desc="系统" },
            new RegistryCplInfo{ Key="Microsoft.Taskbar", Desc="任务栏和导航" },
            new RegistryCplInfo{ Key="Microsoft.Troubleshooting", Desc="疑难解答" },
            new RegistryCplInfo{ Key="Microsoft.TSAppInstall", Desc="TSAppInstall" },
            new RegistryCplInfo{ Key="Microsoft.UserAccounts", Desc="用户帐户" },
            new RegistryCplInfo{ Key="Microsoft.WindowsAnytimeUpgrade", Desc="Windows Anytime Upgrade" },
            new RegistryCplInfo{ Key="Microsoft.WindowsDefender", Desc="Windows Defender" },
            new RegistryCplInfo{ Key="Microsoft.WindowsFirewall", Desc="Windows 防火墙" },
            new RegistryCplInfo{ Key="Microsoft.MobilityCenter", Desc="Windows 移动中心" },
            new RegistryCplInfo{ Key="Microsoft.PortableWorkspaceCreator", Desc="Windows To Go" },
            new RegistryCplInfo{ Key="Microsoft.WindowsUpdate", Desc="Windows 更新" },
            new RegistryCplInfo{ Key="Microsoft.WorkFolders", Desc="工作文件夹" },
            */
        };
        sealed class RegistryCplInfo
        {
            public string Key { get; set; } = string.Empty;
            public string Desc { get; set; } = string.Empty;
        }
        public sealed class RegistryCplKeyInfo
        {
            public string Desc { get; set; }
            public ushort Index { get; set; }
        }



    }
}
