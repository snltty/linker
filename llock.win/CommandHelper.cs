using System.Diagnostics;
using System.IO;

namespace llock.win
{
    public sealed class CommandHelper
    {
        public static void Windows(string arg, string[] commands)
        {
            Execute("cmd.exe", arg, commands);
        }
        public static void Execute(string fileName, string arg, string[] commands)
        {
            Process proc = new Process();
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.FileName = fileName;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.Arguments = arg;
            proc.StartInfo.Verb = "runas";
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.Start();

            if (commands.Length > 0)
            {
                for (int i = 0; i < commands.Length; i++)
                {
                    proc.StandardInput.WriteLine(commands[i]);
                }
            }

            proc.StandardInput.AutoFlush = true;
            proc.StandardInput.WriteLine("exit");
            proc.StandardInput.Close();
        }
    }
}
