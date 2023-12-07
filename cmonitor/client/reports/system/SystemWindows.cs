using common.libs;
using common.libs.winapis;
using Microsoft.Win32;

namespace cmonitor.client.reports.system
{
    public sealed class SystemWindows : ISystem
    {
        private readonly SystemOptionHelper registryOptionHelper;
        public SystemWindows(ClientConfig clientConfig,Config config)
        {
            if (config.IsCLient)
            {
                registryOptionHelper = new SystemOptionHelper(clientConfig);
                OptionUpdate(new SystemOptionUpdateInfo { Name = "SoftwareSASGeneration", Value = false });
            }
        }

        public ReportDriveInfo[] GetAllDrives()
        {
            return DriveInfo.GetDrives().Select(c => new ReportDriveInfo
            {
                Name = c.Name,
                Free = c.TotalFreeSpace,
                Total = c.TotalSize
            }).ToArray();
        }

        public Dictionary<string, SystemOptionKeyInfo> GetOptionKeys()
        {
            return registryOptionHelper.GetKeys();
        }
        public string GetOptionValues()
        {
            return registryOptionHelper.GetValues();
        }
        public bool OptionUpdate(SystemOptionUpdateInfo registryUpdateInfo)
        {
            bool result = registryOptionHelper.UpdateValue(registryUpdateInfo.Name, registryUpdateInfo.Value);
            if (result)
            {
                Interlocked.Exchange(ref registryUpdated, 1);
            }
            return result;
        }
        int registryUpdated = 0;
        int registryUpdateFlag = 1;
        public void OptionRefresh()
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

        public bool Password(PasswordInputInfo command)
        {
            return NetApi32.ChangePassword(null, Environment.UserName, command.OldPassword, command.NewPassword);
        }

        CPUTime oldTime = new CPUTime(0,0);
        public double GetCpu()
        {
            CPUTime newTime = SystemWindowsCPU.GetCPUTime();
            double value = CPUHelper.CalculateCPULoad(oldTime, newTime);
            oldTime = newTime;
            return value;
        }
        public double GetMemory()
        {
            return SystemWindowsMemory.GetMemoryUsage();
        }
    }


    public sealed class SystemOptionHelper
    {
        private string currentUserSid = string.Empty;
        private readonly ClientConfig clientConfig;
        char[] values;
        public SystemOptionHelper(ClientConfig clientConfig)
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
                    string setValue = value ? pathItem.DisallowValue : pathItem.AllowValue;
                    if (Registry.GetValue(path, pathItem.Key, pathItem.AllowValue).ToString() != setValue)
                    {
                        Registry.SetValue(path, pathItem.Key, int.Parse(setValue), RegistryValueKind.DWord);
                        return true;
                    }
                }
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

        public Dictionary<string, SystemOptionKeyInfo> GetKeys()
        {
            Dictionary<string, SystemOptionKeyInfo> keys = new Dictionary<string, SystemOptionKeyInfo>();

            for (int i = 0; i < Infos.Length; i++)
            {
                RegistryOptionInfo item = Infos[i];
                keys[item.Key] = new SystemOptionKeyInfo { Desc = item.Desc, Index = (ushort)i };
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
    }
}
