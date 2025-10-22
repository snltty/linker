using linker.messenger.signin;
using MemoryPack;
using System.Net;

namespace linker.messenger.serializer.memorypack
{
    [MemoryPackable]
    public readonly partial struct SerializableSignInfo
    {
        [MemoryPackIgnore]
        public readonly SignInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        string MachineName => info.MachineName;

        [MemoryPackInclude]
        string GroupId => info.GroupId;

        [MemoryPackInclude]
        string Version => info.Version;

        [MemoryPackInclude]
        Dictionary<string, string> Args => info.Args;


        [MemoryPackConstructor]
        SerializableSignInfo(string machineId, string machineName, string groupId, string version, Dictionary<string, string> args)
        {
            var info = new SignInfo { MachineId = machineId, MachineName = machineName, Args = args, GroupId = groupId, Version = version };
            this.info = info;
        }

        public SerializableSignInfo(SignInfo signInfo)
        {
            this.info = signInfo;
        }
    }
    public class SignInfoFormatter : MemoryPackFormatter<SignInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SignInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSignInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSignInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableSignCacheInfo
    {
        [MemoryPackIgnore]
        public readonly SignCacheInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;
        [MemoryPackInclude]
        string MachineName => info.MachineName;

        [MemoryPackInclude]
        string Version => info.Version;
        [MemoryPackInclude]
        string GroupId => info.GroupId;

        [MemoryPackInclude]
        DateTime LastSignIn => info.LastSignIn;

        [MemoryPackInclude]
        Dictionary<string, string> Args => info.Args;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPEndPoint IP => info.IP;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        bool Connected => info.Connected;


        [MemoryPackConstructor]
        SerializableSignCacheInfo(string machineId, string machineName, string version, string groupId, DateTime lastSignIn, Dictionary<string, string> args, IPEndPoint ip, bool connected)
        {
            var info = new SignCacheInfo
            {
                Id = string.Empty,
                MachineId = machineId,
                MachineName = machineName,
                GroupId = groupId,
                Version = version,
                Args = args,
                LastSignIn = lastSignIn,
                IP = ip,
                Connected = connected,
                Order = 0
            };
            this.info = info;
        }

