using linker.messenger.tuntap;
using linker.messenger.tuntap.lease;
using MemoryPack;
using System.Net;

namespace linker.messenger.serializer.memorypack
{
    public class TuntapVeaLanIPAddressFormatter : MemoryPackFormatter<TuntapVeaLanIPAddress>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TuntapVeaLanIPAddress value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(5);
            writer.WriteValue(value.IPAddress);
            writer.WriteValue(value.PrefixLength);
            writer.WriteValue(value.MaskValue);
            writer.WriteValue(value.NetWork);
            writer.WriteValue(value.Broadcast);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TuntapVeaLanIPAddress value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new TuntapVeaLanIPAddress();
            reader.TryReadObjectHeader(out byte count);
            value.IPAddress = reader.ReadValue<uint>();
            value.PrefixLength = reader.ReadValue<byte>();
            value.MaskValue = reader.ReadValue<uint>();
            value.NetWork = reader.ReadValue<uint>();
            value.Broadcast = reader.ReadValue<uint>();
        }
    }

    public class TuntapVeaLanIPAddressListFormatter : MemoryPackFormatter<TuntapVeaLanIPAddressList>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TuntapVeaLanIPAddressList value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(4);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.IPS);
            writer.WriteValue(value.DstIp);
            writer.WriteValue(value.DstPrefixValue);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TuntapVeaLanIPAddressList value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new TuntapVeaLanIPAddressList();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.IPS = reader.ReadValue<List<TuntapVeaLanIPAddress>>();
            value.DstIp = reader.ReadValue<uint>();
            value.DstPrefixValue = reader.ReadValue<uint>();
        }
    }

    public class TuntapInfoFormatter : MemoryPackFormatter<TuntapInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TuntapInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(17);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Status);
            writer.WriteValue(value.IP);
            writer.WriteValue(value.PrefixLength);
            writer.WriteValue(value.Name);
            writer.WriteValue(value.Lans);
            writer.WriteValue(value.Wan);
            writer.WriteValue(value.SetupError);
            writer.WriteValue(value.NatError);
            writer.WriteValue(value.SystemInfo);
            writer.WriteValue(value.Forwards);
            writer.WriteValue(value.Switch);
            writer.WriteValue(value.NetworkName);
            writer.WriteValue(value.Mtu);
            writer.WriteValue(value.MssFix);
            writer.WriteValue(value.VlsmStatus);
            writer.WriteValue(value.FecProfile);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TuntapInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            reader.TryReadObjectHeader(out byte count);
            value = new TuntapInfo();
            value.MachineId = reader.ReadValue<string>();
            value.Status = reader.ReadValue<TuntapStatus>();
            value.IP = reader.ReadValue<IPAddress>();
            value.PrefixLength = reader.ReadValue<byte>();
            value.Name = reader.ReadValue<string>();
            value.Lans = reader.ReadValue<List<TuntapLanInfo>>();
            value.Wan = reader.ReadValue<IPAddress>();
            value.SetupError = reader.ReadValue<string>();
            value.NatError = reader.ReadValue<string>();
            value.SystemInfo = reader.ReadValue<string>();
            value.Forwards = reader.ReadValue<List<TuntapForwardInfo>>();
            value.Switch = reader.ReadValue<TuntapSwitch>();

            if (count > 12)
                value.NetworkName = reader.ReadValue<string>();

            if (count > 13)
                value.Mtu = reader.ReadValue<int>();

            if (count > 14)
                value.MssFix = reader.ReadValue<int>();

            if (count > 15)
                value.VlsmStatus = reader.ReadValue<TuntapVlsmStatus>();

            if (count > 16)
                value.FecProfile = reader.ReadValue<List<TuntapFecProfileInfo>>();
        }
    }

    public class TuntapForwardInfoFormatter : MemoryPackFormatter<TuntapForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TuntapForwardInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(6);
            writer.WriteValue(value.ListenAddr);
            writer.WriteValue(value.ListenPort);
            writer.WriteValue(value.ConnectAddr);
            writer.WriteValue(value.ConnectPort);
            writer.WriteValue(value.Remark);
            writer.WriteValue(value.Disabled);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TuntapForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            reader.TryReadObjectHeader(out byte count);
            value = new TuntapForwardInfo();
            value.ListenAddr = reader.ReadValue<IPAddress>();
            value.ListenPort = reader.ReadValue<int>();
            value.ConnectAddr = reader.ReadValue<IPAddress>();
            value.ConnectPort = reader.ReadValue<int>();
            value.Remark = reader.ReadValue<string>();
            value.Disabled = reader.ReadValue<bool>();
        }
    }

    public class TuntapForwardTestWrapInfoFormatter : MemoryPackFormatter<TuntapForwardTestWrapInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TuntapForwardTestWrapInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.List);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TuntapForwardTestWrapInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new TuntapForwardTestWrapInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.List = reader.ReadValue<List<TuntapForwardTestInfo>>();
        }
    }

    public class TuntapForwardTestInfoFormatter : MemoryPackFormatter<TuntapForwardTestInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TuntapForwardTestInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(4);
            writer.WriteValue(value.ListenPort);
            writer.WriteValue(value.ConnectAddr);
            writer.WriteValue(value.ConnectPort);
            writer.WriteValue(value.Error);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TuntapForwardTestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new TuntapForwardTestInfo();
            reader.TryReadObjectHeader(out byte count);
            value.ListenPort = reader.ReadValue<int>();
            value.ConnectAddr = reader.ReadValue<IPAddress>();
            value.ConnectPort = reader.ReadValue<int>();
            value.Error = reader.ReadValue<string>();
        }
    }

    public class TuntapLanInfoFormatter : MemoryPackFormatter<TuntapLanInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TuntapLanInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(8);
            writer.WriteValue(value.IP);
            writer.WriteValue(value.PrefixLength);
            writer.WriteValue(value.Disabled);
            writer.WriteValue(value.Exists);
            writer.WriteValue(value.Error);
            writer.WriteValue(value.MapIP);
            writer.WriteValue(value.MapPrefixLength);
            writer.WriteValue(value.Remark);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TuntapLanInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            reader.TryReadObjectHeader(out byte count);
            value = new TuntapLanInfo();
            value.IP = reader.ReadValue<IPAddress>();
            value.PrefixLength = reader.ReadValue<byte>();
            value.Disabled = reader.ReadValue<bool>();
            value.Exists = reader.ReadValue<bool>();
            value.Error = reader.ReadValue<string>();
            value.MapIP = reader.ReadValue<IPAddress>();
            value.MapPrefixLength = reader.ReadValue<byte>();
            if (count > 7)
                value.Remark = reader.ReadValue<string>();

            //reader.Advance(count);
        }
    }

    public class LeaseInfoFormatter : MemoryPackFormatter<LeaseInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref LeaseInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(8);
            writer.WriteValue(value.IP);
            writer.WriteValue(value.PrefixLength);
            writer.WriteValue(value.Name);
            writer.WriteValue(value.SubName);
            writer.WriteValue(value.Subs);
            writer.WriteValue(value.Mtu);
            writer.WriteValue(value.MssFix);
            writer.WriteValue(value.VlsmStatus);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref LeaseInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            reader.TryReadObjectHeader(out byte count);
            value = new LeaseInfo();
            value.IP = reader.ReadValue<IPAddress>();
            value.PrefixLength = reader.ReadValue<byte>();
            value.Name = reader.ReadValue<string>();
            if (count > 3)
                value.SubName = reader.ReadValue<string>();

            if (count > 4)
                value.Subs = reader.ReadValue<List<LeaseSubInfo>>();

            if (count > 5)
                value.Mtu = reader.ReadValue<int>();

            if (count > 6)
                value.MssFix = reader.ReadValue<int>();

            if (count > 7)
                value.VlsmStatus = reader.ReadValue<TuntapVlsmStatus>();

        }
    }

    public class LeaseSubInfoFormatter : MemoryPackFormatter<LeaseSubInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref LeaseSubInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(3);
            writer.WriteValue(value.IP);
            writer.WriteValue(value.PrefixLength);
            writer.WriteValue(value.Name);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref LeaseSubInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            reader.TryReadObjectHeader(out byte count);
            value = new LeaseSubInfo();
            value.IP = reader.ReadValue<IPAddress>();
            value.PrefixLength = reader.ReadValue<byte>();
            value.Name = reader.ReadValue<string>();

        }
    }

    public class TuntapFecProfileInfoFormatter : MemoryPackFormatter<TuntapFecProfileInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TuntapFecProfileInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.SourceSymbols);
            writer.WriteValue(value.RepairSymbols);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TuntapFecProfileInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            reader.TryReadObjectHeader(out byte count);
            value = new TuntapFecProfileInfo();
            value.SourceSymbols = reader.ReadValue<int>();
            value.RepairSymbols = reader.ReadValue<int>();

        }
    }
}
