using linker.messenger.firewall;
using linker.nat;
using MemoryPack;

namespace linker.messenger.serializer.memorypack
{
    public class FirewallRuleInfoFormatter : MemoryPackFormatter<FirewallRuleInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref FirewallRuleInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(11);
            writer.WriteValue(value.Id);
            writer.WriteValue(value.SrcId);
            writer.WriteValue(value.SrcName);
            writer.WriteValue(value.GroupId);
            writer.WriteValue(value.DstCIDR);
            writer.WriteValue(value.DstPort);
            writer.WriteValue(value.Protocol);
            writer.WriteValue(value.Action);
            writer.WriteValue(value.Disabled);
            writer.WriteValue(value.OrderBy);
            writer.WriteValue(value.Remark);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref FirewallRuleInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new FirewallRuleInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Id = reader.ReadValue<string>();
            value.SrcId = reader.ReadValue<string>();
            value.SrcName = reader.ReadValue<string>();
            value.GroupId = reader.ReadValue<string>();
            value.DstCIDR = reader.ReadValue<string>();
            value.DstPort = reader.ReadValue<string>();
            value.Protocol = reader.ReadValue<LinkerFirewallProtocolType>();
            value.Action = reader.ReadValue<LinkerFirewallAction>();
            value.Disabled = reader.ReadValue<bool>();
            value.OrderBy = reader.ReadValue<int>();
            value.Remark = reader.ReadValue<string>();
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

            writer.WriteObjectHeader(5);
            writer.WriteValue(value.GroupId);
            writer.WriteValue(value.Str);
            writer.WriteValue(value.Disabled);
            writer.WriteValue(value.Protocol);
            writer.WriteValue(value.Action);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref FirewallSearchInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new FirewallSearchInfo();
            reader.TryReadObjectHeader(out byte count);
            value.GroupId = reader.ReadString();
            value.Str = reader.ReadString();
            value.Disabled = reader.ReadValue<int>();
            value.Protocol = reader.ReadValue<LinkerFirewallProtocolType>();
            value.Action = reader.ReadValue<LinkerFirewallAction>();
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
            writer.WriteObjectHeader(2);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Data);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref FirewallSearchForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }
            value = new FirewallSearchForwardInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Data = reader.ReadValue<FirewallSearchInfo>();
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

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.State);
            writer.WriteValue(value.List);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref FirewallListInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new FirewallListInfo();
            reader.TryReadObjectHeader(out byte count);
            value.State = reader.ReadValue<LinkerFirewallState>();
            value.List = reader.ReadValue<List<FirewallRuleInfo>>();
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

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Data);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref FirewallAddForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new FirewallAddForwardInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Data = reader.ReadValue<FirewallRuleInfo>();
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

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Id);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref FirewallRemoveForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new FirewallRemoveForwardInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Id = reader.ReadValue<string>();
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

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.State);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref FirewallStateForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new FirewallStateForwardInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.State = reader.ReadValue<LinkerFirewallState>();
        }
    }

    public class FirewallCheckInfoFormatter : MemoryPackFormatter<FirewallCheckInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref FirewallCheckInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.Ids);
            writer.WriteValue(value.IsChecked);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref FirewallCheckInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new FirewallCheckInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Ids = reader.ReadValue<List<string>>();
            value.IsChecked = reader.ReadValue<bool>();
        }
    }
 
    public class FirewallCheckForwardInfoFormatter : MemoryPackFormatter<FirewallCheckForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref FirewallCheckForwardInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Data);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref FirewallCheckForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new FirewallCheckForwardInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Data = reader.ReadValue<FirewallCheckInfo>();
        }
    }
}