        public SerializableSignCacheInfo(SignCacheInfo signInfo)
        {
            this.info = signInfo;
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

            writer.WritePackable(new SerializableSignCacheInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignCacheInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSignCacheInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableSignInListRequestInfo
    {
        [MemoryPackIgnore]
        public readonly SignInListRequestInfo info;

        [MemoryPackInclude]
        int Page => info.Page;
        [MemoryPackInclude]
        int Size => info.Size;
        [MemoryPackInclude]
        string Name => info.Name;
        [MemoryPackInclude]
        string[] Ids => info.Ids;
        [MemoryPackInclude]
        bool Asc => info.Asc;
        [MemoryPackInclude]
        string Prop => info.Prop;

        [MemoryPackConstructor]
        SerializableSignInListRequestInfo(int page, int size, string name, string[] ids, bool asc, string prop)
        {
            var info = new SignInListRequestInfo
            {
                Page = page,
                Size = size,
                Name = name,
                Ids = ids,
                Asc = asc,
                Prop = prop
            };
            this.info = info;
        }

        public SerializableSignInListRequestInfo(SignInListRequestInfo signInfo)
        {
            this.info = signInfo;
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

            writer.WritePackable(new SerializableSignInListRequestInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignInListRequestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSignInListRequestInfo>();
            value = wrapped.info;
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableSignInListResponseInfo
    {
        [MemoryPackIgnore]
        public readonly SignInListResponseInfo info;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        SignInListRequestInfo Request => info.Request;
        [MemoryPackInclude]
        int Count => info.Count;
        [MemoryPackInclude]
        List<SignCacheInfo> List => info.List;

        [MemoryPackConstructor]
        SerializableSignInListResponseInfo(SignInListRequestInfo request, int count, List<SignCacheInfo> list)
        {
            var info = new SignInListResponseInfo
            {
                Request = request,
                Count = count,
                List = list
            };
            this.info = info;
        }

        public SerializableSignInListResponseInfo(SignInListResponseInfo signInfo)
        {
            this.info = signInfo;
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

            writer.WritePackable(new SerializableSignInListResponseInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignInListResponseInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSignInListResponseInfo>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableSignInIdsRequestInfo
    {
        [MemoryPackIgnore]
        public readonly SignInIdsRequestInfo info;

        [MemoryPackInclude]
        int Page => info.Page;
        [MemoryPackInclude]
        int Size => info.Size;
        [MemoryPackInclude]
        string Name => info.Name;

        [MemoryPackConstructor]
        SerializableSignInIdsRequestInfo(int page, int size, string name)
        {
            var info = new SignInIdsRequestInfo
            {
                Page = page,
                Size = size,
                Name = name
            };
            this.info = info;
        }

        public SerializableSignInIdsRequestInfo(SignInIdsRequestInfo signInfo)
        {
            this.info = signInfo;
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

            writer.WritePackable(new SerializableSignInIdsRequestInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignInIdsRequestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSignInIdsRequestInfo>();
            value = wrapped.info;
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableSignInIdsResponseInfo
    {
        [MemoryPackIgnore]
        public readonly SignInIdsResponseInfo info;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        SignInIdsRequestInfo Request => info.Request;
        [MemoryPackInclude]
        int Count => info.Count;
        [MemoryPackInclude]
        List<SignInIdsResponseItemInfo> List => info.List;

        [MemoryPackConstructor]
        SerializableSignInIdsResponseInfo(SignInIdsRequestInfo request, int count, List<SignInIdsResponseItemInfo> list)
        {
            var info = new SignInIdsResponseInfo
            {
                Request = request,
                Count = count,
                List = list
            };
            this.info = info;
        }

        public SerializableSignInIdsResponseInfo(SignInIdsResponseInfo signInfo)
        {
            this.info = signInfo;
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

            writer.WritePackable(new SerializableSignInIdsResponseInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignInIdsResponseInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSignInIdsResponseInfo>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableSignInIdsResponseItemInfo
    {
        [MemoryPackIgnore]
        public readonly SignInIdsResponseItemInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        string MachineName => info.MachineName;


        [MemoryPackConstructor]
        SerializableSignInIdsResponseItemInfo(string machineId, string machineName)
        {
            var info = new SignInIdsResponseItemInfo { MachineId = machineId, MachineName = machineName };
            this.info = info;
        }

        public SerializableSignInIdsResponseItemInfo(SignInIdsResponseItemInfo signInfo)
        {
            this.info = signInfo;
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

            writer.WritePackable(new SerializableSignInIdsResponseItemInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignInIdsResponseItemInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSignInIdsResponseItemInfo>();
            value = wrapped.info;
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableSignInNamesResponseItemInfo
    {
        [MemoryPackIgnore]
        public readonly SignInNamesResponseItemInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        string MachineName => info.MachineName;

        [MemoryPackInclude]
        bool Online => info.Online;

        [MemoryPackConstructor]
        SerializableSignInNamesResponseItemInfo(string machineId, string machineName, bool online)
        {
            var info = new SignInNamesResponseItemInfo { MachineId = machineId, MachineName = machineName, Online = online };
            this.info = info;
        }

        public SerializableSignInNamesResponseItemInfo(SignInNamesResponseItemInfo signInfo)
        {
            this.info = signInfo;
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

            writer.WritePackable(new SerializableSignInNamesResponseItemInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignInNamesResponseItemInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSignInNamesResponseItemInfo>();
            value = wrapped.info;
        }
    }
    [MemoryPackable]
    public readonly partial struct SerializableSignInUserIdsResponseItemInfo
    {
        [MemoryPackIgnore]
        public readonly SignInUserIdsResponseItemInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        string MachineName => info.MachineName;

        [MemoryPackInclude]
        string UserId => info.UserId;

        [MemoryPackInclude]
        bool Online => info.Online;

        [MemoryPackConstructor]
        SerializableSignInUserIdsResponseItemInfo(string machineid, string machineName, string userId, bool online)
        {
            var info = new SignInUserIdsResponseItemInfo { MachineId = machineid, UserId = userId, MachineName = machineName, Online = online };
            this.info = info;
        }

        public SerializableSignInUserIdsResponseItemInfo(SignInUserIdsResponseItemInfo signInfo)
        {
            this.info = signInfo;
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

            writer.WritePackable(new SerializableSignInUserIdsResponseItemInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignInUserIdsResponseItemInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSignInUserIdsResponseItemInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableSignInResponseInfo
    {
        [MemoryPackIgnore]
        public readonly SignInResponseInfo info;

        [MemoryPackInclude]
        bool Status => info.Status;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPEndPoint IP => info.IP;

        [MemoryPackInclude]
        string Msg => info.Msg;


        [MemoryPackConstructor]
        SerializableSignInResponseInfo(bool status, string machineId, IPEndPoint ip, string msg)
        {
            var info = new SignInResponseInfo { Status = status, IP = ip, MachineId = machineId, Msg = msg };
            this.info = info;
        }

        public SerializableSignInResponseInfo(SignInResponseInfo signInfo)
        {
            this.info = signInfo;
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

            writer.WritePackable(new SerializableSignInResponseInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignInResponseInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSignInResponseInfo>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableSignInConfigSetNameInfo
    {
        [MemoryPackIgnore]
        public readonly SignInConfigSetNameInfo info;

        [MemoryPackInclude]
        string Id => info.Id;

        [MemoryPackInclude]
        string NewName => info.NewName;


        [MemoryPackConstructor]
        SerializableSignInConfigSetNameInfo(string id, string newName)
        {
            var info = new SignInConfigSetNameInfo { Id = id, NewName = newName };
            this.info = info;
        }

        public SerializableSignInConfigSetNameInfo(SignInConfigSetNameInfo signInfo)
        {
            this.info = signInfo;
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

            writer.WritePackable(new SerializableSignInConfigSetNameInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignInConfigSetNameInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSignInConfigSetNameInfo>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableSignInPushArgInfo
    {
        [MemoryPackIgnore]
        public readonly SignInPushArgInfo info;

        [MemoryPackInclude]
        string Key => info.Key;

        [MemoryPackInclude]
        string Value => info.Value;


        [MemoryPackConstructor]
        SerializableSignInPushArgInfo(string key, string value)
        {
            var info = new SignInPushArgInfo { Key = key, Value = value };
            this.info = info;
        }

        public SerializableSignInPushArgInfo(SignInPushArgInfo signInfo)
        {
            this.info = signInfo;
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

            writer.WritePackable(new SerializableSignInPushArgInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SignInPushArgInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSignInPushArgInfo>();
            value = wrapped.info;
        }
    }
}
