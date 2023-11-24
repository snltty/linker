namespace notify.win
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (a, b) =>
            {
            };

            int speed = 1;
            string msg = "ÉÙÄêÀÉÍºÍ·Ñ½";
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

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(speed, msg, star1, star2, star3));
        }
    }
}