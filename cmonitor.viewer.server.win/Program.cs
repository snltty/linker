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

            string shareMkey = "cmonitor/share";
            int shareMLength = 10;
            int shareItemMLength = 1024;
            int shareIndex = 5;
            Mode mode = Mode.Server;
            if (arg.Length > 0)
            {
                shareMkey = arg[0];
                shareMLength = int.Parse(arg[1]);
                shareItemMLength = int.Parse(arg[2]);
                shareIndex = int.Parse(arg[3]);
                mode = (Mode)byte.Parse(arg[4]);
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm(shareMkey, shareMLength, shareItemMLength, shareIndex, mode));
        }
    }
}