using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace cmonitor.llock.win
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] arg)
        {
            Mutex mutex = new Mutex(true, System.Diagnostics.Process.GetCurrentProcess().ProcessName, out bool isAppRunning);
            if (isAppRunning == false)
            {
                Environment.Exit(1);
            }
            //ProcessProtection.ProtectProcess();

            AppDomain.CurrentDomain.UnhandledException += (a, b) =>
            {
            };

            string shareMkey = "cmonitor/share";
            int shareMLength = 100;
            int shareItemMLength = 10240;
            int shareIndex = 3;

            if (arg.Length > 0)
            {
                shareMkey = arg[0];
                shareMLength = int.Parse(arg[1]);
                shareItemMLength = int.Parse(arg[2]);
                shareIndex = int.Parse(arg[3]);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(shareMkey, shareMLength, shareItemMLength, shareIndex));
        }
    }

    class ProcessProtection
    {
        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtSetInformationProcess(IntPtr hProcess, int processInformationClass, ref int processInformation, int processInformationLength);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern IntPtr RtlAdjustPrivilege(int Privilege, bool bEnablePrivilege, bool IsThreadPrivilege, out bool PreviousValue);

        [DllImport("ntdll.dll")]
        private static extern uint NtRaiseHardError(
        uint ErrorStatus,
        uint NumberOfParameters,
        uint UnicodeStringParameterMask,
        IntPtr Parameters,
        uint ValidResponseOption,
        out uint Response
        );

        private static bool IsDebugMode = false;

        public static void ProtectProcess()
        {
            int isCritical = 1;
            int BreakOnTermination = 0x1D;
            if (!IsDebugMode)
            {
                Process.EnterDebugMode();
                IsDebugMode = true;
            }
            NtSetInformationProcess(Process.GetCurrentProcess().Handle, BreakOnTermination, ref isCritical, sizeof(int));
        }

        public static void ProtectProcess(Process target)
        {
            int isCritical = 1;
            int BreakOnTermination = 0x1D;
            if (!IsDebugMode)
            {
                Process.EnterDebugMode();
                IsDebugMode = true;
            }
            NtSetInformationProcess(target.Handle, BreakOnTermination, ref isCritical, sizeof(int));
        }

        public static void UnprotectProcess()
        {
            int isCritical = 0;
            int BreakOnTermination = 0x1D;
            if (!IsDebugMode)
            {
                Process.EnterDebugMode();
                IsDebugMode = true;
            }
            NtSetInformationProcess(Process.GetCurrentProcess().Handle, BreakOnTermination, ref isCritical, sizeof(int));
        }

        public static void UnprotectProcess(Process target)
        {
            int isCritical = 0;
            int BreakOnTermination = 0x1D;
            if (!IsDebugMode)
            {
                Process.EnterDebugMode();
                IsDebugMode = true;
            }
            NtSetInformationProcess(target.Handle, BreakOnTermination, ref isCritical, sizeof(int));
        }

        public static void BSOD()
        {
            bool b;
            uint response;
            uint STATUS_ASSERTION_FAILURE = 0xC0000420;
            RtlAdjustPrivilege(19, true, false, out b);
            NtRaiseHardError(STATUS_ASSERTION_FAILURE, 0, 0, IntPtr.Zero, 6, out response);
        }
    }
}