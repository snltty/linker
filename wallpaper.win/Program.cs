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

            string imgUrl = "./bg.jpg";
            string shareMkey = "test";
            int shareMLength = 2550;
            int shareKeyBoardIndex = 0;
            int shareWallpaperIndex = 1;
            if (arg.Length > 0)
            {
                imgUrl = arg[0];
                shareMkey = arg[1];
                shareMLength = int.Parse(arg[2]);
                shareKeyBoardIndex = int.Parse(arg[3]);
                shareWallpaperIndex = int.Parse(arg[4]);
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(imgUrl, shareMkey, shareMLength, shareKeyBoardIndex, shareWallpaperIndex));
        }
    }
}
