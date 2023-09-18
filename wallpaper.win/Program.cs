using System;
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

            string imgUrl = arg[0];
            string shareMkey = arg[1];
            int shareMLength = int.Parse(arg[2]);
            int shareKeyBoardIndex = int.Parse(arg[3]);
            int shareWallpaperIndex = int.Parse(arg[4]);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(imgUrl, shareMkey, shareMLength, shareKeyBoardIndex, shareWallpaperIndex));
        }
    }
}
