namespace llock.win
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

            string shareMkey = arg[0];
            int shareMLength = int.Parse(arg[1]);
            int shareItemMLength = int.Parse(arg[2]);
            int shareIndex = int.Parse(arg[3]);

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
}