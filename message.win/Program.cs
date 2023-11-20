using System;
using System.Threading;
using System.Windows.Forms;

namespace message.win
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
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


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(msg, times));
        }
    }
}
