using System.Net;
using System.Text.Json;

namespace cmonitor.viewer.server.win
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

            AppDomain.CurrentDomain.UnhandledException += (a, b) =>
            {
                MessageBox.Show(b.ExceptionObject.ToString());
            };

            ParamInfo paramInfo = new ParamInfo();
            if (arg.Length > 0)
            {
                paramInfo = JsonSerializer.Deserialize<ParamInfo>(arg[0]);
            }
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm(paramInfo));
        }

        
    }

    public sealed class ParamInfo
    {
        public string ShareMkey { get; set; } = "cmonitor/share";
        public int ShareMLength { get; set; } = 10;
        public int ShareItemMLength { get; set; } = 1024;
        public int ShareIndex { get; set; } = 5;
        public Mode Mode { get; set; } = Mode.Server;
        public string GroupName { get; set; } = "snltty";
        public string ProxyServers { get; set; } = "127.0.0.1:1803";
    }
}