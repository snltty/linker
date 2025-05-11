using linker.messenger.firewall;
using linker.snat;
using MemoryPack;

namespace linker.messenger.serializer.memorypack
{
    [MemoryPackable]
    public readonly partial struct SerializableFirewallRuleInfo
    {
        [MemoryPackIgnore]
        public readonly FirewallRuleInfo info;

        [MemoryPackInclude]
        string Id => info.Id;

        [MemoryPackInclude]
        string SrcId => info.SrcId;

        [MemoryPackInclude]
        string SrcName => info.SrcName;

        [MemoryPackInclude]
        string GroupId => info.GroupId;

        [MemoryPackInclude]
        string DstCIDR => info.DstCIDR;

        [MemoryPackInclude]
        string DstPort => info.DstPort;

        [MemoryPackInclude]
        snat.LinkerFirewallProtocolType Protocol => info.Protocol;

        [MemoryPackInclude]
        snat.LinkerFirewallAction Action => info.Action;

        [MemoryPackInclude]
        bool Disabled => info.Disabled;

        [MemoryPackInclude]
        int OrderBy => info.OrderBy;

        [MemoryPackInclude]
        string Remark => info.Remark;

        [MemoryPackConstructor]
        SerializableFirewallRuleInfo(string id, string srcId, string srcName, string groupId, string dstCIDR, string dstPort,
            snat.LinkerFirewallProtocolType protocol, snat.LinkerFirewallAction action, bool disabled, int orderby, string remark)
        {
            var info = new FirewallRuleInfo
            {
                Id = id,
                SrcId = srcId,
                SrcName = srcName,
                GroupId = groupId,
                DstCIDR = dstCIDR,
                DstPort = dstPort,
                Protocol = protocol,
                Action = action,
                Disabled = disabled,
                OrderBy = orderby,
                Remark = remark
            };
            this.info = info;
        }

        public SerializableFirewallRuleInfo(FirewallRuleInfo info)
        {
            this.info = info;
        }
    }
    public class FirewallRuleInfoFormatter : MemoryPackFormatter<FirewallRuleInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref FirewallRuleInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableFirewallRuleInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref FirewallRuleInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableFirewallRuleInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableFirewallSearchInfo
    {
        [MemoryPackIgnore]
        public readonly FirewallSearchInfo info;



        [MemoryPackInclude]
        string GroupId => info.GroupId;

        [MemoryPackInclude]
        string Str => info.Str;

        [MemoryPackInclude]
        int Disabled => info.Disabled;

        [MemoryPackInclude]
        snat.LinkerFirewallProtocolType Protocol => info.Protocol;

        [MemoryPackInclude]
        snat.LinkerFirewallAction Action => info.Action;

        [MemoryPackConstructor]
        SerializableFirewallSearchInfo(string groupId, string str, snat.LinkerFirewallProtocolType protocol,
            snat.LinkerFirewallAction action, int disabled)
        {
            var info = new FirewallSearchInfo
            {
                GroupId = groupId,
                Str = str,
                Protocol = protocol,
                Action = action,
                Disabled = disabled,
            };
            this.info = info;
        }

        public SerializableFirewallSearchInfo(FirewallSearchInfo info)
        {
            this.info = info;
        }
    }
    public class FirewallSearchInfoFormatter : MemoryPackFormatter<FirewallSearchInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref FirewallSearchInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableFirewallSearchInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref FirewallSearchInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableFirewallSearchInfo>();
            value = wrapped.info;
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableFirewallSearchForwardInfo
    {
        [MemoryPackIgnore]
        public readonly FirewallSearchForwardInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        FirewallSearchInfo Data => info.Data;

        [MemoryPackConstructor]
        SerializableFirewallSearchForwardInfo(string machineId, FirewallSearchInfo data)
        {
            this.info = new FirewallSearchForwardInfo
            {
                MachineId = machineId,
                Data = data
            };
        }

        public SerializableFirewallSearchForwardInfo(FirewallSearchForwardInfo info)
        {
            this.info = info;
        }
    }
    public class FirewallSearchForwardInfoFormatter : MemoryPackFormatter<FirewallSearchForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref FirewallSearchForwardInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableFirewallSearchForwardInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref FirewallSearchForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableFirewallSearchForwardInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableFirewallListInfo
    {
        [MemoryPackIgnore]
        public readonly FirewallListInfo info;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        LinkerFirewallState State => info.State;

        [MemoryPackInclude]
        List<FirewallRuleInfo> List => info.List;

        [MemoryPackConstructor]
        SerializableFirewallListInfo(LinkerFirewallState state, List<FirewallRuleInfo> list)
        {
            this.info = new FirewallListInfo
            {
                List = list,
                State = state
            };
        }

        public SerializableFirewallListInfo(FirewallListInfo info)
        {
            this.info = info;
        }
    }
    public class FirewallListInfoFormatter : MemoryPackFormatter<FirewallListInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref FirewallListInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableFirewallListInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref FirewallListInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableFirewallListInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableFirewallAddForwardInfo
    {
        [MemoryPackIgnore]
        public readonly FirewallAddForwardInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        FirewallRuleInfo Data => info.Data;

        [MemoryPackConstructor]
        SerializableFirewallAddForwardInfo(string machineId, FirewallRuleInfo data)
        {
            this.info = new FirewallAddForwardInfo
            {
                MachineId = machineId,
                Data = data
            };
        }

        public SerializableFirewallAddForwardInfo(FirewallAddForwardInfo info)
        {
            this.info = info;
        }
    }
    public class FirewallAddForwardInfoFormatter : MemoryPackFormatter<FirewallAddForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref FirewallAddForwardInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableFirewallAddForwardInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref FirewallAddForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableFirewallAddForwardInfo>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableFirewallRemoveForwardInfo
    {
        [MemoryPackIgnore]
        public readonly FirewallRemoveForwardInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        string Id => info.Id;

        [MemoryPackConstructor]
        SerializableFirewallRemoveForwardInfo(string machineId, string id)
        {
            this.info = new FirewallRemoveForwardInfo
            {
                MachineId = machineId,
                Id = id
            };
        }

        public SerializableFirewallRemoveForwardInfo(FirewallRemoveForwardInfo info)
        {
            this.info = info;
        }
    }
    public class FirewallRemoveForwardInfoFormatter : MemoryPackFormatter<FirewallRemoveForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref FirewallRemoveForwardInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableFirewallRemoveForwardInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref FirewallRemoveForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableFirewallRemoveForwardInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableFirewallStateForwardInfo
    {
        [MemoryPackIgnore]
        public readonly FirewallStateForwardInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        LinkerFirewallState State => info.State;

        [MemoryPackConstructor]
        SerializableFirewallStateForwardInfo(string machineId, LinkerFirewallState state)
        {
            this.info = new FirewallStateForwardInfo
            {
                MachineId = machineId,
                State = state
            };
        }

        public SerializableFirewallStateForwardInfo(FirewallStateForwardInfo info)
        {
            this.info = info;
        }
    }
    public class FirewallStateForwardInfoFormatter : MemoryPackFormatter<FirewallStateForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref FirewallStateForwardInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableFirewallStateForwardInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref FirewallStateForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableFirewallStateForwardInfo>();
            value = wrapped.info;
        }
    }
}
