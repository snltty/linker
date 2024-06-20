using cmonitor.client;
using cmonitor.config;
using common.libs;
using common.libs.extends;
using common.libs.winapis;
using Microsoft.Win32;
using monitor.plugins.system.report;
using System.Collections.Concurrent;
using System.Linq;

namespace cmonitor.plugins.system.report
{
    public sealed class SystemWindows : ISystem
    {
        private readonly SystemOptionHelper registryOptionHelper;
        private readonly ClientSignInState clientSignInState;
        private ConcurrentQueue<Action> actions = new ConcurrentQueue<Action>();
        private readonly Config config;
        public SystemWindows(Config config, ClientSignInState clientSignInState)
        {
            this.config = config;
            this.clientSignInState = clientSignInState;
            registryOptionHelper = new SystemOptionHelper();
            OptionsInit();
        }

        public ReportDriveInfo[] GetAllDrives()
        {
            List<ReportDriveInfo> result = new List<ReportDriveInfo>();
            foreach (DriveInfo item in DriveInfo.GetDrives())
            {
                try
                {
                    result.Add(new ReportDriveInfo
                    {
                        Name = item.Name,
                        Free = item.TotalFreeSpace,
                        Total = item.TotalSize
                    });
                }
                catch (Exception)
                {
                }
            }
            return result.ToArray();
        }

