namespace cmonitor.share.win
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
            };

            string shareMkey = "cmonitor/share/screen";
            int shareItemMLength = 2 * 1024 * 1024;
            if (arg.Length > 0)
            {
                shareMkey = arg[0];
                shareItemMLength = int.Parse(arg[1]);
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm(shareMkey, shareItemMLength));
        }
    }
}