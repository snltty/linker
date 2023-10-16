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
            int star = 1;
            if(args.Length > 1)
            {
                speed = int.Parse(args[0]);
                msg = args[1];
                star = int.Parse(args[2]);
            }

            Application.Run(new Form1(speed,msg, star));
        }
    }
}
