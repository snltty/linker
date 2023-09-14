using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace wallpaper.win
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

            string img = "https://www.qbcode.cn/images/carousel-cpp.jpg";
            string key = "cmonitor/keyboard";
            int len = 1024;
            if(arg.Length > 0)
            {
                img = arg[0];
            }
            if (arg.Length > 1)
            {
                key = arg[1];
            }
            if (arg.Length > 2)
            {
                len = int.Parse(arg[2]);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(img, key, len));
        }
    }
}
