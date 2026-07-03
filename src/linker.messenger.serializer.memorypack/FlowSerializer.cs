using MemoryPack;
using linker.messenger.flow;
using System.Net;
using linker.tunnel.connection;

namespace linker.messenger.serializer.memorypack
{
    public class FlowItemInfoFormatter : MemoryPackFormatter<FlowItemInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref FlowItemInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.ReceiveBytes);
            writer.WriteValue(value.SendtBytes);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref FlowItemInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new FlowItemInfo();
            reader.TryReadObjectHeader(out byte count);
            value.ReceiveBytes = reader.ReadValue<long>();
            value.SendtBytes = reader.ReadValue<long>();
        }
    }

    public class FlowReportNetInfoFormatter : MemoryPackFormatter<FlowReportNetInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref FlowReportNetInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(4);
            writer.WriteValue(value.City);
            writer.WriteValue(value.Lat);
            writer.WriteValue(value.Lon);
            writer.WriteValue(value.Count);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref FlowReportNetInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new FlowReportNetInfo();
            reader.TryReadObjectHeader(out byte count);
            value.City = reader.ReadValue<string>();
            value.Lat = reader.ReadValue<double>();
            value.Lon = reader.ReadValue<double>();
            value.Count = reader.ReadValue<int>();
        }
    }

    public class FlowInfoFormatter : MemoryPackFormatter<FlowInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref FlowInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(3);
            writer.WriteValue(value.Items);
            writer.WriteValue(value.Start);
            writer.WriteValue(value.Now);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref FlowInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new FlowInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Items = reader.ReadValue<Dictionary<string, FlowItemInfo>>();
            value.Start = reader.ReadValue<DateTime>();
            value.Now = reader.ReadValue<DateTime>();
        }
    }

    public class RelayFlowItemInfoFormatter : MemoryPackFormatter<RelayFlowItemInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayFlowItemInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(6);
            writer.WriteValue(value.ReceiveBytes);
            writer.WriteValue(value.SendtBytes);
            writer.WriteValue(value.DiffReceiveBytes);
            writer.WriteValue(value.DiffSendtBytes);
            writer.WriteValue(value.FromName);
            writer.WriteValue(value.ToName);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayFlowItemInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new RelayFlowItemInfo();
            reader.TryReadObjectHeader(out byte count);
            value.ReceiveBytes = reader.ReadValue<long>();
            value.SendtBytes = reader.ReadValue<long>();
            value.DiffReceiveBytes = reader.ReadValue<long>();
            value.DiffSendtBytes = reader.ReadValue<long>();
            value.FromName = reader.ReadValue<string>();
            value.ToName = reader.ReadValue<string>();
        }
    }

    public class RelayFlowRequestInfoFormatter : MemoryPackFormatter<RelayFlowRequestInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayFlowRequestInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(6);
            writer.WriteValue(value.Key);
            writer.WriteValue(value.SecretKey);
            writer.WriteValue(value.Page);
            writer.WriteValue(value.PageSize);
            writer.WriteValue(value.Order);
            writer.WriteValue(value.OrderType);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayFlowRequestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new RelayFlowRequestInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Key = reader.ReadValue<string>();
            value.SecretKey = reader.ReadValue<string>();
            value.Page = reader.ReadValue<int>();
            value.PageSize = reader.ReadValue<int>();
            value.Order = reader.ReadValue<RelayFlowOrder>();
            value.OrderType = reader.ReadValue<RelayFlowOrderType>();
        }
    }

    public class RelayFlowResponseInfoFormatter : MemoryPackFormatter<RelayFlowResponseInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayFlowResponseInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(4);
            writer.WriteValue(value.Page);
            writer.WriteValue(value.PageSize);
            writer.WriteValue(value.Count);
            writer.WriteValue(value.Data);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayFlowResponseInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new RelayFlowResponseInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Page = reader.ReadValue<int>();
            value.PageSize = reader.ReadValue<int>();
            value.Count = reader.ReadValue<int>();
            value.Data = reader.ReadValue<List<RelayFlowItemInfo>>();
        }
    }

    public class ReverseFlowItemInfoFormatter : MemoryPackFormatter<ReverseFlowItemInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ReverseFlowItemInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(5);
            writer.WriteValue(value.ReceiveBytes);
            writer.WriteValue(value.SendtBytes);
            writer.WriteValue(value.DiffReceiveBytes);
            writer.WriteValue(value.DiffSendtBytes);
            writer.WriteValue(value.Key);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ReverseFlowItemInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new ReverseFlowItemInfo();
            reader.TryReadObjectHeader(out byte count);
            value.ReceiveBytes = reader.ReadValue<long>();
            value.SendtBytes = reader.ReadValue<long>();
            value.DiffReceiveBytes = reader.ReadValue<long>();
            value.DiffSendtBytes = reader.ReadValue<long>();
            value.Key = reader.ReadValue<string>();
        }
    }

    public class ReverseFlowRequestInfoFormatter : MemoryPackFormatter<ReverseFlowRequestInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ReverseFlowRequestInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(6);
            writer.WriteValue(value.Key);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Page);
            writer.WriteValue(value.PageSize);
            writer.WriteValue(value.Order);
            writer.WriteValue(value.OrderType);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ReverseFlowRequestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new ReverseFlowRequestInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Key = reader.ReadValue<string>();
            value.MachineId = reader.ReadValue<string>();
            value.Page = reader.ReadValue<int>();
            value.PageSize = reader.ReadValue<int>();
            value.Order = reader.ReadValue<ReverseFlowOrder>();
            value.OrderType = reader.ReadValue<ReverseFlowOrderType>();
        }
    }

    public class ReverseFlowResponseInfoFormatter : MemoryPackFormatter<ReverseFlowResponseInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ReverseFlowResponseInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(4);
            writer.WriteValue(value.Page);
            writer.WriteValue(value.PageSize);
            writer.WriteValue(value.Count);
            writer.WriteValue(value.Data);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ReverseFlowResponseInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new ReverseFlowResponseInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Page = reader.ReadValue<int>();
            value.PageSize = reader.ReadValue<int>();
            value.Count = reader.ReadValue<int>();
            value.Data = reader.ReadValue<List<ReverseFlowItemInfo>>();
        }
    }

    public class ForwardFlowItemInfoFormatter : MemoryPackFormatter<ForwardFlowItemInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ForwardFlowItemInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(6);
            writer.WriteValue(value.ReceiveBytes);
            writer.WriteValue(value.SendtBytes);
            writer.WriteValue(value.DiffReceiveBytes);
            writer.WriteValue(value.DiffSendtBytes);
            writer.WriteValue(value.Key);
            writer.WriteValue(value.Target);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ForwardFlowItemInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new ForwardFlowItemInfo();
            reader.TryReadObjectHeader(out byte count);
            value.ReceiveBytes = reader.ReadValue<long>();
            value.SendtBytes = reader.ReadValue<long>();
            value.DiffReceiveBytes = reader.ReadValue<long>();
            value.DiffSendtBytes = reader.ReadValue<long>();
            value.Key = reader.ReadValue<string>();
            value.Target = reader.ReadValue<IPEndPoint>();

        }
    }

    public class ForwardFlowRequestInfoFormatter : MemoryPackFormatter<ForwardFlowRequestInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ForwardFlowRequestInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(5);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Page);
            writer.WriteValue(value.PageSize);
            writer.WriteValue(value.Order);
            writer.WriteValue(value.OrderType);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ForwardFlowRequestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new ForwardFlowRequestInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Page = reader.ReadValue<int>();
            value.PageSize = reader.ReadValue<int>();
            value.Order = reader.ReadValue<ForwardFlowOrder>();
            value.OrderType = reader.ReadValue<ForwardFlowOrderType>();
        }
    }

    public class ForwardFlowResponseInfoFormatter : MemoryPackFormatter<ForwardFlowResponseInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ForwardFlowResponseInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(4);
            writer.WriteValue(value.Page);
            writer.WriteValue(value.PageSize);
            writer.WriteValue(value.Count);
            writer.WriteValue(value.Data);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ForwardFlowResponseInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new ForwardFlowResponseInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Page = reader.ReadValue<int>();
            value.PageSize = reader.ReadValue<int>();
            value.Count = reader.ReadValue<int>();
            value.Data = reader.ReadValue<List<ForwardFlowItemInfo>>();
        }
    }

    public class Socks5FlowItemInfoFormatter : MemoryPackFormatter<Socks5FlowItemInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Socks5FlowItemInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(6);
            writer.WriteValue(value.ReceiveBytes);
            writer.WriteValue(value.SendtBytes);
            writer.WriteValue(value.DiffReceiveBytes);
            writer.WriteValue(value.DiffSendtBytes);
            writer.WriteValue(value.Key);
            writer.WriteValue(value.Target);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref Socks5FlowItemInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new Socks5FlowItemInfo();
            reader.TryReadObjectHeader(out byte count);
            value.ReceiveBytes = reader.ReadValue<long>();
            value.SendtBytes = reader.ReadValue<long>();
            value.DiffReceiveBytes = reader.ReadValue<long>();
            value.DiffSendtBytes = reader.ReadValue<long>();
            value.Key = reader.ReadValue<string>();
            value.Target = reader.ReadValue<IPEndPoint>();

        }
    }

    public class Socks5FlowRequestInfoFormatter : MemoryPackFormatter<Socks5FlowRequestInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Socks5FlowRequestInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(5);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Page);
            writer.WriteValue(value.PageSize);
            writer.WriteValue(value.Order);
            writer.WriteValue(value.OrderType);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref Socks5FlowRequestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new Socks5FlowRequestInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Page = reader.ReadValue<int>();
            value.PageSize = reader.ReadValue<int>();
            value.Order = reader.ReadValue<Socks5FlowOrder>();
            value.OrderType = reader.ReadValue<Socks5FlowOrderType>();
        }
    }

    public class Socks5FlowResponseInfoFormatter : MemoryPackFormatter<Socks5FlowResponseInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Socks5FlowResponseInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(4);
            writer.WriteValue(value.Page);
            writer.WriteValue(value.PageSize);
            writer.WriteValue(value.Count);
            writer.WriteValue(value.Data);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref Socks5FlowResponseInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new Socks5FlowResponseInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Page = reader.ReadValue<int>();
            value.PageSize = reader.ReadValue<int>();
            value.Count = reader.ReadValue<int>();
            value.Data = reader.ReadValue<List<Socks5FlowItemInfo>>();
        }
    }

    public class TunnelFlowItemInfoFormatter : MemoryPackFormatter<TunnelFlowItemInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelFlowItemInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(7);
            writer.WriteValue(value.ReceiveBytes);
            writer.WriteValue(value.SendtBytes);
            writer.WriteValue(value.Key);
            writer.WriteValue(value.TransitionId);
            writer.WriteValue(value.Direction);
            writer.WriteValue(value.Type);
            writer.WriteValue(value.Mode);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelFlowItemInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new TunnelFlowItemInfo();
            reader.TryReadObjectHeader(out byte count);
            value.ReceiveBytes = reader.ReadValue<long>();
            value.SendtBytes = reader.ReadValue<long>();
            value.Key = reader.ReadValue<string>();
            value.TransitionId = reader.ReadValue<string>();
            value.Direction = reader.ReadValue<TunnelDirection>();
            value.Type = reader.ReadValue<TunnelType>();
            value.Mode = reader.ReadValue<TunnelMode>();
        }
    }

    public class TunnelFlowRequestInfoFormatter : MemoryPackFormatter<TunnelFlowRequestInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelFlowRequestInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(5);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Page);
            writer.WriteValue(value.PageSize);
            writer.WriteValue(value.Order);
            writer.WriteValue(value.OrderType);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelFlowRequestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new TunnelFlowRequestInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Page = reader.ReadValue<int>();
            value.PageSize = reader.ReadValue<int>();
            value.Order = reader.ReadValue<TunnelFlowOrder>();
            value.OrderType = reader.ReadValue<TunnelFlowOrderType>();
        }
    }

    public class TunnelFlowResponseInfoFormatter : MemoryPackFormatter<TunnelFlowResponseInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelFlowResponseInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(4);
            writer.WriteValue(value.Page);
            writer.WriteValue(value.PageSize);
            writer.WriteValue(value.Count);
            writer.WriteValue(value.Data);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelFlowResponseInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new TunnelFlowResponseInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Page = reader.ReadValue<int>();
            value.PageSize = reader.ReadValue<int>();
            value.Count = reader.ReadValue<int>();
            value.Data = reader.ReadValue<List<TunnelFlowItemInfo>>();
        }
    }
}
