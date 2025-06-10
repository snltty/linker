using linker.libs.winapis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using static linker.libs.winapis.WTSAPI32;
using static linker.libs.winapis.ADVAPI32;
using static linker.libs.winapis.Kernel32;
using static linker.libs.winapis.NetApi32;
using static linker.libs.winapis.User32;

namespace linker.libs.winapis
{
    public class Win32Interop
    {
        private static nint lastInputDesktop;

        private static bool GetCurrentDesktop(out string desktopName)
        {
            var inputDesktop = OpenInputDesktop();
            try
            {
                byte[] deskBytes = new byte[256];
                if (!GetUserObjectInformationW(inputDesktop, UOI_NAME, deskBytes, 256, out uint lenNeeded))
                {
                    desktopName = string.Empty;
                    return false;
                }

                desktopName = Encoding.Unicode.GetString(deskBytes.Take((int)lenNeeded).ToArray()).Replace("\0", "");
                return true;
            }
            finally
            {
                CloseDesktop(inputDesktop);
            }
        }
        private static List<WindowsSession> GetActiveSessions()
        {
            List<WindowsSession> sessions = new List<WindowsSession>();
            uint consoleSessionId = Kernel32.WTSGetActiveConsoleSessionId();
            sessions.Add(new WindowsSession()
            {
                Id = consoleSessionId,
                Type = WindowsSessionType.Console,
                Name = "Console",
                Username = GetUsernameFromSessionId(consoleSessionId)
            });

            nint ppSessionInfo = nint.Zero;
            int count = 0;
            int enumSessionResult = WTSAPI32.WTSEnumerateSessions(WTSAPI32.WTS_CURRENT_SERVER_HANDLE, 0, 1, ref ppSessionInfo, ref count);
            int dataSize = Marshal.SizeOf(typeof(WTSAPI32.WTS_SESSION_INFO));
            nint current = ppSessionInfo;

            if (enumSessionResult != 0)
            {
                for (int i = 0; i < count; i++)
                {
                    object wtsInfo = Marshal.PtrToStructure(current, typeof(WTSAPI32.WTS_SESSION_INFO));
                    if (wtsInfo is null)
                    {
                        continue;
                    }
                    WTSAPI32.WTS_SESSION_INFO sessionInfo = (WTSAPI32.WTS_SESSION_INFO)wtsInfo;
                    current += dataSize;
                    if (sessionInfo.State == WTSAPI32.WTS_CONNECTSTATE_CLASS.WTSActive && sessionInfo.SessionID != consoleSessionId)
                    {

                        sessions.Add(new WindowsSession()
                        {
                            Id = sessionInfo.SessionID,
                            Name = sessionInfo.pWinStationName,
                            Type = WindowsSessionType.RDP,
                            Username = GetUsernameFromSessionId(sessionInfo.SessionID)
                        });
                    }
                }
            }

            return sessions;
        }
        private static string GetUsernameFromSessionId(uint sessionId)
        {
            var username = string.Empty;

            if (WTSAPI32.WTSQuerySessionInformation(nint.Zero, sessionId, WTSAPI32.WTS_INFO_CLASS.WTSUserName, out var buffer, out var strLen) && strLen > 1)
            {
                username = Marshal.PtrToStringAnsi(buffer);
                WTSAPI32.WTSFreeMemory(buffer);
            }

            return username ?? string.Empty;
        }

        private static nint OpenInputDesktop()
        {
            return User32.OpenInputDesktop(0, true, User32.ACCESS_MASK.GENERIC_ALL);
        }
        public static bool SwitchToInputDesktop()
        {
            try
            {
                CloseDesktop(lastInputDesktop);

                nint inputDesktop = OpenInputDesktop();
                if (inputDesktop == nint.Zero)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"OpenInputDesktop fail");
                    return false;
                }

