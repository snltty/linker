using System.Collections.Concurrent;
using System.Diagnostics;

namespace cmonitor.client.reports.command
{
    public sealed class CommandLineWindows : ICommandLine
    {
        private ConcurrentDictionary<int, ProcessCacheInfo> processs { get; set; } = new ConcurrentDictionary<int, ProcessCacheInfo>();

        public Action<int, string> OnData { get; set; }

        public CommandLineWindows()
        {
            TimeoutTask();
        }
        private void TimeoutTask()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (processs.IsEmpty == false)
                    {
                        long ticks = DateTime.Now.Ticks;
                        long timeout = TimeSpan.TicksPerMillisecond * 1000 * 15;
                        IEnumerable<int> timoutIds = processs.Values.Where(c => (ticks - c.LastTime) > timeout).Select(c => c.Id);
                        if (timoutIds.Any())
                        {
                            foreach (int id in timoutIds)
                            {
                                Stop(id);
                            }
                        }
                    }

                    await Task.Delay(5000);
                }

            }, TaskCreationOptions.LongRunning);
        }
        public int Start()
        {
            Process proc = Execute();
            processs.TryAdd(proc.Id, new ProcessCacheInfo { Id = proc.Id, Process = proc, LastTime = DateTime.Now.Ticks });
            return proc.Id;
        }

        public void Write(int id, string command)
        {
            if (processs.TryGetValue(id, out ProcessCacheInfo proc))
            {
                try
                {
                    proc.Process.StandardInput.Write(command);
                }
                catch (Exception)
                {
                }
            }
        }

        public void Alive(int id)
        {
            if (processs.TryGetValue(id, out ProcessCacheInfo proc))
            {
                try
                {
                    proc.LastTime = DateTime.Now.Ticks;
                }
                catch (Exception)
                {
                }
            }
        }

        public void Stop(int id)
        {
            if (processs.TryRemove(id, out ProcessCacheInfo proc))
            {
                try
                {
                    proc.Process.Kill();
                    proc.Process.Dispose();
                }
                catch (Exception)
                {
                }
            }
        }

        private Process Execute()
        {
            if (OperatingSystem.IsWindows())
            {
                return Windows(string.Empty);
            }
            else if (OperatingSystem.IsLinux())
            {
                return Linux(string.Empty);
            }
            return Osx(string.Empty);
        }
        private Process Windows(string arg)
        {
            return Execute("cmd.exe", arg);
        }
        private Process Linux(string arg)
        {
            return Execute("/bin/bash", arg);
        }
        private Process Osx(string arg)
        {
            return Execute("/bin/bash", arg);
        }
        private Process Execute(string fileName, string arg)
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

            proc.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
            proc.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);
            proc.EnableRaisingEvents = true;
            proc.Exited += new EventHandler(Exited);


            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            return proc;
        }
        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Process proc = sender as Process;
            if (e.Data != null)
            {
                OnData?.Invoke(proc.Id, e.Data);
            }
        }
        private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Process proc = sender as Process;
            if (e.Data != null)
            {
                OnData?.Invoke(proc.Id, e.Data);
            }
        }
        private void Exited(object sender, EventArgs e)
        {
            Process proc = sender as Process;
            if (proc != null)
            {
                proc.Dispose();
                processs.TryRemove(proc.Id, out _);
            }
        }

        sealed class ProcessCacheInfo
        {
            public int Id { get; set; }
            public Process Process { get; set; }
            public long LastTime { get; set; }
        }
    }
}
