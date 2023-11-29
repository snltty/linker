using System;
using System.Collections.Generic;
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
        private const int shareMemoryStateSize = 1;

        private string key;
        private int length;
        private int itemSize;
        private byte[] bytes;
        private object lockObj = new object();
        IShareMemory accessorLocal = null;
        IShareMemory accessorGlobal = null;

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
                if (accessorLocal == null)
                {
                    accessorLocal = ShareMemoryFactory.Create(key, length, itemSize);
                    if (accessorLocal.Init() == false)
                    {
                        accessorLocal = null;
                    }
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
                    accessorGlobal = ShareMemoryFactory.Create(key, length, itemSize);
                    if (accessorGlobal.Init() == false)
                    {
                        accessorGlobal = null;
                    }
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
                lock (lockObj)
                {
                    accessorGlobal.ReadArray(0, gloablBytes, 0, gloablBytes.Length);
                    accessorLocal.WriteArray(0, gloablBytes, 0, itemSize);
                    for (int index = 0; index < length; index++)
                    {
                        //检查更新状态
                        if (ReadState(accessorGlobal, index, ShareMemoryState.Updated) == false)
                        {
                            continue;
                        }

                        int _index = index * itemSize;
                        int keyLen = BitConverter.ToInt32(gloablBytes, _index + shareMemoryStateSize);
                        if (keyLen > 0)
                        {
                            accessorLocal.WriteArray(_index, gloablBytes, _index, itemSize);
                        }
                        WriteState(accessorGlobal, index, ShareMemoryState.Updated, false);
                    }
                }
            }

        }

        public Dictionary<string, ShareItemInfo> GetItems(out bool updated)
        {
            updated = false;
            if (accessorLocal == null) return dic;
            try
            {
                lock (lockObj)
                {
                    accessorLocal.ReadArray(0, bytes, 0, bytes.Length);
                    for (int index = 0; index < length; index++)
                    {
                        //state
                        bool _updated = ReadState(accessorLocal, index, ShareMemoryState.Updated);
                        if (_updated == false) continue;
                        WriteState(accessorLocal, index, ShareMemoryState.Updated, false);
                        updated |= _updated;

                        //key length
                        int _index = index * itemSize + shareMemoryStateSize;
                        int keyLen = BitConverter.ToInt32(bytes, _index);
                        _index += 4;
                        if (keyLen > 0 && keyLen + 8 + shareMemoryStateSize < itemSize)
                        {
                            //key
                            string key = Encoding.UTF8.GetString(bytes, _index, keyLen);
                            _index += keyLen;

                            //val length
                            string val = string.Empty;
                            int valLen = BitConverter.ToInt32(bytes, _index);
                            _index += 4;
                            //value
                            if (keyLen + 8 + shareMemoryStateSize + valLen <= itemSize)
                            {
                                val = Encoding.UTF8.GetString(bytes, _index, valLen);
                            }

                            dic[key] = new ShareItemInfo
                            {
                                Index = index,
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
            IShareMemory accessor = accessorLocal ?? accessorGlobal;
            if (accessor == null) return string.Empty;

            index = index * itemSize + shareMemoryStateSize;

            int keylen = accessor.ReadInt(index);
            index += 4 + keylen;
            if (keylen == 0) return string.Empty;

            int vallen = accessor.ReadInt(index);
            index += 4;
            if (vallen == 0 || keylen + 8 + shareMemoryStateSize + vallen > itemSize) return string.Empty;

            byte[] bytes = new byte[vallen];
            accessor.ReadArray(index, bytes, 0, bytes.Length);

            return Encoding.UTF8.GetString(bytes);
        }

        public bool Update(int index, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return Update(index, Array.Empty<byte>(), Encoding.UTF8.GetBytes(value));
            }
            else
            {
                return Update(index, Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(value));
            }
        }
        public bool Update(int index, byte[] key, byte[] value)
        {
            try
            {
                if (accessorLocal == null && accessorGlobal == null) return false;
                if (index == 0) return false;
                if (key.Length + 8 + shareMemoryStateSize + value.Length > itemSize) return false;

                lock (lockObj)
                {
                    int valIndex = index * itemSize + shareMemoryStateSize;
                    int startIndex = valIndex;
                    int keylen = key.Length;
                    int vallen = value.Length;
                    if (key.Length > 0)
                    {
                        if (accessorLocal != null)
                        {
                            accessorLocal.WriteInt(valIndex, keylen);
                            accessorLocal.WriteArray(valIndex + 4, key, 0, key.Length);
                        }
                        if (accessorGlobal != null)
                        {
                            accessorGlobal.WriteInt(valIndex, keylen);
                            accessorGlobal.WriteArray(valIndex + 4, key, 0, key.Length);
                        }
                        valIndex += 4 + key.Length;
                    }
                    else
                    {
                        int keyLen = 0;
                        if (accessorLocal != null)
                        {
                            keyLen = accessorLocal.ReadInt(valIndex);
                        }
                        if (keyLen == 0 && accessorGlobal != null)
                        {
                            keyLen = accessorGlobal.ReadInt(valIndex);
                        }
                        valIndex += 4 + keyLen;
                    }

                    if (accessorLocal != null)
                    {
                        accessorLocal.WriteInt(valIndex, vallen);
                        accessorLocal.WriteArray(valIndex + 4, value, 0, value.Length);
                    }
                    if (accessorGlobal != null)
                    {
                        accessorGlobal.WriteInt(valIndex, vallen);
                        accessorGlobal.WriteArray(valIndex + 4, value, 0, value.Length);
                    }
                }
                WriteUpdated(index, true);
                return true;
            }
            catch (Exception)
            {
            }
            return false;
        }

        private bool ReadState(IShareMemory accessor, int index, ShareMemoryState state)
        {
            if (accessor == null) return false;

            ShareMemoryState stateByte = (ShareMemoryState)accessor.ReadByte(index * itemSize);
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

        private void WriteState(IShareMemory accessor, int index, ShareMemoryState state, bool value)
        {
            if (accessor == null) return;
            byte stateValue = accessor.ReadByte(index * itemSize);
            byte stateByte = (byte)state;
            if (value)
            {
                stateValue |= stateByte;
            }
            else
            {
                stateValue &= (byte)(~stateByte);
            }
            accessor.WriteByte(index * itemSize, stateValue);
        }
        public void WriteUpdated(int index, bool updated = true)
        {
            WriteState(accessorLocal, index, ShareMemoryState.Updated, updated);
            WriteState(accessorGlobal, index, ShareMemoryState.Updated, updated);
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
    }

    public struct ShareMemoryStruct
    {
        public ShareMemoryState State;
        public int KeyLength;
        public byte[] Key;
        public int ValueLength;
        public byte[] Value;
    };

    public enum ShareMemoryState : byte
    {
        Updated = 0b0000_0001,
        Closed = 0b0000_0010,
        Running = 0b0000_0100,
        All = 0b1111_1111
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
