using cmonitor.server.client.reports.screen.winapis;
using cmonitor.server.client.reports.screen.winapiss;
using common.libs;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Text;
using System.Xml.Linq;
using static cmonitor.server.client.reports.screen.winapis.ADVAPI32;
using static cmonitor.server.client.reports.screen.winapiss.User32;

namespace cmonitor.server.client.reports.screen
{
    public class Win32Interop
    {
        private static nint _lastInputDesktop;

        public static List<WindowsSession> GetActiveSessions()
        {
            var sessions = new List<WindowsSession>();
            var consoleSessionId = Kernel32.WTSGetActiveConsoleSessionId();
            sessions.Add(new WindowsSession()
            {
                Id = consoleSessionId,
                Type = WindowsSessionType.Console,
                Name = "Console",
                Username = GetUsernameFromSessionId(consoleSessionId)
            });

            nint ppSessionInfo = nint.Zero;
            var count = 0;
            var enumSessionResult = WTSAPI32.WTSEnumerateSessions(WTSAPI32.WTS_CURRENT_SERVER_HANDLE, 0, 1, ref ppSessionInfo, ref count);
            var dataSize = Marshal.SizeOf(typeof(WTSAPI32.WTS_SESSION_INFO));
            var current = ppSessionInfo;

            if (enumSessionResult != 0)
            {
                for (int i = 0; i < count; i++)
                {
                    var wtsInfo = Marshal.PtrToStructure(current, typeof(WTSAPI32.WTS_SESSION_INFO));
                    if (wtsInfo is null)
                    {
                        continue;
                    }
                    var sessionInfo = (WTSAPI32.WTS_SESSION_INFO)wtsInfo;
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

        public static string GetCommandLine()
        {
            var commandLinePtr = Kernel32.GetCommandLine();
            return Marshal.PtrToStringAuto(commandLinePtr) ?? string.Empty;
        }

        public static bool GetCurrentDesktop(out string desktopName)
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

        public static string GetUsernameFromSessionId(uint sessionId)
        {
            var username = string.Empty;

            if (WTSAPI32.WTSQuerySessionInformation(nint.Zero, sessionId, WTSAPI32.WTS_INFO_CLASS.WTSUserName, out var buffer, out var strLen) && strLen > 1)
            {
                username = Marshal.PtrToStringAnsi(buffer);
                WTSAPI32.WTSFreeMemory(buffer);
            }

            return username ?? string.Empty;
        }

        public static nint OpenInputDesktop()
        {
            return User32.OpenInputDesktop(0, true, ACCESS_MASK.GENERIC_ALL);
        }

        public static bool CreateInteractiveSystemProcess(
            string commandLine,
             int targetSessionId,
             bool forceConsoleSession,
             string desktopName,
             bool hiddenWindow,
             out PROCESS_INFORMATION procInfo)
        {
            uint winlogonPid = 0;
            var hUserTokenDup = nint.Zero;
            var hPToken = nint.Zero;
            var hProcess = nint.Zero;

            procInfo = new PROCESS_INFORMATION();

            // If not force console, find target session.  If not present,
            // use last active session.
            var dwSessionId = Kernel32.WTSGetActiveConsoleSessionId();
            if (!forceConsoleSession)
            {
                var activeSessions = GetActiveSessions();
                if (activeSessions.Any(x => x.Id == targetSessionId))
                {
                    dwSessionId = (uint)targetSessionId;
                }
                else
                {
                    dwSessionId = activeSessions.Last().Id;
                }
            }

            // Obtain the process ID of the winlogon process that is running within the currently active session.
            var processes = Process.GetProcessesByName("winlogon");
            foreach (Process p in processes)
            {
                if ((uint)p.SessionId == dwSessionId)
                {
                    winlogonPid = (uint)p.Id;
                }
            }
            // Obtain a handle to the winlogon process.
            hProcess = Kernel32.OpenProcess(MAXIMUM_ALLOWED, false, winlogonPid);

            // Obtain a handle to the access token of the winlogon process.
            if (!OpenProcessToken(hProcess, TOKEN_DUPLICATE, ref hPToken))
            {
                Kernel32.CloseHandle(hProcess);
                return false;
            }

            // Security attibute structure used in DuplicateTokenEx and CreateProcessAsUser.
            var sa = new SECURITY_ATTRIBUTES();
            sa.Length = Marshal.SizeOf(sa);

            // Copy the access token of the winlogon process; the newly created token will be a primary token.
            if (!DuplicateTokenEx(hPToken, MAXIMUM_ALLOWED, ref sa, SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, TOKEN_TYPE.TokenPrimary, out hUserTokenDup))
            {
                Kernel32.CloseHandle(hProcess);
                Kernel32.CloseHandle(hPToken);
                return false;
            }

            // By default, CreateProcessAsUser creates a process on a non-interactive window station, meaning
            // the window station has a desktop that is invisible and the process is incapable of receiving
            // user input. To remedy this we set the lpDesktop parameter to indicate we want to enable user 
            // interaction with the new process.
            var si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.lpDesktop = @"winsta0\" + desktopName;

            // Flags that specify the priority and creation method of the process.
            uint dwCreationFlags;
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

            // Create a new process in the current user's logon session.
            var result = CreateProcessAsUser(
                hUserTokenDup,
                null,
                commandLine,
                ref sa,
                ref sa,
                false,
                dwCreationFlags,
                nint.Zero,
                null,
                ref si,
                out procInfo);
            uint code = Kernel32.GetLastError();

            // Invalidate the handles.
            Kernel32.CloseHandle(hProcess);
            Kernel32.CloseHandle(hPToken);
            Kernel32.CloseHandle(hUserTokenDup);

            return result;
        }

        public static void SetMonitorState(MonitorState state)
        {
            SendMessage(0xFFFF, 0x112, 0xF170, (int)state);
        }

        public static MessageBoxResult ShowMessageBox(nint owner, string message, string caption, MessageBoxType messageBoxType)
        {
            return (MessageBoxResult)MessageBox(owner, message, caption, (long)messageBoxType);
        }

        public static bool SwitchToInputDesktop()
        {
            try
            {
                CloseDesktop(_lastInputDesktop);

                nint inputDesktop = OpenInputDesktop();
                if (inputDesktop == nint.Zero)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        Logger.Instance.Error($"OpenInputDesktop fail");
                    return false;
                }

                bool result = SetThreadDesktop(inputDesktop);
                if (result == false)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        Logger.Instance.Error($"SetThreadDesktop fail");
                }
                result &= SwitchDesktop(inputDesktop);
                if (result == false)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        Logger.Instance.Error($"SwitchDesktop fail");
                }

                _lastInputDesktop = inputDesktop;
                return result;
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
                return false;
            }
        }

