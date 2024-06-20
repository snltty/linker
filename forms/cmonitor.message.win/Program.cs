namespace cmonitor.message.win
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Mutex mutex = new Mutex(true, System.Diagnostics.Process.GetCurrentProcess().ProcessName, out bool isAppRunning);
            if (isAppRunning == false)
            {
                Environment.Exit(1);
            }

            AppDomain.CurrentDomain.UnhandledException += (a, b) =>
            {
            };


            string msg = "上课时间，请注意课堂纪律！";
            int times = 10;
            if (args.Length > 0)
            {
                msg = args[0];
                if (args.Length > 1)
                {
                    if (int.TryParse(args[1], out times) == false)
                    {
                        times = 10;
                    }
                }
            }

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm(msg, times));
        }
    }
}