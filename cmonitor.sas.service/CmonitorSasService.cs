using System.IO.MemoryMappedFiles;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;

namespace cmonitor.sas.service
{
    public partial class CmonitorSasService : ServiceBase
    {
        private readonly string[] args;
        public CmonitorSasService(string[] args)
        {
            this.args = args;
            InitializeComponent();
        }


        string shareMkey = "cmonitor/share";
        int shareMLength = 10;
        int shareItemMLength = 255;
        int shareIndex = 3;
        string mainArgs = string.Empty;
        string mainExeName = "cmonitor";
        byte[] keyBytes = Encoding.UTF8.GetBytes("cmonitor.sas.service");
        MemoryMappedFile mmf2;
        MemoryMappedViewAccessor accessor2;
        CancellationTokenSource cancellationTokenSource;

        protected override void OnStart(string[] _args)
        {
            try
            {
                if (args != null && args.Length > 0)
                {
                    shareMkey = args[0];
                    shareMLength = int.Parse(args[1]);
                    shareIndex = int.Parse(args[2]);

                    if (args.Length >= 4)
                    {
                        mainArgs = args[3];
                    }
                }
                mmf2 = MemoryMappedFile.CreateOrOpen($"Global\\{shareMkey}", shareMLength * shareItemMLength);
                accessor2 = mmf2.CreateViewAccessor();
                CheckMemory();
            }
            catch (Exception)
            {
            }
            CheckMainProcess();
        }
        protected override void OnStop()
        {
            cancellationTokenSource?.Cancel();
        }

        private void CheckMemory()
        {
            cancellationTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew((a) =>
            {
                CancellationTokenSource tks = a as CancellationTokenSource;
                while (tks.IsCancellationRequested == false)
                {
                    try
                    {
                        string value = ReadMemory(shareIndex);
                        if (value == "ctrl+alt+delete")
                        {
                            try
                            {
                                SendSAS(false);
                            }
                            catch (Exception)
                            {
                            }
                            WriteMemory(shareIndex, keyBytes, new byte[0]);
                        }
                    }
                    finally
                    {
                    }

                    Thread.Sleep(10);
                }
            }, cancellationTokenSource, TaskCreationOptions.LongRunning);
        }
        private string ReadMemory(int index)
        {
            int keyIndex = index * shareItemMLength;
            int keyLength = accessor2.ReadByte(keyIndex);
            keyIndex += 1 + keyLength;
            int valueLength = accessor2.ReadByte(keyIndex);
            keyIndex += 1;

            byte[] valueBytes = new byte[valueLength];
            if (valueBytes.Length > 0)
            {
                accessor2.ReadArray(keyIndex, valueBytes, 0, valueLength);
                return Encoding.UTF8.GetString(valueBytes, 0, valueLength);
            }
            return string.Empty;
        }
        private void WriteMemory(int index, byte[] key, byte[] value)
        {
            int keyIndex = index * shareItemMLength;
            if (value.Length > 0)
                accessor2.Write(keyIndex, (byte)key.Length);
            keyIndex++;
            if (value.Length > 0)
                accessor2.WriteArray(keyIndex, key, 0, key.Length);
            keyIndex += key.Length;

            accessor2.Write(keyIndex, (byte)value.Length);
            if (value.Length > 0)
            {
                keyIndex++;
                accessor2.WriteArray(keyIndex, value, 0, value.Length);
                keyIndex += value.Length;
            }

            UpdatedState(index);
        }
        private void UpdatedState(int updatedOffset)
        {
            accessor2.Write((shareMLength - 1) * shareItemMLength, (byte)1);
        }

        [DllImport("sas.dll")]
        public static extern void SendSAS(bool asUser);


        Process proc;
        private void CheckMainProcess()
        {
            if (string.IsNullOrWhiteSpace(mainArgs))
            {
                return;
            }
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        if (Process.GetProcessesByName(mainExeName).Length <= 0)
                        {
                            KillExe();
                            OpenExe();
                        }
                    }
                    catch (Exception)
                    {
                    }
                    Thread.Sleep(30000);
                }
            }, TaskCreationOptions.LongRunning);
        }
        private bool OpenExe()
        {
            try
            {
                string filename = Process.GetCurrentProcess().MainModule.FileName;
                string dir = Path.GetDirectoryName(filename);
                string file = Path.Combine(dir, mainExeName);
                ProcessStartInfo processStartInfo = new ProcessStartInfo()
                {
                    WorkingDirectory = dir,
                    FileName = file,
                    CreateNoWindow = false,
                    ErrorDialog = false,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = string.Join(" ", this.args),
                    Verb = "runas",
                };
                proc = Process.Start(processStartInfo);

                return true;
            }
            catch (Exception)
            {
                try
                {
                    proc.Kill();
                    proc.Dispose();
                }
                catch (Exception)
                {
                }
                proc = null;
            }
            return false;
        }
        private void KillExe()
        {
            try
            {
                proc?.Close();
                proc?.Dispose();

            }
            catch (Exception)
            {
            }
            finally
            {
                proc = null;
            }
        }
    }
}
