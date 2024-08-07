using System;
using System.Diagnostics;
using System.IO;

namespace linker.libs
{
    public sealed class CommandHelper
    {
        public static string Windows(string arg, string[] commands)
        {
            return Execute("cmd.exe", arg, commands);
        }
        public static string PowerShell(string arg, string[] commands)
        {
            if (IsPowerShellInstalled() == false)
            {
                return string.Empty;
            }
            return Execute("powershell.exe", arg, commands);
        }

        public static string Linux(string arg, string[] commands)
        {
            return Execute("/bin/bash", arg, commands);
        }
        public static string Osx(string arg, string[] commands)
        {
            return Execute("/bin/bash", arg, commands);
        }

        public static Process Execute(string fileName, string arg)
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
            proc.Start();

            //Process proc = Process.Start(fileName, arg);
            return proc;
        }

        public static string Execute(string fileName, string arg, string[] commands)
        {
            using Process proc = new Process();
            proc.StartInfo.WorkingDirectory = Path.GetFullPath(Path.Join("./"));
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.FileName = fileName;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.Arguments = arg;
            proc.StartInfo.Verb = "runas";
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
            string error = proc.StandardError.ReadToEnd();
            string output = string.Empty;
            if (string.IsNullOrWhiteSpace(error))
            {
                output = proc.StandardOutput.ReadToEnd();
            }
            else
            {
                LoggerHelper.Instance.Warning($"file:{fileName},arg:{arg},commands:{string.Join(Environment.NewLine,commands)} -> {error}");
            }
            proc.WaitForExit();
            proc.Close();
            proc.Dispose();

            return output;
        }

        public static bool IsPowerShellInstalled()
        {
            string[] powerShellPaths = {
                Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"),
                Environment.ExpandEnvironmentVariables(@"%SystemRoot%\SysWOW64\WindowsPowerShell\v1.0\powershell.exe")
             };

            foreach (string path in powerShellPaths)
            {
                if (File.Exists(path))
                {
                    return true;
                }
            }

            return false;
        }

    }
}
