using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace cmonitor.libs
{
    /// <summary>
    /// InitLocal 和 InitGlobal 都可以初始化，都初始化时，需要启动 StartLoop，将InitGlobal同步数据到Local
    /// AddAttributeAction 设置状态变化回调，需要启动 Loop，监听数据变化
    /// </summary>
    public sealed class ShareMemory
    {
        private const int shareMemoryAttributeIndex = 0;
        private const int shareMemoryAttributeSize = 1;
        private const int shareMemoryVersionSize = 8;
        private const int shareMemoryVersionIndex = 1;
        private const int shareMemoryHeadSize = shareMemoryAttributeSize + shareMemoryVersionSize;

        private string key;
        private int length;
        private int itemSize;
        private byte[] bytes;
        private object lockObj = new object();
        private long mainVersion = 0;
        IShareMemory accessorLocal = null;
        IShareMemory accessorGlobal = null;

        private readonly ShareMemoryAttribute[] itemAttributes = Array.Empty<ShareMemoryAttribute>();
        private readonly long[] itemVersions = Array.Empty<long>();
        ConcurrentDictionary<int, List<Action<ShareMemoryAttribute>>> attributeActions = new ConcurrentDictionary<int, List<Action<ShareMemoryAttribute>>>();
        private CancellationTokenSource cancellationTokenSource;
        private ConcurrentQueue<ShareItemAttributeChanged> attributeChangeds = new ConcurrentQueue<ShareItemAttributeChanged>();

        private readonly Dictionary<string, ShareItemInfo> dic = new Dictionary<string, ShareItemInfo>();

        public ShareMemory(string key, int length, int itemSize)
        {
            this.key = key;
            this.length = length;
            this.itemSize = itemSize;
            bytes = new byte[length * itemSize];
            itemAttributes = new ShareMemoryAttribute[length];
            itemVersions = new long[length];
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

        private byte[] gloablBytes;
        public void InitGlobal()
        {
            try
            {
                if (OperatingSystem.IsWindows() && accessorGlobal == null)
                {
                    gloablBytes = new byte[bytes.Length];
                    accessorGlobal = ShareMemoryFactory.Create($"Global\\{key}", length, itemSize);
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
        public void AddAttributeAction(int index, Action<ShareMemoryAttribute> action)
        {
            if (attributeActions.TryGetValue(index, out List<Action<ShareMemoryAttribute>> actions) == false)
            {
                actions = new List<Action<ShareMemoryAttribute>>();
                attributeActions.TryAdd(index, actions);
            }
            if (actions.Any(c => c == action) == false)
            {
                actions.Add(action);
            }
        }
        public void RemoveAttributeAction(int index, Action<ShareMemoryAttribute> action)
        {
            if (attributeActions.TryGetValue(index, out List<Action<ShareMemoryAttribute>> actions) == false)
            {
                return;
            }
            actions.Remove(action);
        }


        public void StartLoop()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(() =>
            {
                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    try
                    {
                        SyncMemory();
                        AttributeCallback();
                        ReadItems();
                    }
                    catch (Exception)
                    {
                    }

                    Thread.Sleep(10);
                }

            }, TaskCreationOptions.LongRunning);

            Task.Factory.StartNew(() =>
            {
                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    try
                    {
                        while (attributeChangeds.TryDequeue(out ShareItemAttributeChanged result))
                        {
                            result.Action(result.Attribute);
                        }
                    }
                    catch (Exception)
                    {
                    }

                    Thread.Sleep(30);
                }

            }, TaskCreationOptions.LongRunning);
        }

        private void AttributeCallback()
        {
            IShareMemory accessor = accessorLocal ?? accessorGlobal;
            if (accessor == null) return;

            bool allUpdated = false;
            for (int index = 0; index < length; index++)
            {
                ShareMemoryAttribute attribute = ReadAttribute(accessorLocal, index);
                bool updated = ReadVersionUpdated(accessorLocal, index);
                allUpdated |= updated;
                if (updated)
                {
                    itemAttributes[index] &= (~ShareMemoryAttribute.Updated);
                    attribute |= ShareMemoryAttribute.Updated;
                }
                if (attribute != itemAttributes[index])
                {
                    itemAttributes[index] = attribute;
                    if (attributeActions.TryGetValue(index, out List<Action<ShareMemoryAttribute>> actions) && actions.Count > 0)
                    {
                        foreach (var action in actions)
                        {
                            attributeChangeds.Enqueue(new ShareItemAttributeChanged { Action=action, Attribute= attribute });
                        }
                    }
                }

            }
            if (allUpdated)
            {
                mainVersion++;
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
                        if (ReadVersionUpdated(accessorGlobal, index) == false)
                        {
                            continue;
                        }

                        int _index = index * itemSize;
                        int keyLen = BitConverter.ToInt32(gloablBytes, _index + shareMemoryHeadSize);
                        if (keyLen > 0)
                        {
                            accessorLocal.WriteArray(_index, gloablBytes, _index, itemSize);
                        }
                    }

                }
            }

        }
        private void ReadItems()
        {
            IShareMemory accessor = accessorLocal ?? accessorGlobal;
            if (accessor == null) return;
            try
            {
                lock (lockObj)
                {
                    accessor.ReadArray(0, bytes, 0, bytes.Length);

                    for (int index = 0; index < length; index++)
                    {
                        ShareMemoryAttribute attribute = ReadAttribute(accessor, index);
                        long itemVersion = ReadVersion(accessor, index);
                        bool skip = (itemVersion <= itemVersions[index] || (attribute & ShareMemoryAttribute.HiddenForList) == ShareMemoryAttribute.HiddenForList);
                        itemVersions[index] = itemVersion;
                        if (skip)
                        {
                            continue;
                        }

                        //key length
                        int _index = index * itemSize + shareMemoryHeadSize;
                        int keyLen = BitConverter.ToInt32(bytes, _index);
                        _index += 4;
                        if (keyLen > 0 && keyLen + 8 + shareMemoryHeadSize < itemSize)
                        {
                            //key
                            string key = Encoding.UTF8.GetString(bytes, _index, keyLen);
                            _index += keyLen;

                            //val length
                            string val = string.Empty;
                            int valLen = BitConverter.ToInt32(bytes, _index);
                            _index += 4;
                            //value
                            if (keyLen + 8 + shareMemoryHeadSize + valLen <= itemSize)
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
        }

        public Dictionary<string, ShareItemInfo> ReadItems(out long version)
        {
            version = mainVersion;
            return dic;
        }
        public string ReadValueString(int index)
        {
            byte[] bytes = ReadValueArray(index);
            if (bytes.Length == 0) return string.Empty;

            return Encoding.UTF8.GetString(bytes);
        }
        public long ReadValueInt64(int index)
        {
            IShareMemory accessor = accessorLocal ?? accessorGlobal;
            if (accessor == null || index >= length) return 0;
            index = index * itemSize + shareMemoryHeadSize;

            int keylen = accessor.ReadInt(index);
            index += 4 + keylen;
            if (keylen == 0) return 0;

            int vallen = accessor.ReadInt(index);
            index += 4;
            if (vallen == 0 || keylen + 8 + shareMemoryHeadSize + vallen > itemSize) return 0;

            return accessor.ReadInt64(index);
        }
        public byte[] ReadValueArray(int index)
        {
            IShareMemory accessor = accessorLocal ?? accessorGlobal;
            if (accessor == null || index >= length) return Array.Empty<byte>();
            index = index * itemSize + shareMemoryHeadSize;

            int keylen = accessor.ReadInt(index);
            index += 4 + keylen;
            if (keylen == 0) return Array.Empty<byte>();

            int vallen = accessor.ReadInt(index);
            index += 4;
            if (vallen == 0 || keylen + 8 + shareMemoryHeadSize + vallen > itemSize) return Array.Empty<byte>();

            byte[] bytes = new byte[vallen];
            accessor.ReadArray(index, bytes, 0, bytes.Length);

            return bytes;
        }

        public bool Update(int index, string key, string value,
            ShareMemoryAttribute addAttri = ShareMemoryAttribute.None,
            ShareMemoryAttribute removeAttri = ShareMemoryAttribute.None)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return Update(index, Array.Empty<byte>(), Encoding.UTF8.GetBytes(value), addAttri, removeAttri);
            }
            else
            {
                return Update(index, Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(value), addAttri, removeAttri);
            }
        }
        public bool Update(int index, byte[] key, byte[] value,
            ShareMemoryAttribute addAttri = ShareMemoryAttribute.None,
            ShareMemoryAttribute removeAttri = ShareMemoryAttribute.None)
        {
            try
            {
                if (accessorLocal == null && accessorGlobal == null) return false;
                if (index > length) return false;
                if (key.Length + 8 + shareMemoryHeadSize + value.Length > itemSize) return false;

                lock (lockObj)
                {
                    int valIndex = index * itemSize + shareMemoryHeadSize;
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
                    IncrementVersion(index);
                    if (removeAttri > 0)
                    {
                        RemoveAttribute(index, removeAttri);
                    }
                    if (addAttri > 0)
                    {
                        AddAttribute(index, addAttri);
                    }
                }

                return true;
            }
            catch (Exception)
            {
            }
            return false;
        }

        private ShareMemoryAttribute ReadAttribute(IShareMemory accessor, int index)
        {
            if (accessor == null || index >= length) return ShareMemoryAttribute.None;

            ShareMemoryAttribute stateByte = (ShareMemoryAttribute)accessor.ReadByte(index * itemSize + shareMemoryAttributeIndex);
            return stateByte;
        }
        private bool ReadAttributeEqual(IShareMemory accessor, int index, ShareMemoryAttribute attribute)
        {
            if (accessor == null || index >= length) return false;

            ShareMemoryAttribute attributeByte = (ShareMemoryAttribute)accessor.ReadByte(index * itemSize + shareMemoryAttributeIndex);
            return (attributeByte & attribute) == attribute;
        }

        private void AddAttribute(IShareMemory accessor, int index, ShareMemoryAttribute attribute)
        {
            if (accessor == null || index >= length) return;
            byte attributeValue = accessor.ReadByte(index * itemSize + shareMemoryAttributeIndex);
            byte attributeByte = (byte)attribute;
            attributeValue |= attributeByte;
            accessor.WriteByte(index * itemSize + shareMemoryAttributeIndex, attributeValue);

            IncrementVersion(accessor, index);
        }
        private void RemoveAttribute(IShareMemory accessor, int index, ShareMemoryAttribute attribute)
        {
            if (accessor == null || index >= length) return;
            byte attributeValue = accessor.ReadByte(index * itemSize + shareMemoryAttributeIndex);
            byte attributeByte = (byte)attribute;
            attributeValue &= (byte)(~attributeByte);
            accessor.WriteByte(index * itemSize + shareMemoryAttributeIndex, attributeValue);

            IncrementVersion(accessor, index);
        }

        private void IncrementVersion(IShareMemory accessor, int index)
        {
            if (accessor == null || index >= length) return;
            long version = accessor.ReadInt64(index * itemSize + shareMemoryVersionIndex);
            accessor.WriteInt64(index * itemSize + shareMemoryVersionIndex, version + 1);
        }
        private long ReadVersion(IShareMemory accessor, int index)
        {
            if (accessor == null || index >= length) return 0;
            long version = accessor.ReadInt64(index * itemSize + shareMemoryVersionIndex);
            return version;
        }
        private bool ReadVersionUpdated(IShareMemory accessor, int index)
        {
            long version = ReadVersion(accessor, index);
            return version > itemVersions[index];
        }
        private bool ReadVersionUpdated(IShareMemory accessor, int index,ref long version)
        {
            long _version = ReadVersion(accessor, index);
            bool res = _version > version;
            version = _version;
            return res;
        }

        public ShareMemoryAttribute ReadAttribute(int index)
        {
            IShareMemory accessor = accessorLocal ?? accessorGlobal;
            if (accessor == null) return ShareMemoryAttribute.None;

            return ReadAttribute(accessor, index);
        }
        public bool ReadAttributeEqual(int index, ShareMemoryAttribute attribute)
        {
            IShareMemory accessor = accessorLocal ?? accessorGlobal;
            if (accessor == null) return false;

            return ReadAttributeEqual(accessor, index, attribute);
        }

        public void AddAttribute(int index, ShareMemoryAttribute attribute)
        {
            AddAttribute(accessorLocal, index, attribute);
            AddAttribute(accessorGlobal, index, attribute);
        }
        public void RemoveAttribute(int index, ShareMemoryAttribute attribute)
        {
            RemoveAttribute(accessorLocal, index, attribute);
            RemoveAttribute(accessorGlobal, index, attribute);
        }

        public void IncrementVersion(int index)
        {
            IncrementVersion(accessorLocal, index);
            IncrementVersion(accessorGlobal, index);
        }
        public long ReadVersion(int index)
        {
            IShareMemory accessor = accessorLocal ?? accessorGlobal;
            if (accessor == null) return 0;

            return ReadVersion(accessor, index);
        }
        public bool ReadVersionUpdated(int index)
        {
            IShareMemory accessor = accessorLocal ?? accessorGlobal;
            if (accessor == null) return false;

            return ReadVersionUpdated(accessor, index);
        }
        public bool ReadVersionUpdated(int index,ref long version)
        {
            IShareMemory accessor = accessorLocal ?? accessorGlobal;
            if (accessor == null) return false;

            return ReadVersionUpdated(accessor, index,ref version);
        }
    }

    public enum ShareMemoryAttribute : byte
    {
        None = 0,
        Updated = 0b0000_0001,
        Closed = 0b0000_0010,
        Running = 0b0000_0100,
        HiddenForList = 0b0000_1000,
        All = 0b1111_1111
    }

    public sealed class ShareItemAttributeChanged
    {
        public Action<ShareMemoryAttribute> Action { get; set; }
        public ShareMemoryAttribute Attribute { get; set; }
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
