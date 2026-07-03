using linker.messenger.relay.server;
using linker.tunnel.connection;
using linker.tunnel.transport;
using MemoryPack;
using System.Net;

namespace linker.messenger.serializer.memorypack
{
    public class RelayAskResultInfoFormatter : MemoryPackFormatter<RelayAskResultInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayAskResultInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.MasterId);
            writer.WriteValue(value.Nodes);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayAskResultInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }
            value = new RelayAskResultInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MasterId = reader.ReadValue<string>();
            if (count > 1)
                value.Nodes = reader.ReadValue<List<RelayServerNodeStoreInfo>>();
        }
    }

    public class RelayCacheInfoFormatter : MemoryPackFormatter<RelayCacheInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayCacheInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(9);
            writer.WriteValue(value.FlowId);
            writer.WriteValue(value.FromId);
            writer.WriteValue(value.FromName);
            writer.WriteValue(value.ToId);
            writer.WriteValue(value.ToName);
            writer.WriteValue(value.GroupId);
            writer.WriteValue(value.Super);
            writer.WriteValue(value.Bandwidth);
            writer.WriteValue(value.UserId);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayCacheInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new RelayCacheInfo();
            reader.TryReadObjectHeader(out byte count);
            value.FlowId = reader.ReadValue<ulong>();
            value.FromId = reader.ReadValue<string>();
            value.FromName = reader.ReadValue<string>();
            value.ToId = reader.ReadValue<string>();
            value.ToName = reader.ReadValue<string>();
            value.GroupId = reader.ReadValue<string>();
            value.Super = reader.ReadValue<bool>();
            value.Bandwidth = reader.ReadValue<double>();
            if (count > 8)
                value.UserId = reader.ReadValue<string>();
        }
    }

    public class RelayMessageInfoFormatter : MemoryPackFormatter<RelayMessageInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayMessageInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(5);
            writer.WriteValue(value.Type);
            writer.WriteValue(value.FlowId);
            writer.WriteValue(value.FromId);
            writer.WriteValue(value.ToId);
            writer.WriteValue(value.MasterId);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayMessageInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new RelayMessageInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Type = reader.ReadValue<RelayMessengerType>();
            value.FlowId = reader.ReadValue<ulong>();
            value.FromId = reader.ReadValue<string>();
            value.ToId = reader.ReadValue<string>();
            if (count > 4)
                value.MasterId = reader.ReadValue<string>();
        }
    }

    public class RelayServerNodeReportInfoFormatter : MemoryPackFormatter<RelayServerNodeReportInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayServerNodeReportInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(15);
            writer.WriteValue(value.NodeId);
            writer.WriteValue(value.Name);
            writer.WriteValue(value.Host);
            writer.WriteValue(value.Protocol);
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

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayServerNodeReportInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new RelayServerNodeReportInfo();
            reader.TryReadObjectHeader(out byte count);
            value.NodeId = reader.ReadValue<string>();
            value.Name = reader.ReadValue<string>();
            value.Host = reader.ReadValue<string>();
            value.Protocol = reader.ReadValue<TunnelProtocolType>();
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

    public class RelayServerNodeStoreInfoFormatter : MemoryPackFormatter<RelayServerNodeStoreInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayServerNodeStoreInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(20);
            writer.WriteValue(value.NodeId);
            writer.WriteValue(value.Name);
            writer.WriteValue(value.Host);
            writer.WriteValue(value.Protocol);
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

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayServerNodeStoreInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new RelayServerNodeStoreInfo();
            reader.TryReadObjectHeader(out byte count);
            value.NodeId = reader.ReadValue<string>();
            value.Name = reader.ReadValue<string>();
            value.Host = reader.ReadValue<string>();
            value.Protocol = reader.ReadValue<TunnelProtocolType>();
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

    public class RelayServerNodeReportInfoFormatterOld : MemoryPackFormatter<RelayServerNodeReportInfoOld>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayServerNodeReportInfoOld value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(17);
            writer.WriteValue(value.Id);
            writer.WriteValue(value.Name);
            writer.WriteValue(value.MaxConnection);
            writer.WriteValue(value.MaxBandwidth);
            writer.WriteValue(value.MaxBandwidthTotal);
            writer.WriteValue(value.MaxGbTotal);
            writer.WriteValue(value.MaxGbTotalLastBytes);
            writer.WriteValue(value.ConnectionRatio);
            writer.WriteValue(value.BandwidthRatio);
            writer.WriteValue(value.Public);
            writer.WriteValue(value.Delay);
            writer.WriteValue(value.EndPoint);
            writer.WriteValue(value.LastTicks);
            writer.WriteValue(value.Url);
            writer.WriteValue(value.AllowProtocol);
            writer.WriteValue(value.Sync2Server);
            writer.WriteValue(value.Version);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayServerNodeReportInfoOld value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new RelayServerNodeReportInfoOld();
            reader.TryReadObjectHeader(out byte count);
            value.Id = reader.ReadValue<string>();
            value.Name = reader.ReadValue<string>();
            value.MaxConnection = reader.ReadValue<int>();
            value.MaxBandwidth = reader.ReadValue<double>();
            value.MaxBandwidthTotal = reader.ReadValue<double>();
            value.MaxGbTotal = reader.ReadValue<double>();
            value.MaxGbTotalLastBytes = reader.ReadValue<long>();
            value.ConnectionRatio = reader.ReadValue<double>();
            value.BandwidthRatio = reader.ReadValue<double>();
            value.Public = reader.ReadValue<bool>();
            value.Delay = reader.ReadValue<int>();
            value.EndPoint = reader.ReadValue<IPEndPoint>();
            value.LastTicks = reader.ReadValue<long>();
            if (count > 13)
                value.Url = reader.ReadValue<string>();
            if (count > 14)
                value.AllowProtocol = reader.ReadValue<TunnelProtocolType>();
            if (count > 15)
                value.Sync2Server = reader.ReadValue<bool>();
            if (count > 16)
                value.Version = reader.ReadValue<string>();
        }
    }
}
