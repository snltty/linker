namespace cmonitor.snatch.win
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


            string shareMkey = "cmonitor/share";
            int shareMLength = 10;
            int shareItemMLength = 1024;
            int shareQuestionIndex = 5;
            int shareAnswerIndex = 6;
            if (arg.Length > 0)
            {
                shareMkey = arg[0];
                shareMLength = int.Parse(arg[1]);
                shareItemMLength = int.Parse(arg[2]);
                shareQuestionIndex = int.Parse(arg[3]);
                shareAnswerIndex = int.Parse(arg[4]);
            }


            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm(shareMkey, shareMLength, shareItemMLength, shareQuestionIndex, shareAnswerIndex));
        }
    }
}