        bool restored = false;
        bool reused = false;
        private void OptionsInit()
        {
            LoopTask();
            actions.Enqueue(() =>
            {
                Task.Run(async () =>
                {
                    Logger.Instance.Info($"regedit restore");
                    while (restored == false)
                    {
                        restored |= registryOptionHelper.Restore();
                        await Task.Delay(5000);
                    }
                });

            });

            clientSignInState.NetworkFirstEnabledHandle += () =>
            {
                actions.Enqueue(() =>
                {
                    Task.Run(async () =>
                    {
                        Logger.Instance.Info($"regedit reuse");
                        while (reused == false)
                        {
                            if (restored)
                            {
                                reused |= registryOptionHelper.Reuse();
                                OptionUpdate(new SystemOptionUpdateInfo[] { new SystemOptionUpdateInfo { Key = "SoftwareSASGeneration", Value = false } });
                            }
                            await Task.Delay(5000);
                        }
                    });
                });
            };
        }
        public Dictionary<string, SystemOptionKeyInfo> OptionKeys()
        {
            return registryOptionHelper.GetKeys();
        }
        public string OptionValues()
        {
            return registryOptionHelper.GetValues();
        }
        public void OptionUpdate(SystemOptionUpdateInfo[] registryUpdateInfo)
        {
            actions.Enqueue(() =>
            {
                registryOptionHelper.UpdateValue(registryUpdateInfo);
            });
        }
        private void LoopTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (registryOptionHelper.GetSid() == false)
                    {
                        await Task.Delay(1000);
                    }
                    else
                    {
                        if (actions.IsEmpty == false)
                        {
                            while (actions.TryDequeue(out Action action))
                            {
                                if (config.Data.Elevated)
                                {
                                    Win32Interop.SwitchToInputDesktop();
                                }
                                action();
                            }
                            registryOptionHelper.Refresh();
                        }
                        await Task.Delay(30);
                    }
                }
            });


            Task.Run(async () =>
            {
                while (true)
                {
                    await TimeSync();
                    await Task.Delay(30000);
                }
            });
        }
        private async Task TimeSync()
        {
            if (registryOptionHelper.GetValue("TimeSync", true))
            {
                try
                {
                    DateTime dt = await Win32Interop.GetNetworkTime();
                    Win32Interop.SetSystemTime(dt);
                }
                catch (Exception)
                {
                }
            }
        }

        public bool Password(PasswordInputInfo command)
        {
            return NetApi32.ChangePassword(null, Environment.UserName, command.OldPassword, command.NewPassword);
        }


        CPUTime oldTime = new CPUTime(0, 0);
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
        private char[] values;
        private bool changed = true;

        public SystemOptionHelper()
        {
            values = string.Empty.PadLeft(Infos.Length, '0').ToCharArray();
            GetSid();
        }

        private string backupPath = "HKEY_LOCAL_MACHINE\\SOFTWARE\\cmonitorBackup";
        private string backupKey = "cmonitorBackup";
        public bool Restore()
        {
            if (OperatingSystem.IsWindows() && GetSid())
            {
                try
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey("Software");
                    key = key.OpenSubKey(backupKey, true);
                    foreach (RegistryOptionInfo option in Infos)
                    {
                        foreach (RegistryOptionPathInfo path in option.Paths)
                        {
                            string pathStr = ReplaceRegistryPath(path.Path);
                            if (string.IsNullOrWhiteSpace(pathStr))
                            {
                                continue;
                            }
                            //备份已经设置的
                            object setValue = Registry.GetValue(pathStr, path.Key, null);
                            if (setValue != null)
                            {
                                Registry.SetValue(backupPath, $"{option.Key}_{path.Key}_old", setValue);
                            }
                            //删除设置
                            string delPathStr = pathStr.Replace("HKEY_CURRENT_USER\\", "").Replace("HKEY_LOCAL_MACHINE\\", "");
                            try
                            {
                                RegistryKey _key = Registry.LocalMachine.OpenSubKey(delPathStr, true);
                                _key?.DeleteValue(path.Key, false);
                            }
                            catch (Exception ex)
                            {
                                Logger.Instance.Error(ex);
                                return false;
                            }
                            try
                            {
                                RegistryKey _key = Registry.CurrentUser.OpenSubKey(delPathStr, true);
                                _key?.DeleteValue(path.Key, false);
                            }
                            catch (Exception ex)
                            {
                                Logger.Instance.Error(ex);
                                return false;
                            }
                        }
                    }
                    Updated();
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                    return false;
                }
            }
            return true;
        }
        public bool Reuse()
        {
            if (OperatingSystem.IsWindows() && GetSid())
            {
                try
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey("Software");
                    key = key.OpenSubKey(backupKey, true);
                    foreach (RegistryOptionInfo option in Infos)
                    {
                        foreach (RegistryOptionPathInfo path in option.Paths)
                        {
                            string pathStr = ReplaceRegistryPath(path.Path);
                            if (string.IsNullOrWhiteSpace(pathStr))
                            {
                                continue;
                            }
                            object value = null;
                            try
                            {
                                value = Registry.GetValue(backupPath, $"{option.Key}_{path.Key}_old", null);
                                if (value != null)
                                {
                                    Registry.SetValue(pathStr, path.Key, value);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Instance.Error($"{pathStr}->{path.Key}:{value}");
                                Logger.Instance.Error(ex);
                                return false;
                            }
                        }
                    }
                    Updated();
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                    return false;
                }
            }
            return true;
        }

        private int registryUpdated = 0;
        private int registryUpdateFlag = 1;
        private void Updated()
        {
            changed = true;
            Interlocked.Exchange(ref registryUpdated, 1);
        }
        public void Refresh()
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
        public string GetValues()
        {
            if (OperatingSystem.IsWindows() && GetSid() && changed)
            {
                changed = false;
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
                        try
                        {

                            object obj = Registry.GetValue(path, pathItem.Key, null);

                            values[i] = '0';
                            if (obj != null)
                            {
                                values[i] = obj.ToString() == pathItem.DisallowValue ? '1' : '0';
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Error($"get values {path}->{pathItem.Key}");
                            Logger.Instance.Error(ex);
                        }
                    }
                }
            }
            return new string(values);
        }
        public bool GetValue(string key, bool allow)
        {
            bool _allow = false;
            if (OperatingSystem.IsWindows() && GetSid())
            {
                RegistryOptionPathInfo[] paths = GetPaths(key);
                for (int i = 0; i < paths.Length; i++)
                {
                    object obj = Registry.GetValue(paths[i].Path, paths[i].Key, null);
                    if (obj != null)
                    {
                        _allow |= obj.ToString() == (allow ? paths[i].AllowValue : paths[i].DisallowValue);
                    }
                }
            }
            return _allow;
        }
        public bool UpdateValue(SystemOptionUpdateInfo[] keys)
        {
            bool result = false;
            if (GetSid() == false)
            {
                return result;
            }

            if (OperatingSystem.IsWindows())
            {
                foreach (SystemOptionUpdateInfo item in keys)
                {
                    RegistryOptionInfo info = Infos.FirstOrDefault(c => c.Key == item.Key);
                    if (info == null)
                    {
                        continue;
                    }

                    foreach (RegistryOptionPathInfo pathItem in info.Paths)
                    {
                        string path = ReplaceRegistryPath(pathItem.Path);
                        if (string.IsNullOrWhiteSpace(path))
                        {
                            continue;
                        }
                        string setValue = item.Value ? pathItem.DisallowValue : pathItem.AllowValue;
                        try
                        {
                            object oldValue = Registry.GetValue(path, pathItem.Key, null);
                            if (oldValue == null || oldValue.ToString() != setValue)
                            {
                                Registry.SetValue(path, pathItem.Key, int.Parse(setValue), RegistryValueKind.DWord);
                                result |= true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Error($"update value {path}->{pathItem.Key}");
                            Logger.Instance.Error(ex);
                        }
                    }
                }

            }
            if (result)
            {
                Updated();
            }
            return result;
        }

        private string ReplaceRegistryPath(string path)
        {
            if (!string.IsNullOrWhiteSpace(currentUserSid))
            {
                return path.Replace("HKEY_CURRENT_USER", $"HKEY_USERS\\{currentUserSid}");
            }
            return string.Empty;
        }
        public bool GetSid()
        {
            if (string.IsNullOrWhiteSpace(currentUserSid) == false)
            {
                return true;
            }

            currentUserSid = Win32Interop.GetCurrentUserSid();
            if (string.IsNullOrWhiteSpace(currentUserSid) == false)
            {
                Logger.Instance.Error($"current user sid {currentUserSid}");
                return true;
            }

            currentUserSid = Win32Interop.GetDefaultUserSid();
            if (string.IsNullOrWhiteSpace(currentUserSid) == false)
            {
                Logger.Instance.Error($"default user sid {currentUserSid}");
                return true;
            }


            return string.IsNullOrWhiteSpace(currentUserSid) == false;
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
                Desc="U盘禁用",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\USBSTOR", Key="Start", DisallowValue="4", AllowValue="3" },
                     new RegistryOptionPathInfo{ Path="HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\StorageDevicePolicies", Key="WriteProtect", DisallowValue="1", AllowValue="0" }
                }
            },
             new RegistryOptionInfo{
                Key="TimeSync",
                Desc="时间同步",
                Paths = new RegistryOptionPathInfo[]{
                     new RegistryOptionPathInfo{ Path="HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", Key="Snltty_TimeSync", DisallowValue="0", AllowValue="1" }
                }
            }
        };
        public RegistryOptionPathInfo[] GetPaths(string key)
        {
            RegistryOptionInfo info = Infos.FirstOrDefault(c => c.Key == key);
            if (info != null)
            {
                return info.Paths;
            }
            return Array.Empty<RegistryOptionPathInfo>();
        }

        public sealed class RegistryOptionInfo
        {
            public string Key { get; set; } = string.Empty;
            public string Desc { get; set; } = string.Empty;
            public RegistryOptionPathInfo[] Paths { get; set; }
        }
        public sealed class RegistryOptionPathInfo
        {
            public string Path { get; set; }
            public string Key { get; set; }
            public string DisallowValue { get; set; }
            public string AllowValue { get; set; }
        }
    }
}
