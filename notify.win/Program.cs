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

            int speed = int.Parse(args[0]);
            string msg = args[1];

            Application.Run(new Form1(speed,msg));
        }
    }
}