                bool result = SetThreadDesktop(inputDesktop);
                if (result == false)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"SetThreadDesktop fail");
                }
                result &= SwitchDesktop(inputDesktop);
                if (result == false)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"SwitchDesktop fail");
                }

                lastInputDesktop = inputDesktop;
                return result;
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                return false;
            }
        }


        private static uint GetWinLogonPid(uint dwSessionId)
        {
            uint winlogonPid = 0;
            Process[] processes = Process.GetProcessesByName("winlogon");
            foreach (Process p in processes)
            {
                if ((uint)p.SessionId == dwSessionId)
                {
                    winlogonPid = (uint)p.Id;
                }
            }
            return winlogonPid;
        }
        private static uint GetDwSessionId(int targetSessionId, bool forceConsoleSession)
        {
            uint dwSessionId = Kernel32.WTSGetActiveConsoleSessionId();
            if (forceConsoleSession == false)
            {
                List<WindowsSession> activeSessions = GetActiveSessions();
                if (activeSessions.Any(x => x.Id == targetSessionId))
                {
                    dwSessionId = (uint)targetSessionId;
                }
                else
                {
                    dwSessionId = activeSessions.Last().Id;
                }
            }
            return dwSessionId;
        }
        private static STARTUPINFO GetStartUpInfo(bool hiddenWindow, string desktopName, out uint dwCreationFlags)
        {
            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.lpDesktop = @"winsta0\" + desktopName;
            if (hiddenWindow)
            {
                dwCreationFlags = NORMAL_PRIORITY_CLASS | CREATE_UNICODE_ENVIRONMENT | CREATE_NO_WINDOW;
                si.dwFlags = STARTF_USESHOWWINDOW;
                si.wShowWindow = 0;
            }
            else
            {
                dwCreationFlags = NORMAL_PRIORITY_CLASS | CREATE_UNICODE_ENVIRONMENT | CREATE_NEW_CONSOLE;
            }
            return si;
        }
        private static bool CreateInteractiveSystemProcess(string commandLine, int targetSessionId, bool forceConsoleSession, string desktopName, bool hiddenWindow, out PROCESS_INFORMATION procInfo)
        {
            nint hPToken = nint.Zero;
            procInfo = new PROCESS_INFORMATION();

            uint dwSessionId = GetDwSessionId(targetSessionId, forceConsoleSession);
            uint winlogonPid = GetWinLogonPid(dwSessionId);

            nint hProcess = Kernel32.OpenProcess(MAXIMUM_ALLOWED, false, winlogonPid);
            if (OpenProcessToken(hProcess, TOKEN_DUPLICATE, ref hPToken) == false)
            {
                Kernel32.CloseHandle(hProcess);
                return false;
            }

            SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
            sa.Length = Marshal.SizeOf(sa);
            if (DuplicateTokenEx(hPToken, MAXIMUM_ALLOWED, ref sa, SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, TOKEN_TYPE.TokenPrimary, out nint hUserTokenDup) == false)
            {
                Kernel32.CloseHandle(hProcess);
                Kernel32.CloseHandle(hPToken);
                return false;
            }

            STARTUPINFO si = GetStartUpInfo(hiddenWindow, desktopName, out uint dwCreationFlags);
            bool result = CreateProcessAsUser(hUserTokenDup, null, commandLine, ref sa, ref sa, false, dwCreationFlags, nint.Zero, null, ref si, out procInfo);

            Kernel32.CloseHandle(hProcess);
            Kernel32.CloseHandle(hPToken);
            Kernel32.CloseHandle(hUserTokenDup);

            return result;
        }

        public static string GetCommandLine()
        {
            nint commandLinePtr = Kernel32.GetCommandLine();
            return Marshal.PtrToStringAuto(commandLinePtr) ?? string.Empty;
        }
        
        public static void RelaunchElevated()
        {
            if (OperatingSystem.IsWindows() == false) return;

            try
            {
                string commandLine = GetCommandLine();
                bool result = CreateInteractiveSystemProcess($"{commandLine} --elevated", -1, false, "default", true, out PROCESS_INFORMATION procInfo);
                uint code = Kernel32.GetLastError();
                if (result)
                {
                    Environment.Exit(0);
                }
            }
            catch(Exception ex)
            {
                LoggerHelper.Instance.Debug($"CreateInteractiveSystemProcess {ex}");
            }
        }
        


        private static string currentUsername = string.Empty;
        public static string GetCurrentUserSid()
        {
            if (OperatingSystem.IsWindows() == false)
            {
                return string.Empty;
            }
            if (OperatingSystem.IsWindows())
            {
                WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
                currentUsername = currentIdentity.Name;
                if (IsSystemUser() == false)
                {
                    return currentIdentity.User.Value;
                }
            }

            IntPtr hToken;
            int sessionId = (int)Kernel32.WTSGetActiveConsoleSessionId();
            if (WTSAPI32.WTSQueryUserToken(sessionId, out hToken))
            {
                try
                {
                    IntPtr tokenInformation;
                    int returnLength;
                    if (GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenUser, IntPtr.Zero, 0, out returnLength) || returnLength == 0)
                    {
                        return string.Empty;
                    }

                    tokenInformation = Marshal.AllocHGlobal(returnLength);
                    try
                    {
                        if (GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenUser, tokenInformation, returnLength, out returnLength) == false)
                        {
                            return string.Empty;
                        }

                        var user = (TOKEN_USER)Marshal.PtrToStructure(tokenInformation, typeof(TOKEN_USER));
                        string stringSid;
                        if (ConvertSidToStringSid(user.User.Sid, out stringSid))
                        {
                            return stringSid;
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(tokenInformation);
                    }
                }
                finally
                {
                    if (hToken != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(hToken);
                    }
                }
            }
            return string.Empty;
        }
        public static string GetDefaultUserSid()
        {
            if (OperatingSystem.IsWindows() == false)
            {
                return string.Empty;
            }

            List<WindowUserInfo> users = new List<WindowUserInfo>();
            int resumeHandle = 0;
            int result = NetUserEnum(null, 0, 2, out IntPtr bufPtr, -1, out int entriesRead, out int totalEntries, ref resumeHandle);
            if (result == 0)
            {
                try
                {
                    for (int i = 0; i < entriesRead; i++)
                    {
                        USER_INFO_0 userInfo = (USER_INFO_0)Marshal.PtrToStructure(bufPtr + (Marshal.SizeOf(typeof(USER_INFO_0)) * i), typeof(USER_INFO_0));

                        int cbSid = 0, cchReferencedDomainName = 0;
                        StringBuilder referencedDomainName = new StringBuilder();
                        IntPtr pSid = IntPtr.Zero;
                        bool bSuccess = LookupAccountName(null, userInfo.usri0_name, pSid, ref cbSid, referencedDomainName, ref cchReferencedDomainName, out int peUse);
                        if (bSuccess == false && cbSid > 0)
                        {
                            pSid = Marshal.AllocHGlobal(cbSid);
                            referencedDomainName.EnsureCapacity(cchReferencedDomainName);
                            bSuccess = LookupAccountName(null, userInfo.usri0_name, pSid, ref cbSid, referencedDomainName, ref cchReferencedDomainName, out peUse);
                        }
                        if (bSuccess == false || ConvertSidToStringSid(pSid, out string stringSid) == false) continue;
                        if (pSid != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(pSid);
                        }


                        if (NetUserGetInfo(null, userInfo.usri0_name, 3, out IntPtr bufptr) != NERR_Success)
                        {
                            continue;
                        }
                        USER_INFO_3 info = (USER_INFO_3)Marshal.PtrToStructure(bufptr, typeof(USER_INFO_3));
                        if (info.LastLogon > 0)
                        {
                            users.Add(new WindowUserInfo { LastLogon = info.LastLogon, Sid = stringSid });
                        }
                       int value = NetApiBufferFree(bufptr);
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                   int _ = NetApiBufferFree(bufPtr);
                }
            }

            if (users.Count > 0)
            {
                return users.OrderByDescending(c => c.LastLogon).FirstOrDefault().Sid;
            }
            return string.Empty;
        }
        public static bool IsSystemUser()
        {
            return currentUsername == "NT AUTHORITY\\SYSTEM";
        }

        public static string GetUserName()
        {
            // 获取活动的控制台会话 ID
            uint sessionId = WTSGetActiveConsoleSessionId();

            IntPtr buffer;
            uint bytesReturned;
            string userName = "";

            // 获取用户名
            if (WTSQuerySessionInformation(IntPtr.Zero, sessionId, WTS_INFO_CLASS.WTSUserName, out buffer, out bytesReturned))
            {
                userName = Marshal.PtrToStringAnsi(buffer);
                WTSFreeMemory(buffer);
            }

            return userName;
        }

        public static void SetHandleBlockKill(IntPtr handle)
        {
            const int HANDLE_FLAG_PROTECT_FROM_CLOSE = 0x1;
            Kernel32.SetHandleInformation(handle, HANDLE_FLAG_PROTECT_FROM_CLOSE, HANDLE_FLAG_PROTECT_FROM_CLOSE);
        }

        public static async Task<DateTime> GetNetworkTime()
        {
            // 定义 NTP 服务器地址
            string ntpServer = "time.windows.com";

            // 创建 UDP 客户端
            using UdpClient client = new UdpClient();

            // 连接到 NTP 服务器
            IPAddress ntpServerAddress = Dns.GetHostEntry(ntpServer).AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ntpServerAddress, 123);
            client.Connect(endPoint);

            // 构造 NTP 请求包
            byte[] ntpData = new byte[48];
            ntpData[0] = 0x1B;

            // 发送请求包并接收响应
            await client.SendAsync(ntpData, ntpData.Length).ConfigureAwait(false);
            byte[] responseData = client.Receive(ref endPoint);

            // 关闭 UDP 客户端
            client.Close();

            // 解析 NTP 响应包，提取时间信息
            ulong seconds = (ulong)responseData[40] << 24 | (ulong)responseData[41] << 16 | (ulong)responseData[42] << 8 | (ulong)responseData[43];
            ulong fraction = (ulong)responseData[44] << 24 | (ulong)responseData[45] << 16 | (ulong)responseData[46] << 8 | (ulong)responseData[47];

            // NTP 时间戳的起始时间是 1900 年 1 月 1 日
            DateTime ntpEpoch = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // 计算 NTP 响应时间
            ulong milliseconds = (seconds * 1000 + (fraction * 1000) / 0x100000000L);
            DateTime networkTime = ntpEpoch.AddMilliseconds((long)milliseconds);

            // 转换为本地时间
            DateTime localTime = networkTime.ToLocalTime();

            return networkTime;

        }
        public static void SetSystemTime(DateTime dateTime)
        {
            SYSTEMTIME st = new SYSTEMTIME
            {
                wYear = (ushort)dateTime.Year,
                wMonth = (ushort)dateTime.Month,
                wDay = (ushort)dateTime.Day,
                wHour = (ushort)dateTime.Hour,
                wMinute = (ushort)dateTime.Minute,
                wSecond = (ushort)dateTime.Second,
                wMilliseconds = (ushort)dateTime.Millisecond
            };
            Kernel32.SetSystemTime(ref st);
        }

    }


    [DataContract]
    public enum WindowsSessionType
    {
        Console = 1,
        RDP = 2
    }

    [DataContract]
    public class WindowsSession
    {
        [DataMember(Name = "ID")]
        public uint Id { get; set; }
        [DataMember(Name = "Name")]
        public string Name { get; set; } = string.Empty;
        [DataMember(Name = "Type")]
        public WindowsSessionType Type { get; set; }
        [DataMember(Name = "Username")]
        public string Username { get; set; } = string.Empty;
    }

    public struct WindowUserInfo
    {
        public int LastLogon;
        public string Sid;
    }


}
