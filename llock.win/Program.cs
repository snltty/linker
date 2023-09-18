using System;
using System.Threading;
using System.Windows.Forms;

namespace llock.win
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] arg)
        {
            Mutex mutex = new Mutex(true, System.Diagnostics.Process.GetCurrentProcess().ProcessName, out bool isAppRunning);
            if (isAppRunning == false)
            {
                Environment.Exit(1);
            }

            string shareMkey = arg[0];
            int shareMLength = int.Parse(arg[1]);
            int shareIndex= int.Parse(arg[2]);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(shareMkey, shareMLength, shareIndex));
        }
    }
}