        public static void SetConsoleWindowVisibility(bool isVisible)
        {
            var handle = Kernel32.GetConsoleWindow();

            if (isVisible)
            {
                ShowWindow(handle, (int)SW.SW_SHOW);
            }
            else
            {
                ShowWindow(handle, (int)SW.SW_HIDE);
            }

            Kernel32.CloseHandle(handle);
        }


        public static void RelaunchElevated()
        {
            if (OperatingSystem.IsWindows() == false) return;

            try
            {
                AddTokenPrivilege();
            }
            catch
            {
            }
            try
            {
                string commandLine = Win32Interop.GetCommandLine();
                bool result = Win32Interop.CreateInteractiveSystemProcess($"{commandLine} --elevated", -1, false, "default", true, out PROCESS_INFORMATION procInfo);
                uint code = Kernel32.GetLastError();
                if (result)
                {
                    Environment.Exit(0);
                }
            }
            catch
            {
            }
        }

        public static void AddTokenPrivilege()
        {
            if (OperatingSystem.IsWindows())
            {
                WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();
                CommandHelper.Windows(string.Empty, new string[] {
                    $"ntrights +r SeAssignPrimaryTokenPrivilege -u {windowsIdentity.Name}"
                });
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
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    tokenInformation = Marshal.AllocHGlobal(returnLength);
                    try
                    {
                        if (!GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenUser, tokenInformation, returnLength, out returnLength))
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
        public static bool IsSystemUser()
        {
            return currentUsername == "NT AUTHORITY\\SYSTEM";
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


}
