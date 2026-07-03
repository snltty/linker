using linker.messenger.reverse;
using linker.messenger.reverse.server;
using MemoryPack;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.serializer.memorypack
{
    public class ReverseAddResultInfoFormatter : MemoryPackFormatter<ReverseAddResultInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ReverseAddResultInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(3);
            writer.WriteValue(value.Success);
            writer.WriteValue(value.Message);
            writer.WriteValue(value.BufferSize);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ReverseAddResultInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new ReverseAddResultInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Success = reader.ReadValue<bool>();
            value.Message = reader.ReadValue<string>();
            value.BufferSize = reader.ReadValue<byte>();
        }
    }

    public class ReverseAddForwardInfoFormatter : MemoryPackFormatter<ReverseAddForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ReverseAddForwardInfo value)
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

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ReverseAddForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new ReverseAddForwardInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Data = reader.ReadValue<ReverseInfo>();
        }
    }

    public class ReverseRemoveForwardInfoFormatter : MemoryPackFormatter<ReverseRemoveForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ReverseRemoveForwardInfo value)
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

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ReverseRemoveForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new ReverseRemoveForwardInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Id = reader.ReadValue<int>();

        }
    }

    public class ReverseProxyInfoFormatter : MemoryPackFormatter<ReverseProxyInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ReverseProxyInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(8);
            writer.WriteValue(value.Id);
            writer.WriteValue(value.Domain);
            writer.WriteValue(value.RemotePort);
            writer.WriteValue(value.BufferSize);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.NodeId);
            writer.WriteValue(value.ProtocolType);
            writer.WriteValue(value.Addr);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ReverseProxyInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new ReverseProxyInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Id = reader.ReadValue<ulong>();
            value.Domain = reader.ReadValue<string>();
            value.RemotePort = reader.ReadValue<int>();
            value.BufferSize = reader.ReadValue<byte>();
            value.MachineId = reader.ReadValue<string>();
            value.NodeId = reader.ReadValue<string>();
            value.ProtocolType = reader.ReadValue<ProtocolType>();
            value.Addr = reader.ReadValue<IPAddress>();
        }
    }

    public class ReverseAddInfoFormatter : MemoryPackFormatter<ReverseAddInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ReverseAddInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(7);
            writer.WriteValue(value.Domain);
            writer.WriteValue(value.RemotePort);
            writer.WriteValue(value.NodeId);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.GroupId);
            writer.WriteValue(value.Super);
            writer.WriteValue(value.Bandwidth);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ReverseAddInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new ReverseAddInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Domain = reader.ReadValue<string>();
            value.RemotePort = reader.ReadValue<int>();
            if (count > 2)
            {
                value.NodeId = reader.ReadValue<string>();
                value.MachineId = reader.ReadValue<string>();
                value.GroupId = reader.ReadValue<string>();
                value.Super = reader.ReadValue<bool>();
                value.Bandwidth = reader.ReadValue<double>();
            }

        }
    }
  
    public class ReverseInfoFormatter : MemoryPackFormatter<ReverseInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ReverseInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(12);
            writer.WriteValue(value.Id);
            writer.WriteValue(value.Name);
            writer.WriteValue(value.Domain);
            writer.WriteValue(value.RemotePort);
            writer.WriteValue(value.BufferSize);
            writer.WriteValue(value.LocalEP);
            writer.WriteValue(value.Started);
            writer.WriteValue(value.Msg);
            writer.WriteValue(value.LocalMsg);
            writer.WriteValue(value.RemotePortMin);
            writer.WriteValue(value.RemotePortMax);
            writer.WriteValue(value.NodeId);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ReverseInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new ReverseInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Id = reader.ReadValue<long>();
            value.Name = reader.ReadValue<string>();
            value.Domain = reader.ReadValue<string>();
            value.RemotePort = reader.ReadValue<int>();
            value.BufferSize = reader.ReadValue<byte>();
            value.LocalEP = reader.ReadValue<IPEndPoint>();
            value.Started = reader.ReadValue<bool>();
            value.Msg = reader.ReadValue<string>();
            value.LocalMsg = reader.ReadValue<string>();
            value.RemotePortMin = reader.ReadValue<int>();
            value.RemotePortMax = reader.ReadValue<int>();
            if (count > 11)
            {
                value.NodeId = reader.ReadValue<string>();
            }
        }
    }

    public class ReverseServerNodeReportInfoFormatter : MemoryPackFormatter<ReverseServerNodeReportInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ReverseServerNodeReportInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(17);
            writer.WriteValue(value.NodeId);
            writer.WriteValue(value.Name);
            writer.WriteValue(value.Host);
            writer.WriteValue(value.Domain);
            writer.WriteValue(value.WebPort);
            writer.WriteValue(value.TunnelPorts);
            writer.WriteValue(value.Connections);
            writer.WriteValue(value.Bandwidth);
            writer.WriteValue(value.DataEachMonth);
            writer.WriteValue(value.DataRemain);
            writer.WriteValue(value.Url);
            writer.WriteValue(value.Logo);
            writer.WriteValue(value.MasterKey);
            writer.WriteValue(value.Version);
            writer.WriteValue(value.ConnectionsRatio);
            writer.WriteValue(value.BandwidthRatio);
            writer.WriteValue(value.MasterCount);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ReverseServerNodeReportInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new ReverseServerNodeReportInfo();
            reader.TryReadObjectHeader(out byte count);
            value.NodeId = reader.ReadValue<string>();
            value.Name = reader.ReadValue<string>();
            value.Host = reader.ReadValue<string>();
            value.Domain = reader.ReadValue<string>();
            value.WebPort = reader.ReadValue<int>();
            value.TunnelPorts = reader.ReadValue<string>();
            value.Connections = reader.ReadValue<int>();
            value.Bandwidth = reader.ReadValue<int>();
            value.DataEachMonth = reader.ReadValue<int>();
            value.DataRemain = reader.ReadValue<long>();
            value.Url = reader.ReadValue<string>();
            value.Logo = reader.ReadValue<string>();
            value.MasterKey = reader.ReadValue<string>();
            value.Version = reader.ReadValue<string>();
            value.ConnectionsRatio = reader.ReadValue<int>();
            value.BandwidthRatio = reader.ReadValue<double>();
            value.MasterCount = reader.ReadValue<int>();

        }
    }

    public class ReverseServerNodeStoreInfoFormatter : MemoryPackFormatter<ReverseServerNodeStoreInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ReverseServerNodeStoreInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(22);
            writer.WriteValue(value.NodeId);
            writer.WriteValue(value.Name);
            writer.WriteValue(value.Host);
            writer.WriteValue(value.Domain);
            writer.WriteValue(value.WebPort);
            writer.WriteValue(value.TunnelPorts);
            writer.WriteValue(value.Connections);
            writer.WriteValue(value.Bandwidth);
            writer.WriteValue(value.DataEachMonth);
            writer.WriteValue(value.DataRemain);
            writer.WriteValue(value.Url);
            writer.WriteValue(value.Logo);
            writer.WriteValue(value.MasterKey);
            writer.WriteValue(value.Version);
            writer.WriteValue(value.ConnectionsRatio);
            writer.WriteValue(value.BandwidthRatio);
            writer.WriteValue(value.MasterCount);
            writer.WriteValue(value.Id);
            writer.WriteValue(value.BandwidthEach);
            writer.WriteValue(value.Public);
            writer.WriteValue(value.LastTicks);
            writer.WriteValue(value.Manageable);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ReverseServerNodeStoreInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new ReverseServerNodeStoreInfo();
            reader.TryReadObjectHeader(out byte count);
            value.NodeId = reader.ReadValue<string>();
            value.Name = reader.ReadValue<string>();
            value.Host = reader.ReadValue<string>();
            value.Domain = reader.ReadValue<string>();
            value.WebPort = reader.ReadValue<int>();
            value.TunnelPorts = reader.ReadValue<string>();
            value.Connections = reader.ReadValue<int>();
            value.Bandwidth = reader.ReadValue<int>();
            value.DataEachMonth = reader.ReadValue<int>();
            value.DataRemain = reader.ReadValue<long>();
            value.Url = reader.ReadValue<string>();
            value.Logo = reader.ReadValue<string>();
            value.MasterKey = reader.ReadValue<string>();
            value.Version = reader.ReadValue<string>();
            value.ConnectionsRatio = reader.ReadValue<int>();
            value.BandwidthRatio = reader.ReadValue<double>();
            value.MasterCount = reader.ReadValue<int>();
            value.Id = reader.ReadValue<int>();

            value.BandwidthEach = reader.ReadValue<int>();
            value.Public = reader.ReadValue<bool>();
            value.LastTicks = reader.ReadValue<long>();
            value.Manageable = reader.ReadValue<bool>();
        }
    }

    public class ReverseServerNodeReportInfoOldFormatter : MemoryPackFormatter<ReverseServerNodeReportInfoOld>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ReverseServerNodeReportInfoOld value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(17);
            writer.WriteValue(value.Id);
            writer.WriteValue(value.MaxBandwidth);
            writer.WriteValue(value.MaxBandwidthTotal);
            writer.WriteValue(value.MaxGbTotal);
            writer.WriteValue(value.MaxGbTotalLastBytes);
            writer.WriteValue(value.BandwidthRatio);
            writer.WriteValue(value.Name);
            writer.WriteValue(value.Public);
            writer.WriteValue(value.Delay);
            writer.WriteValue(value.Domain);
            writer.WriteValue(value.Address);
            writer.WriteValue(value.LastTicks);
            writer.WriteValue(value.Url);
            writer.WriteValue(value.Sync2Server);
            writer.WriteValue(value.Version);
            writer.WriteValue(value.WebPort);
            writer.WriteValue(value.PortRange);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ReverseServerNodeReportInfoOld value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new ReverseServerNodeReportInfoOld();
            reader.TryReadObjectHeader(out byte count);
            value.Id = reader.ReadValue<string>();
            value.MaxBandwidth = reader.ReadValue<double>();
            value.MaxBandwidthTotal = reader.ReadValue<double>();
            value.MaxGbTotal = reader.ReadValue<double>();
            value.MaxGbTotalLastBytes = reader.ReadValue<long>();
            value.BandwidthRatio = reader.ReadValue<double>();
            value.Name = reader.ReadValue<string>();
            value.Public = reader.ReadValue<bool>();
            value.Delay = reader.ReadValue<int>();
            value.Domain = reader.ReadValue<string>();
            value.Address = reader.ReadValue<IPAddress>();
            value.LastTicks = reader.ReadValue<long>();
            value.Url = reader.ReadValue<string>();
            value.Sync2Server = reader.ReadValue<bool>();
            value.Version = reader.ReadValue<string>();
            value.WebPort = reader.ReadValue<int>();
            value.PortRange = reader.ReadValue<int[]>();
        }
    }


}
