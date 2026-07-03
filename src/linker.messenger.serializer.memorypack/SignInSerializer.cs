using linker.messenger.signin;
using MemoryPack;
using System.Net;

namespace linker.messenger.serializer.memorypack
{
    public class SignInfoFormatter : MemoryPackFormatter<SignInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SignInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(5);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.MachineName);
            writer.WriteValue(value.GroupId);
            writer.WriteValue(value.Version);
            writer.WriteValue(value.Args);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new SignInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.MachineName = reader.ReadValue<string>();
            value.GroupId = reader.ReadValue<string>();
            value.Version = reader.ReadValue<string>();
            value.Args = reader.ReadValue<Dictionary<string, string>>();
        }
    }

    public class SignCacheInfoFormatter : MemoryPackFormatter<SignCacheInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SignCacheInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(8);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.MachineName);
            writer.WriteValue(value.Version);
            writer.WriteValue(value.GroupId);
            writer.WriteValue(value.LastSignIn);
            writer.WriteValue(value.Args);
            writer.WriteValue(value.IP);
            writer.WriteValue(value.Connected);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignCacheInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new SignCacheInfo();
            value.Id = string.Empty;
            value.Order = 0;
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.MachineName = reader.ReadValue<string>();
            value.Version = reader.ReadValue<string>();
            value.GroupId = reader.ReadValue<string>();
            value.LastSignIn = reader.ReadValue<DateTime>();
            value.Args = reader.ReadValue<Dictionary<string, string>>();
            value.IP = reader.ReadValue<IPEndPoint>();
            value.Connected = reader.ReadValue<bool>();
        }
    }

    public class SignInListRequestInfoFormatter : MemoryPackFormatter<SignInListRequestInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SignInListRequestInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(6);
            writer.WriteValue(value.Page);
            writer.WriteValue(value.Size);
            writer.WriteValue(value.Name);
            writer.WriteValue(value.Ids);
            writer.WriteValue(value.Asc);
            writer.WriteValue(value.Prop);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignInListRequestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new SignInListRequestInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Page = reader.ReadValue<int>();
            value.Size = reader.ReadValue<int>();
            value.Name = reader.ReadValue<string>();
            value.Ids = reader.ReadValue<string[]>();
            value.Asc = reader.ReadValue<bool>();
            value.Prop = reader.ReadValue<string>();
        }
    }

    public class SignInListResponseInfoFormatter : MemoryPackFormatter<SignInListResponseInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SignInListResponseInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(3);
            writer.WriteValue(value.Request);
            writer.WriteValue(value.Count);
            writer.WriteValue(value.List);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignInListResponseInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new SignInListResponseInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Request = reader.ReadValue<SignInListRequestInfo>();
            value.Count = reader.ReadValue<int>();
            value.List = reader.ReadValue<List<SignCacheInfo>>();
        }
    }

    public class SignInIdsRequestInfoFormatter : MemoryPackFormatter<SignInIdsRequestInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SignInIdsRequestInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(3);
            writer.WriteValue(value.Page);
            writer.WriteValue(value.Size);
            writer.WriteValue(value.Name);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignInIdsRequestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new SignInIdsRequestInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Page = reader.ReadValue<int>();
            value.Size = reader.ReadValue<int>();
            value.Name = reader.ReadValue<string>();
        }
    }

    public class SignInIdsResponseInfoFormatter : MemoryPackFormatter<SignInIdsResponseInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SignInIdsResponseInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(3);
            writer.WriteValue(value.Request);
            writer.WriteValue(value.Count);
            writer.WriteValue(value.List);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignInIdsResponseInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new SignInIdsResponseInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Request = reader.ReadValue<SignInIdsRequestInfo>();
            value.Count = reader.ReadValue<int>();
            value.List = reader.ReadValue<List<SignInIdsResponseItemInfo>>();
        }
    }

    public class SignInIdsResponseItemInfoFormatter : MemoryPackFormatter<SignInIdsResponseItemInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SignInIdsResponseItemInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.MachineName);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignInIdsResponseItemInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new SignInIdsResponseItemInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.MachineName = reader.ReadValue<string>();
        }
    }

    public class SignInNamesResponseItemInfoFormatter : MemoryPackFormatter<SignInNamesResponseItemInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SignInNamesResponseItemInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(3);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.MachineName);
            writer.WriteValue(value.Online);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignInNamesResponseItemInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new SignInNamesResponseItemInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.MachineName = reader.ReadValue<string>();
            value.Online = reader.ReadValue<bool>();
        }
    }
   
    public class SignInUserIdsResponseItemInfoFormatter : MemoryPackFormatter<SignInUserIdsResponseItemInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SignInUserIdsResponseItemInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(4);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.MachineName);
            writer.WriteValue(value.UserId);
            writer.WriteValue(value.Online);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignInUserIdsResponseItemInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new SignInUserIdsResponseItemInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.MachineName = reader.ReadValue<string>();
            value.UserId = reader.ReadValue<string>();
            value.Online = reader.ReadValue<bool>();
        }
    }

    public class SignInResponseInfoFormatter : MemoryPackFormatter<SignInResponseInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SignInResponseInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(4);
            writer.WriteValue(value.Status);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.IP);
            writer.WriteValue(value.Msg);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignInResponseInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new SignInResponseInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Status = reader.ReadValue<bool>();
            value.MachineId = reader.ReadValue<string>();
            value.IP = reader.ReadValue<IPEndPoint>();
            value.Msg = reader.ReadValue<string>();
        }
    }

    public class SignInConfigSetNameInfoFormatter : MemoryPackFormatter<SignInConfigSetNameInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SignInConfigSetNameInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(3);
            writer.WriteValue(value.Id);
            writer.WriteValue(value.Name);
            writer.WriteValue(value.Avatar);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignInConfigSetNameInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new SignInConfigSetNameInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Id = reader.ReadValue<string>();
            value.Name = reader.ReadValue<string>();

            if (count > 2)
            {
                value.Avatar = reader.ReadValue<string>();
            }
        }
    }

    public class SignInPushArgInfoFormatter : MemoryPackFormatter<SignInPushArgInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SignInPushArgInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.Key);
            writer.WriteValue(value.Value);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignInPushArgInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new SignInPushArgInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Key = reader.ReadValue<string>();
            value.Value = reader.ReadValue<string>();
        }
    }
}
