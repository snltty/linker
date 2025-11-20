using MemoryPack;
using linker.messenger.decenter;

namespace linker.messenger.serializer.memorypack
{
    [MemoryPackable]
    public readonly partial struct SerializableDecenterSyncInfo
    {
        [MemoryPackIgnore]
        public readonly DecenterSyncInfo info;

        [MemoryPackInclude]
        string Name => info.Name;

        [MemoryPackInclude]
        Memory<byte> Data => info.Data;

        [MemoryPackConstructor]
        SerializableDecenterSyncInfo(string name, Memory<byte> data)
        {
            var info = new DecenterSyncInfo { Name = name, Data = data };
            this.info = info;
        }

        public SerializableDecenterSyncInfo(DecenterSyncInfo tunnelCompactInfo)
        {
            this.info = tunnelCompactInfo;
        }
    }
    public class DecenterSyncInfoFormatter : MemoryPackFormatter<DecenterSyncInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref DecenterSyncInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableDecenterSyncInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref DecenterSyncInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

           
            value = new DecenterSyncInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Name = reader.ReadValue<string>();
            value.Data = reader.ReadValue<Memory<byte>>();

        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableDecenterPullPageInfo
    {
        [MemoryPackIgnore]
        public readonly DecenterPullPageInfo info;

        [MemoryPackInclude]
        string Name => info.Name;

        [MemoryPackInclude]
        int Page => info.Page;

        [MemoryPackInclude]
        int Size => info.Size;

        [MemoryPackConstructor]
        SerializableDecenterPullPageInfo(string name, int page, int size)
        {
            var info = new DecenterPullPageInfo { Name = name, Page = page, Size = size };
            this.info = info;
        }

        public SerializableDecenterPullPageInfo(DecenterPullPageInfo tunnelCompactInfo)
        {
            this.info = tunnelCompactInfo;
        }
    }
    public class DecenterPullPageInfoFormatter : MemoryPackFormatter<DecenterPullPageInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref DecenterPullPageInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableDecenterPullPageInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref DecenterPullPageInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value =new DecenterPullPageInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Name = reader.ReadValue<string>();
            value.Page = reader.ReadValue<int>();
            value.Size = reader.ReadValue<int>();
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableDecenterPullPageResultInfo
    {
        [MemoryPackIgnore]
        public readonly DecenterPullPageResultInfo info;

        [MemoryPackInclude]
        int Page => info.Page;

        [MemoryPackInclude]
        int Size => info.Size;

        [MemoryPackInclude]
        int Count => info.Count;
        [MemoryPackInclude]
        List<Memory<byte>> List => info.List;

        [MemoryPackConstructor]
        SerializableDecenterPullPageResultInfo(int page, int size, int count, List<Memory<byte>> list)
        {
            var info = new DecenterPullPageResultInfo { Page = page, Size = size, Count = count, List = list };
            this.info = info;
        }

        public SerializableDecenterPullPageResultInfo(DecenterPullPageResultInfo tunnelCompactInfo)
        {
            this.info = tunnelCompactInfo;
        }
    }
    public class DecenterPullPageResultInfoFormatter : MemoryPackFormatter<DecenterPullPageResultInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref DecenterPullPageResultInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableDecenterPullPageResultInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref DecenterPullPageResultInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new DecenterPullPageResultInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Page = reader.ReadValue<int>();
            value.Size = reader.ReadValue<int>();
            value.Count = reader.ReadValue<int>();
            value.List = reader.ReadValue<List<Memory<byte>>>();
        }
    }
}
