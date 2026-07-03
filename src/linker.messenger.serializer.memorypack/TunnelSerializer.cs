using linker.messenger.tunnel;
using linker.tunnel.connection;
using linker.tunnel.transport;
using linker.tunnel.wanport;
using linker.upnp;
using MemoryPack;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.serializer.memorypack
{
    public class TunnelWanPortProtocolInfoFormatter : MemoryPackFormatter<TunnelWanPortProtocolInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelWanPortProtocolInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.ProtocolType);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelWanPortProtocolInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new TunnelWanPortProtocolInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.ProtocolType = reader.ReadValue<TunnelWanPortProtocolType>();
        }
    }

    public class TunnelTransportWanPortInfoFormatter : MemoryPackFormatter<TunnelTransportWanPortInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelTransportWanPortInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(9);
            writer.WriteValue(value.Local);
            writer.WriteValue(value.Remote);
            writer.WriteValue(value.LocalIps);
            writer.WriteValue(value.RouteLevel);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.MachineName);
            writer.WriteValue(value.PortMapWan);
            writer.WriteValue(value.PortMapLan);
            writer.WriteValue(value.PredictPorts);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelTransportWanPortInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new TunnelTransportWanPortInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Local = reader.ReadValue<IPEndPoint>();
            value.Remote = reader.ReadValue<IPEndPoint>();
            value.LocalIps = reader.ReadValue<IPAddress[]>();
            value.RouteLevel = reader.ReadValue<int>();
            value.MachineId = reader.ReadValue<string>();
            value.MachineName = reader.ReadValue<string>();
            value.PortMapWan = reader.ReadValue<int>();
            value.PortMapLan = reader.ReadValue<int>();

            if (count > 8)
                value.PredictPorts = reader.ReadValue<int[]>();
        }
    }

    public class TunnelTransportItemInfoFormatter : MemoryPackFormatter<TunnelTransportItemInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelTransportItemInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(11);
            writer.WriteValue(value.Name);
            writer.WriteValue(value.Label);
            writer.WriteValue(value.ProtocolType);
            writer.WriteValue(value.Disabled);
            writer.WriteValue(value.Reverse);
            writer.WriteValue(value.SSL);
            writer.WriteValue(value.BufferSize);
            writer.WriteValue(value.Order);
            writer.WriteValue(value.Addr);
            writer.WriteValue(value.TunnelType);
            writer.WriteValue(value.EnableAddr);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelTransportItemInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new TunnelTransportItemInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Name = reader.ReadValue<string>();
            value.Label = reader.ReadValue<string>();
            value.ProtocolType = reader.ReadValue<string>();
            value.Disabled = reader.ReadValue<bool>();
            value.Reverse = reader.ReadValue<bool>();
            value.SSL = reader.ReadValue<bool>();
            value.BufferSize = reader.ReadValue<byte>();
            value.Order = reader.ReadValue<byte>();
            if (count > 8)
                value.Addr = reader.ReadValue<Addrs>();

            if (count > 9)
                value.TunnelType = reader.ReadValue<TunnelType>();

            if (count > 10)
                value.EnableAddr = reader.ReadValue<bool>();
        }
    }

    public class TunnelTransportInfoFormatter : MemoryPackFormatter<TunnelTransportInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelTransportInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(10);
            writer.WriteValue(value.Local);
            writer.WriteValue(value.Remote);
            writer.WriteValue(value.TransactionId);
            writer.WriteValue(value.TransportType);
            writer.WriteValue(value.TransportName);
            writer.WriteValue(value.Direction);
            writer.WriteValue(value.SSL);
            writer.WriteValue(value.BufferSize);
            writer.WriteValue(value.FlowId);
            writer.WriteValue(value.Configure);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelTransportInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new TunnelTransportInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Local = reader.ReadValue<TunnelTransportWanPortInfo>();
            value.Remote = reader.ReadValue<TunnelTransportWanPortInfo>();
            value.TransactionId = reader.ReadValue<string>();
            value.TransportType = reader.ReadValue<TunnelProtocolType>();
            value.TransportName = reader.ReadValue<string>();
            value.Direction = reader.ReadValue<TunnelDirection>();
            value.SSL = reader.ReadValue<bool>();
            value.BufferSize = reader.ReadValue<byte>();
            value.FlowId = reader.ReadValue<uint>();
            if (count > 9)
                value.Configure = reader.ReadValue<Dictionary<string, string>>();
        }
    }


    public class TunnelRouteLevelInfoFormatter : MemoryPackFormatter<TunnelRouteLevelInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelRouteLevelInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(9);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.RouteLevel);
            writer.WriteValue(value.RouteLevelPlus);
            writer.WriteValue(value.NeedReboot);
            writer.WriteValue(value.PortMapWan);
            writer.WriteValue(value.PortMapLan);
            writer.WriteValue(value.Net);
            writer.WriteValue(value.InIp);
            writer.WriteValue(value.Mesh);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelRouteLevelInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new TunnelRouteLevelInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.RouteLevel = reader.ReadValue<int>();
            value.RouteLevelPlus = reader.ReadValue<int>();
            value.NeedReboot = reader.ReadValue<bool>();
            value.PortMapWan = reader.ReadValue<int>();
            value.PortMapLan = reader.ReadValue<int>();
            value.Net = reader.ReadValue<TunnelNetInfo>();

            if (count > 7)
                value.InIp = reader.ReadValue<IPAddress>();

            if (count > 8)
                value.Mesh = reader.ReadValue<TunnelMeshInfo>();
        }
    }

    public class TunnelRelayInfoFormatter : MemoryPackFormatter<TunnelMeshInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelMeshInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(3);
            writer.WriteValue(value.Enabled);
            writer.WriteValue(value.Bandwidth);
            writer.WriteValue(value.MachineName);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelMeshInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new TunnelMeshInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Enabled = reader.ReadValue<bool>();
            value.Bandwidth = reader.ReadValue<int>();
            value.MachineName = reader.ReadValue<string>();
        }
    }

    public class TunnelNetworkInfoFormatter : MemoryPackFormatter<TunnelLocalNetworkInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelLocalNetworkInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(4);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.HostName);
            writer.WriteValue(value.Lans);
            writer.WriteValue(value.Routes);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelLocalNetworkInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new TunnelLocalNetworkInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.HostName = reader.ReadValue<string>();
            value.Lans = reader.ReadValue<TunnelInterfaceInfo[]>();
            value.Routes = reader.ReadValue<IPAddress[]>();
        }
    }

    public class TunnelInterfaceInfoFormatter : MemoryPackFormatter<TunnelInterfaceInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelInterfaceInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(4);
            writer.WriteValue(value.Name);
            writer.WriteValue(value.Desc);
            writer.WriteValue(value.Mac);
            writer.WriteValue(value.Ips);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelInterfaceInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new TunnelInterfaceInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Name = reader.ReadValue<string>();
            value.Desc = reader.ReadValue<string>();
            value.Mac = reader.ReadValue<string>();
            value.Ips = reader.ReadValue<IPAddress[]>();
        }
    }

    public class TunnelNetInfoFormatter : MemoryPackFormatter<TunnelNetInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelNetInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(6);
            writer.WriteValue(value.CountryCode);
            writer.WriteValue(value.City);
            writer.WriteValue(value.Lat);
            writer.WriteValue(value.Lon);
            writer.WriteValue(value.Isp);
            writer.WriteValue(value.Nat);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelNetInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new TunnelNetInfo();
            reader.TryReadObjectHeader(out byte count);
            value.CountryCode = reader.ReadValue<string>();
            value.City = reader.ReadValue<string>();
            value.Lat = reader.ReadValue<double>();
            value.Lon = reader.ReadValue<double>();
            value.Isp = reader.ReadValue<string>();
            value.Nat = reader.ReadValue<string>();
        }
    }

    public class TunnelSetRouteLevelInfoFormatter : MemoryPackFormatter<TunnelSetRouteLevelInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelSetRouteLevelInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(6);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.RouteLevelPlus);
            writer.WriteValue(value.PortMapWan);
            writer.WriteValue(value.PortMapLan);
            writer.WriteValue(value.InIp);
            writer.WriteValue(value.Mesh);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelSetRouteLevelInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new TunnelSetRouteLevelInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.RouteLevelPlus = reader.ReadValue<int>();
            value.PortMapWan = reader.ReadValue<int>();
            value.PortMapLan = reader.ReadValue<int>();

            if (count > 4)
                value.InIp = reader.ReadValue<IPAddress>();

            if (count > 5)
                value.Mesh = reader.ReadValue<TunnelMeshInfo>();
        }
    }
   
    public class TunnelTransportItemSetInfoFormatter : MemoryPackFormatter<TunnelTransportItemSetInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelTransportItemSetInfo value)
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

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelTransportItemSetInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new TunnelTransportItemSetInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Data = reader.ReadValue<List<TunnelTransportItemInfo>>();
        }
    }

    public class PortMappingInfoFormatter : MemoryPackFormatter<PortMappingInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref PortMappingInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(9);
            writer.WriteValue(value.ClientIp);
            writer.WriteValue(value.PublicPort);
            writer.WriteValue(value.PrivatePort);
            writer.WriteValue(value.ProtocolType);
            writer.WriteValue(value.Enabled);
            writer.WriteValue(value.Description);
            writer.WriteValue(value.LeaseDuration);
            writer.WriteValue(value.DeviceType);
            writer.WriteValue(value.Deletable);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref PortMappingInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new PortMappingInfo();
            reader.TryReadObjectHeader(out byte count);
            value.ClientIp = reader.ReadValue<IPAddress>();
            value.PublicPort = reader.ReadValue<int>();
            value.PrivatePort = reader.ReadValue<int>();
            value.ProtocolType = reader.ReadValue<ProtocolType>();
            value.Enabled = reader.ReadValue<bool>();
            value.Description = reader.ReadValue<string>();
            value.LeaseDuration = reader.ReadValue<int>();
            value.DeviceType = reader.ReadValue<DeviceType>();
            value.Deletable = reader.ReadValue<bool>();
        }
    }
}
