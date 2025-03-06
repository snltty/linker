using MemoryPack;
using linker.messenger.sforward;
using System.Net;

namespace linker.messenger.serializer.memorypack
{
    [MemoryPackable]
    public readonly partial struct SerializableSForwardInfo
    {
        [MemoryPackIgnore]
        public readonly SForwardInfo info;

        [MemoryPackInclude]
        long Id => info.Id;

        [MemoryPackInclude]
        string Name => info.Name;

        [MemoryPackInclude]
        string Domain => info.Domain;

        [MemoryPackInclude]
        int RemotePort => info.RemotePort;

        [MemoryPackInclude]
        byte BufferSize => info.BufferSize;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPEndPoint LocalEP => info.LocalEP;

        [MemoryPackInclude]
        bool Started => info.Started;

        [MemoryPackInclude]
        string Msg => info.Msg;

        [MemoryPackInclude]
        string LocalMsg => info.LocalMsg;

        [MemoryPackInclude]
        int RemotePortMin => info.RemotePortMin;

        [MemoryPackInclude]
        int RemotePortMax => info.RemotePortMax;

        [MemoryPackConstructor]
        SerializableSForwardInfo(long id, string name, string domain, int remotePort, byte bufferSize, IPEndPoint localEP, bool started, string msg, string localMsg, int remotePortMin, int remotePortMax)
        {
            this.info = new SForwardInfo
            {
                LocalEP = localEP,
                RemotePort = remotePort,
                RemotePortMax = remotePortMax,
                Name = name,
                BufferSize = bufferSize,
                Domain = domain,
                Id = id,
                LocalMsg = localMsg,
                Msg = localMsg,
                RemotePortMin = remotePortMin,
                Started = started,
            };
        }

        public SerializableSForwardInfo(SForwardInfo info)
        {
            this.info = info;
        }
    }
    public class SForwardInfoFormatter : MemoryPackFormatter<SForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SForwardInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSForwardInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSForwardInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableSForwardAddInfo
    {
        [MemoryPackIgnore]
        public readonly SForwardAddInfo info;

        [MemoryPackInclude]
        string Domain => info.Domain;

        [MemoryPackInclude]
        int RemotePort => info.RemotePort;

        [MemoryPackInclude]
        string SecretKey => info.SecretKey;

        [MemoryPackConstructor]
        SerializableSForwardAddInfo(string domain, int remotePort, string secretKey)
        {
            this.info = new SForwardAddInfo
            {
                RemotePort = remotePort,
                Domain = domain,
                SecretKey = secretKey
            };
        }

        public SerializableSForwardAddInfo(SForwardAddInfo info)
        {
            this.info = info;
        }
    }
    public class SForwardAddInfoFormatter : MemoryPackFormatter<SForwardAddInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SForwardAddInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSForwardAddInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SForwardAddInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSForwardAddInfo>();
            value = wrapped.info;
        }
    }
    [MemoryPackable]
    public readonly partial struct SerializableSForwardAddResultInfo
    {
        [MemoryPackIgnore]
        public readonly SForwardAddResultInfo info;

        [MemoryPackInclude]
        bool Success => info.Success;

        [MemoryPackInclude]
        string Message => info.Message;

        [MemoryPackInclude]
        byte BufferSize => info.BufferSize;

        [MemoryPackConstructor]
        SerializableSForwardAddResultInfo(bool success, string message, byte bufferSize)
        {
            this.info = new SForwardAddResultInfo
            {
                BufferSize = bufferSize,
                Message = message,
                Success = success
            };
        }

        public SerializableSForwardAddResultInfo(SForwardAddResultInfo info)
        {
            this.info = info;
        }
    }
    public class SForwardAddResultInfoFormatter : MemoryPackFormatter<SForwardAddResultInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SForwardAddResultInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSForwardAddResultInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SForwardAddResultInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSForwardAddResultInfo>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableSForwardAddForwardInfo
    {
        [MemoryPackIgnore]
        public readonly SForwardAddForwardInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        SForwardInfo Data => info.Data;

        [MemoryPackConstructor]
        SerializableSForwardAddForwardInfo(string machineId, SForwardInfo data)
        {
            this.info = new SForwardAddForwardInfo
            {
                MachineId = machineId,
                Data = data
            };
        }

        public SerializableSForwardAddForwardInfo(SForwardAddForwardInfo info)
        {
            this.info = info;
        }
    }
    public class SForwardAddForwardInfoFormatter : MemoryPackFormatter<SForwardAddForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SForwardAddForwardInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSForwardAddForwardInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SForwardAddForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSForwardAddForwardInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableSForwardRemoveForwardInfo
    {
        [MemoryPackIgnore]
        public readonly SForwardRemoveForwardInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        long Id => info.Id;

        [MemoryPackConstructor]
        SerializableSForwardRemoveForwardInfo(string machineId, long id)
        {
            this.info = new SForwardRemoveForwardInfo
            {
                MachineId = machineId,
                Id = id
            };
        }

        public SerializableSForwardRemoveForwardInfo(SForwardRemoveForwardInfo info)
        {
            this.info = info;
        }
    }
    public class SForwardRemoveForwardInfoFormatter : MemoryPackFormatter<SForwardRemoveForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SForwardRemoveForwardInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSForwardRemoveForwardInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SForwardRemoveForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSForwardRemoveForwardInfo>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableSForwardProxyInfo
    {
        [MemoryPackIgnore]
        public readonly SForwardProxyInfo info;

        [MemoryPackInclude]
        ulong Id => info.Id;

        [MemoryPackInclude]
        string Domain => info.Domain;

        [MemoryPackInclude]
        int RemotePort => info.RemotePort;

        [MemoryPackInclude]
        byte BufferSize => info.BufferSize;


        [MemoryPackConstructor]
        SerializableSForwardProxyInfo(ulong id, string domain, int remotePort, byte bufferSize)
        {
            this.info = new SForwardProxyInfo
            {
                Id = id,
                BufferSize = bufferSize,
                Domain = domain,
                RemotePort = remotePort
            };
        }

        public SerializableSForwardProxyInfo(SForwardProxyInfo info)
        {
            this.info = info;
        }
    }
    public class SForwardProxyInfoFormatter : MemoryPackFormatter<SForwardProxyInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SForwardProxyInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSForwardProxyInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SForwardProxyInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSForwardProxyInfo>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableSForwardCountInfo
    {
        [MemoryPackIgnore]
        public readonly SForwardCountInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        int Count => info.Count;

        [MemoryPackConstructor]
        SerializableSForwardCountInfo(string machineId, int count)
        {
            this.info = new SForwardCountInfo
            {
                MachineId = machineId,
                Count = count
            };
        }

        public SerializableSForwardCountInfo(SForwardCountInfo info)
        {
            this.info = info;
        }
    }
    public class SForwardCountInfoFormatter : MemoryPackFormatter<SForwardCountInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SForwardCountInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSForwardCountInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SForwardCountInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSForwardCountInfo>();
            value = wrapped.info;
        }
    }
}
