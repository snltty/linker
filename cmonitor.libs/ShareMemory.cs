using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace cmonitor.libs
{
    /// <summary>
    /// InitLocal 和 InitGlobal 都可以初始化，都初始化时，需要启动 Loop，将InitGlobal同步数据到Local
    /// StateAction 设置状态变化回调，需要启动 Loop，监听数据变化
    /// </summary>
    public sealed class ShareMemory
    {
        private string key;
        private int length;
        private int itemSize;
        private byte[] bytes;
        private object lockObj = new object();
        MemoryMappedFile mmfLocal = null;
        MemoryMappedViewAccessor accessorLocal = null;
        MemoryMappedFile mmfGlobal = null;
        MemoryMappedViewAccessor accessorGlobal = null;

        Action<int, ShareMemoryState> stateAction;

        private readonly Dictionary<string, ShareItemInfo> dic = new Dictionary<string, ShareItemInfo>();

        public ShareMemory(string key, int length, int itemSize)
        {
            this.key = key;
            this.length = length;
            this.itemSize = itemSize;
            bytes = new byte[length * itemSize];
        }

        public void InitLocal()
        {
            try
            {
                if (OperatingSystem.IsWindows() && accessorLocal == null)
                {
                    mmfLocal = MemoryMappedFile.CreateOrOpen($"{key}", bytes.Length);
                    accessorLocal = mmfLocal.CreateViewAccessor();
                }
            }
            catch (Exception)
            {
            }
        }
        public void InitGlobal()
        {
            try
            {
                if (OperatingSystem.IsWindows() && accessorGlobal == null)
                {
                    mmfGlobal = MemoryMappedFile.CreateOrOpen($"Global\\{key}", bytes.Length);
                    accessorGlobal = mmfGlobal.CreateViewAccessor();
                }
            }
            catch (Exception)
            {
            }
        }
        public void StateAction(Action<int, ShareMemoryState> stateAction)
        {
            this.stateAction = stateAction;
        }

        public void Loop()
        {
            InitStateValues();
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (accessorLocal != null || accessorGlobal != null)
                    {
                        try
                        {
                            SyncMemory();
                            StateCallback();
                        }
                        catch (Exception)
                        {
                        }
                    }

                    Thread.Sleep(10);
                }

            }, TaskCreationOptions.LongRunning);
        }

        byte[] valuesBytes;
        byte[] gloablBytes;
        private void InitStateValues()
        {
            gloablBytes = new byte[bytes.Length];

            var values = Enum.GetValues(typeof(ShareMemoryState));
            valuesBytes = new byte[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                valuesBytes[i] = (byte)values.GetValue(i);
            }
        }

        private void StateCallback()
        {
            if (stateAction != null)
            {
                for (int index = 0; index < length; index++)
                {
                    for (int i = 0; i < valuesBytes.Length; i++)
                    {
                        ShareMemoryState state = (ShareMemoryState)valuesBytes[i];
                        bool result = ReadState(accessorLocal, index, state);
                        if (result)
                        {
                            stateAction(index, state);
                        }
                    }
                }
            }
        }
        private void SyncMemory()
        {
            if (accessorGlobal != null && accessorLocal != null)
            {
                //检查更新状态
                if (ReadState(accessorGlobal, 0, ShareMemoryState.Updated) == false)
                {
                    return;
                }
                WriteState(accessorGlobal, 0, ShareMemoryState.Updated, false);
                lock (lockObj)
                {
                    accessorGlobal.ReadArray(0, gloablBytes, 0, gloablBytes.Length);
                    accessorLocal.WriteArray(0, gloablBytes, 0, itemSize);
                    for (int i = 1; i < length; i++)
                    {
                        int index = i * itemSize;
                        int keyLen = BitConverter.ToInt32(gloablBytes, index);
                        if (keyLen > 0)
                        {
                            accessorLocal.WriteArray(index, gloablBytes, index, itemSize);
                        }
                    }
                    WriteState(accessorLocal, 0, ShareMemoryState.Updated, true);
                }
            }

        }

        public Dictionary<string, ShareItemInfo> GetItems(out bool updated)
        {
            updated = false;
            if (accessorLocal == null) return dic;
            try
            {
                updated = ReadState(accessorLocal, 0, ShareMemoryState.Updated);
                if (updated == false)
                {
                    return dic;
                }
                lock (lockObj)
                {
                    WriteState(accessorLocal, 0, ShareMemoryState.Updated, false);

                    accessorLocal.ReadArray(0, bytes, 0, bytes.Length);

                    for (int i = 1; i < length; i++)
                    {
                        int index = i * itemSize;
                        int keyLen = BitConverter.ToInt32(bytes, index);
                        index += 4;
                        if (keyLen > 0)
                        {
                            string key = Encoding.UTF8.GetString(bytes, index, keyLen);
                            index += keyLen;

                            string val = string.Empty;
                            int valLen = BitConverter.ToInt32(bytes, index);
                            index += 4;
                            if (keyLen + 8 + valLen <= itemSize)
                            {
                                val = Encoding.UTF8.GetString(bytes, index, valLen);
                            }
                            dic[key] = new ShareItemInfo
                            {
                                Index = i,
                                Value = val
                            };
                        }
                    }
                }

            }
            catch (Exception)
            {
            }
            return dic;
        }
        public string GetItemValue(int index)
        {
            MemoryMappedViewAccessor accessor = accessorLocal ?? accessorGlobal;
            if (accessor == null) return string.Empty;

            index *= itemSize;

            accessor.Read(index, out int keylen);
            index += 4 + keylen;
            if (keylen == 0) return string.Empty;

            accessor.Read(index, out int vallen);
            index += 4;
            if (vallen == 0 || keylen + 8 + vallen > itemSize) return string.Empty;

            byte[] bytes = new byte[vallen];
            accessor.ReadArray(index, bytes, 0, bytes.Length);

            return Encoding.UTF8.GetString(bytes);
        }

        public void Update(int index, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                Update(index,Array.Empty<byte>(), Encoding.UTF8.GetBytes(value));
            }
            else
            {
                Update(index, Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(value));
            }
        }
        public void Update(int index, byte[] key, byte[] value)
        {
            try
            {
                if (accessorLocal == null && accessorGlobal == null) return;
                if (index == 0) return;
                if (key.Length + 8 + value.Length > itemSize) return;

                lock (lockObj)
                {
                    int valIndex = index * itemSize;
                    int startIndex = valIndex;
                    int keylen = key.Length;
                    int vallen = value.Length;
                    if (key.Length > 0)
                    {
                        if (accessorLocal != null)
                        {
                            accessorLocal.Write(valIndex, ref keylen);
                            accessorLocal.WriteArray(valIndex + 4, key, 0, key.Length);
                        }
                        if (accessorGlobal != null)
                        {
                            accessorGlobal.Write(valIndex, ref keylen);
                            accessorGlobal.WriteArray(valIndex + 4, key, 0, key.Length);
                        }
                        valIndex += 4 + key.Length;
                    }
                    else
                    {
                        int keyLen = 0;
                        if (accessorLocal != null)
                        {
                            accessorLocal.Read(valIndex, out keyLen);
                        }
                        if (keyLen == 0 && accessorGlobal != null)
                        {
                            accessorGlobal.Read(valIndex, out keyLen);
                        }
                        valIndex += 4 + keyLen;
                    }

                    if (accessorLocal != null)
                    {
                        accessorLocal.Write(valIndex, vallen);
                        accessorLocal.WriteArray(valIndex + 4, value, 0, value.Length);
                    }
                    if (accessorGlobal != null)
                    {
                        accessorGlobal.Write(valIndex, vallen);
                        accessorGlobal.WriteArray(valIndex + 4, value, 0, value.Length);
                    }
                }
                WriteUpdated(index, true);
            }
            catch (Exception)
            {
            }
        }

        private bool ReadState(MemoryMappedViewAccessor accessor, int index, ShareMemoryState state)
        {
            if (accessor == null) return false;

            ShareMemoryState stateByte = (ShareMemoryState)accessor.ReadByte(index);
            return (stateByte & state) == state;
        }
        public bool ReadUpdated(int index)
        {
            if (accessorLocal != null)
                return ReadState(accessorLocal, index, ShareMemoryState.Updated);
            if (accessorGlobal != null)
                return ReadState(accessorGlobal, index, ShareMemoryState.Updated);
            return false;
        }
        public bool ReadClosed(int index)
        {
            if (accessorLocal != null)
                return ReadState(accessorLocal, index, ShareMemoryState.Closed);
            if (accessorGlobal != null)
                return ReadState(accessorGlobal, index, ShareMemoryState.Closed);
            return false;
        }
        public bool ReadRunning(int index)
        {
            if (accessorLocal != null)
                return ReadState(accessorLocal, index, ShareMemoryState.Running);
            if (accessorGlobal != null)
                return ReadState(accessorGlobal, index, ShareMemoryState.Running);
            return false;
        }

        private void WriteState(MemoryMappedViewAccessor accessor, int index, ShareMemoryState state, bool value)
        {
            if (accessor == null) return;
            byte stateValue = accessor.ReadByte(index);
            byte stateByte = (byte)state;
            if (value)
            {
                stateValue |= stateByte;
            }
            else
            {
                stateValue &= (byte)(~stateByte);
            }
            accessor.Write(index, stateValue);
        }
        public void WriteUpdated(int index, bool updated = true)
        {
            WriteState(accessorLocal, index, ShareMemoryState.Updated, updated);
            WriteState(accessorGlobal, index, ShareMemoryState.Updated, updated);
            WriteState(accessorLocal, 0, ShareMemoryState.Updated, updated);
            WriteState(accessorGlobal, 0, ShareMemoryState.Updated, updated);
        }
        public void WriteClosed(int index, bool closed = true)
        {
            WriteState(accessorLocal, index, ShareMemoryState.Closed, closed);
            WriteState(accessorGlobal, index, ShareMemoryState.Closed, closed);
            WriteUpdated(index, true);
        }
        public void WriteRunning(int index, bool running = true)
        {
            WriteState(accessorLocal, index, ShareMemoryState.Running, running);
            WriteState(accessorGlobal, index, ShareMemoryState.Running, running);
            WriteUpdated(index, true);
        }

        public void Disponse()
        {
            accessorLocal?.Dispose();
            mmfLocal?.Dispose();
            accessorGlobal?.Dispose();
            mmfGlobal?.Dispose();
        }
    }

    public enum ShareMemoryState : byte
    {
        Updated = 0b0000_0001,
        Closed = 0b0000_0010,
        Running = 0b0000_0100,
    }

    public sealed partial class ShareItemInfo
    {
        /// <summary>
        /// 内存下标
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// 内存值
        /// </summary>
        public string Value { get; set; }

    }
}
