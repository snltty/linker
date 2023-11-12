using System.IO.MemoryMappedFiles;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Runtime.InteropServices;

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
                }
                mmf2 = MemoryMappedFile.CreateOrOpen($"Global\\{shareMkey}", shareMLength * shareItemMLength);
                accessor2 = mmf2.CreateViewAccessor();
                CheckMemory();
            }
            catch (Exception)
            {
            }
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
    }
}
