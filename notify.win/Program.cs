using System;
using System.Windows.Forms;

namespace notify.win
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            int speed = 1;
            string msg = "少年郎秃头呀";
            int star1 = 1;
            int star2 = 1;
            int star3 = 1;
            if (args.Length > 1)
            {
                speed = int.Parse(args[0]);
                msg = args[1];
                star1 = int.Parse(args[2]);
                star2 = int.Parse(args[3]);
                star3 = int.Parse(args[4]);
            }

            Application.Run(new Form1(speed,msg, star1, star2, star3));
        }
    }
}
