namespace cmonitor.wallpaper.win
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

            string imgUrl = "./bg.jpg";
            string shareMkey = "cmonitor/share";
            int shareMLength = 10;
            int shareItemMLength = 1024;
            int shareKeyBoardIndex = 1;
            int shareWallpaperIndex = 2;
            if (arg.Length > 0)
            {
                imgUrl = arg[0];
                shareMkey = arg[1];
                shareMLength = int.Parse(arg[2]);
                shareItemMLength = int.Parse(arg[3]);
                shareKeyBoardIndex = int.Parse(arg[4]);
                shareWallpaperIndex = int.Parse(arg[5]);
            }

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm(imgUrl, shareMkey, shareMLength, shareItemMLength, shareKeyBoardIndex, shareWallpaperIndex));
        }
    }
}