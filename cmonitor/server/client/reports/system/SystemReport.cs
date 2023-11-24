using cmonitor.server.client.reports.screen;
using cmonitor.server.client.reports.screen.winapis;
using common.libs;
using MemoryPack;
using Microsoft.Win32;

namespace cmonitor.server.client.reports.system
{
    public sealed class SystemReport : IReport
    {
        public string Name => "System";

        private readonly SystemReportInfo systemReportInfo = new SystemReportInfo();
        private readonly RegistryOptionHelper registryOptionHelper;

        private double lastCpu;
        private double lastMemory;
        private ReportDriveInfo[] drives;
        private Dictionary<string, RegistryOptionHelper.RegistryOptionKeyInfo> registryKeys;

        public SystemReport(ClientConfig clientConfig)
        {
            registryOptionHelper = new RegistryOptionHelper(clientConfig);

            drives = WindowsDrive.GetAllDrives();
            registryKeys = registryOptionHelper.GetKeys();
            ReportTask();
            RegistryOptions(new RegistryUpdateInfo { Name = "SoftwareSASGeneration", Value = true });
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
        private readonly ClientConfig clientConfig;
        char[] values;
        public RegistryOptionHelper(ClientConfig clientConfig)
        {
            this.clientConfig = clientConfig;
            values = string.Empty.PadLeft(Infos.Length, '0').ToCharArray();
            GetSid();
        }

        public string GetValues()
        {
            GetSid();

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
            if (string.IsNullOrWhiteSpace(currentUserSid))
            {
                return false;
            }
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
        private void GetSid()
        {
            if (string.IsNullOrWhiteSpace(currentUserSid))
            {
                currentUserSid = clientConfig.UserSid;
            }
            if (string.IsNullOrWhiteSpace(currentUserSid))
            {
                currentUserSid = Win32Interop.GetCurrentUserSid();
                clientConfig.UserSid = currentUserSid;
            }
            if (string.IsNullOrWhiteSpace(currentUserSid))
            {
                currentUserSid = Win32Interop.GetDefaultUserSid();
                clientConfig.UserSid = currentUserSid;
            }
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
                     new RegistryOptionPathInfo{ Path="HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\USBSTOR", Key="Start", DisallowValue="4", AllowValue="3" }
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

}
