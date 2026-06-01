using linker.messenger.reverse;
using linker.messenger.reverse.server;
using MemoryPack;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.serializer.memorypack
{
    [MemoryPackable]
    public readonly partial struct SerializableReverseAddResultInfo
    {
        [MemoryPackIgnore]
        public readonly ReverseAddResultInfo info;

        [MemoryPackInclude]
        bool Success => info.Success;

        [MemoryPackInclude]
        string Message => info.Message;

        [MemoryPackInclude]
        byte BufferSize => info.BufferSize;

        [MemoryPackConstructor]
        SerializableReverseAddResultInfo(bool success, string message, byte bufferSize)
        {
            this.info = new ReverseAddResultInfo
            {
                BufferSize = bufferSize,
                Message = message,
                Success = success
            };
        }

        public SerializableReverseAddResultInfo(ReverseAddResultInfo info)
        {
            this.info = info;
        }
    }
    public class ReverseAddResultInfoFormatter : MemoryPackFormatter<ReverseAddResultInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ReverseAddResultInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableReverseAddResultInfo(value));
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



    [MemoryPackable]
    public readonly partial struct SerializableReverseAddForwardInfo
    {
        [MemoryPackIgnore]
        public readonly ReverseAddForwardInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        ReverseInfo Data => info.Data;

        [MemoryPackConstructor]
        SerializableReverseAddForwardInfo(string machineId, ReverseInfo data)
        {
            this.info = new ReverseAddForwardInfo
            {
                MachineId = machineId,
                Data = data
            };
        }

        public SerializableReverseAddForwardInfo(ReverseAddForwardInfo info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializableReverseAddForwardInfo(value));
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


    [MemoryPackable]
    public readonly partial struct SerializableReverseRemoveForwardInfo
    {
        [MemoryPackIgnore]
        public readonly ReverseRemoveForwardInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        int Id => info.Id;

        [MemoryPackConstructor]
        SerializableReverseRemoveForwardInfo(string machineId, int id)
        {
            this.info = new ReverseRemoveForwardInfo
            {
                MachineId = machineId,
                Id = id
            };
        }

        public SerializableReverseRemoveForwardInfo(ReverseRemoveForwardInfo info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializableReverseRemoveForwardInfo(value));
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

    [MemoryPackable]
    public readonly partial struct SerializableReverseProxyInfo
    {
        [MemoryPackIgnore]
        public readonly ReverseProxyInfo info;

        [MemoryPackInclude]
        ulong Id => info.Id;

        [MemoryPackInclude]
        string Domain => info.Domain;

        [MemoryPackInclude]
        int RemotePort => info.RemotePort;

        [MemoryPackInclude]
        byte BufferSize => info.BufferSize;

        [MemoryPackInclude]
        string MachineId => info.MachineId;
        [MemoryPackInclude]
        string NodeId => info.NodeId;

        [MemoryPackInclude]
        ProtocolType ProtocolType => info.ProtocolType;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPAddress Addr => info.Addr;

        [MemoryPackConstructor]
        SerializableReverseProxyInfo(ulong id, string domain, int remotePort, byte bufferSize, string machineId, string nodeid, ProtocolType protocolType, IPAddress addr)
        {
            this.info = new ReverseProxyInfo
            {
                Id = id,
                BufferSize = bufferSize,
                Domain = domain,
                RemotePort = remotePort,
                NodeId = nodeid,
                ProtocolType = protocolType,
                Addr = addr,
                MachineId = machineId
            };
        }
        public SerializableReverseProxyInfo(ReverseProxyInfo info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializableReverseProxyInfo(value));
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

    [MemoryPackable]
    public readonly partial struct SerializableReverseAddInfo
    {
        [MemoryPackIgnore]
        public readonly ReverseAddInfo info;

        [MemoryPackInclude]
        string Domain => info.Domain;

        [MemoryPackInclude]
        int RemotePort => info.RemotePort;

        [MemoryPackInclude]
        string NodeId => info.NodeId;
        [MemoryPackInclude]
        string MachineId => info.MachineId;
        [MemoryPackInclude]
        string GroupId => info.GroupId;

        [MemoryPackInclude]
        bool Super => info.Super;


        [MemoryPackInclude]
        double Bandwidth => info.Bandwidth;

        [MemoryPackConstructor]
        SerializableReverseAddInfo(string domain, int remotePort, string nodeid, string machineid, string groupid, bool super, double bandwidth)
        {
            this.info = new ReverseAddInfo
            {
                RemotePort = remotePort,
                Domain = domain,
                NodeId = nodeid,
                GroupId = groupid,
                MachineId = machineid,
                Super = super,
                Bandwidth = bandwidth
            };
        }

        public SerializableReverseAddInfo(ReverseAddInfo info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializableReverseAddInfo(value));
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
    [MemoryPackable]
    public readonly partial struct SerializableReverseInfo
    {
        [MemoryPackIgnore]
        public readonly ReverseInfo info;

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
        [MemoryPackInclude]
        string NodeId => info.NodeId;

        [MemoryPackConstructor]
        SerializableReverseInfo(long id, string name, string domain, int remotePort, byte bufferSize, IPEndPoint localEP,
            bool started, string msg, string localMsg, int remotePortMin, int remotePortMax, string nodeid)
        {
            this.info = new ReverseInfo
            {
                LocalEP = localEP,
                RemotePort = remotePort,
                RemotePortMax = remotePortMax,
                Name = name,
                BufferSize = bufferSize,
                Domain = domain,
                Id = id,
                LocalMsg = localMsg,
                Msg = msg,
                RemotePortMin = remotePortMin,
                Started = started,
                NodeId = nodeid
            };
        }

        public SerializableReverseInfo(ReverseInfo info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializableReverseInfo(value));
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




    [MemoryPackable]
    public readonly partial struct SerializableReverseServerNodeReportInfo
    {
        [MemoryPackIgnore]
        public readonly ReverseServerNodeReportInfo info;

        [MemoryPackInclude]
        string NodeId => info.NodeId;
        [MemoryPackInclude]
        string Name => info.Name;
        [MemoryPackInclude]
        string Host => info.Host;
        [MemoryPackInclude]
        string Domain => info.Domain;
        [MemoryPackInclude]
        int WebPort => info.WebPort;
        [MemoryPackInclude]
        string TunnelPorts => info.TunnelPorts;
        [MemoryPackInclude]
        int Connections => info.Connections;
        [MemoryPackInclude]
        int Bandwidth => info.Bandwidth;
        [MemoryPackInclude]
        int DataEachMonth => info.DataEachMonth;
        [MemoryPackInclude]
        long DataRemain => info.DataRemain;
        [MemoryPackInclude]
        string Url => info.Url;
        [MemoryPackInclude]
        string Logo => info.Logo;
        [MemoryPackInclude]
        string MasterKey => info.MasterKey;
        [MemoryPackInclude]
        string Version => info.Version;
        [MemoryPackInclude]
        int ConnectionsRatio => info.ConnectionsRatio;
        [MemoryPackInclude]
        double BandwidthRatio => info.BandwidthRatio;
        [MemoryPackInclude]
        int MasterCount => info.MasterCount;


        [MemoryPackConstructor]
        SerializableReverseServerNodeReportInfo(string nodeId, string name, string host, string domain, int webport, string tunnelports, int connections, int bandwidth, int dataEachMonth,
            long dataRemain, string url, string logo, string masterKey, string version, int connectionsRatio, double bandwidthRatio,int masterCount)
        {
            var info = new ReverseServerNodeReportInfo
            {
                NodeId = nodeId,
                Name = name,
                Host = host,
                Domain = domain,
                WebPort = webport,
                TunnelPorts = tunnelports,
                Connections = connections,
                Bandwidth = bandwidth,
                DataEachMonth = dataEachMonth,
                DataRemain = dataRemain,
                Url = url,
                Logo = logo,
                MasterKey = masterKey,
                Version = version,
                ConnectionsRatio = connectionsRatio,
                BandwidthRatio = bandwidthRatio,
                MasterCount = masterCount,

            };
            this.info = info;
        }

        public SerializableReverseServerNodeReportInfo(ReverseServerNodeReportInfo info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializableReverseServerNodeReportInfo(value));
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


    [MemoryPackable]
    public readonly partial struct SerializableReverseServerNodeStoreInfo
    {
        [MemoryPackIgnore]
        public readonly ReverseServerNodeStoreInfo info;

        [MemoryPackInclude]
        string NodeId => info.NodeId;
        [MemoryPackInclude]
        string Name => info.Name;
        [MemoryPackInclude]
        string Host => info.Host;
        [MemoryPackInclude]
        string Domain => info.Domain;
        [MemoryPackInclude]
        int WebPort => info.WebPort;
        [MemoryPackInclude]
        string TunnelPorts => info.TunnelPorts;
        [MemoryPackInclude]
        int Connections => info.Connections;
        [MemoryPackInclude]
        int Bandwidth => info.Bandwidth;
        [MemoryPackInclude]
        int DataEachMonth => info.DataEachMonth;
        [MemoryPackInclude]
        long DataRemain => info.DataRemain;
        [MemoryPackInclude]
        string Url => info.Url;
        [MemoryPackInclude]
        string Logo => info.Logo;
        [MemoryPackInclude]
        string MasterKey => info.MasterKey;
        [MemoryPackInclude]
        string Version => info.Version;

        [MemoryPackInclude]
        int ConnectionsRatio => info.ConnectionsRatio;
        [MemoryPackInclude]
        double BandwidthRatio => info.BandwidthRatio;
        [MemoryPackInclude]
       int MasterCount => info.MasterCount;


        [MemoryPackInclude]
        int Id => info.Id;
        [MemoryPackInclude]
        int BandwidthEachConnection => info.BandwidthEach;
        [MemoryPackInclude]
        bool Public => info.Public;
        [MemoryPackInclude]
        long LastTicks => info.LastTicks;

        [MemoryPackInclude]
        bool Manageable => info.Manageable;

        [MemoryPackConstructor]
        SerializableReverseServerNodeStoreInfo(string nodeId, string name, string host, string domain, int webport, string tunnelports, int connections, int bandwidth, int dataEachMonth,
            long dataRemain, string url, string logo, string masterKey, string version, int connectionsRatio, double bandwidthRatio,int masterCount,
           int id, int bandwidthEachConnection, bool Public, long lastTicks, bool manageable)
        {
            var info = new ReverseServerNodeStoreInfo
            {
                NodeId = nodeId,
                Name = name,
                Host = host,
                Domain = domain,
                WebPort = webport,
                TunnelPorts = tunnelports,
                Connections = connections,
                Bandwidth = bandwidth,
                DataEachMonth = dataEachMonth,
                DataRemain = dataRemain,
                Url = url,
                Logo = logo,
                MasterKey = masterKey,
                Version = version,
                ConnectionsRatio = connectionsRatio,
                BandwidthRatio = bandwidthRatio,
                MasterCount = masterCount,
                Id = id,

                BandwidthEach = bandwidthEachConnection,
                Public = Public,
                LastTicks = lastTicks,
                Manageable = manageable
            };
            this.info = info;
        }

        public SerializableReverseServerNodeStoreInfo(ReverseServerNodeStoreInfo info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializableReverseServerNodeStoreInfo(value));
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



    [MemoryPackable]
    public readonly partial struct SerializableReverseServerNodeReportInfoOld
    {
        [MemoryPackIgnore]
        public readonly ReverseServerNodeReportInfoOld info;

        [MemoryPackInclude]
        string Id => info.Id;
        [MemoryPackInclude]
        double MaxBandwidth => info.MaxBandwidth;
        [MemoryPackInclude]
        double MaxBandwidthTotal => info.MaxBandwidthTotal;
        [MemoryPackInclude]
        double MaxGbTotal => info.MaxGbTotal;
        [MemoryPackInclude]
        long MaxGbTotalLastBytes => info.MaxGbTotalLastBytes;
        [MemoryPackInclude]
        double BandwidthRatio => info.BandwidthRatio;
        [MemoryPackInclude]
        string Name => info.Name;
        [MemoryPackInclude]
        bool Public => info.Public;
        [MemoryPackInclude]
        int Delay => info.Delay;
        [MemoryPackInclude]
        string Domain => info.Domain;
        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPAddress Address => info.Address;
        [MemoryPackInclude]
        long LastTicks => info.LastTicks;
        [MemoryPackInclude]
        string Url => info.Url;

        [MemoryPackInclude]
        bool Sync2Server => info.Sync2Server;
        [MemoryPackInclude]
        string Version => info.Version;

        [MemoryPackInclude]
        int WebPort => info.WebPort;
        [MemoryPackInclude]
        int[] PortRange => info.PortRange;


        [MemoryPackConstructor]
        SerializableReverseServerNodeReportInfoOld(
            string id, string name, double maxBandwidth, double maxBandwidthTotal,
            double maxGbTotal, long maxGbTotalLastBytes, double bandwidthRatio,
            bool Public, int delay,
            string domain, IPAddress address, long lastTicks, string url, bool sync2Server, string version, int webport, int[] portrange)
        {
            var info = new ReverseServerNodeReportInfoOld
            {
                BandwidthRatio = bandwidthRatio,
                MaxBandwidth = maxBandwidth,
                MaxBandwidthTotal = maxBandwidthTotal,
                MaxGbTotal = maxGbTotal,
                MaxGbTotalLastBytes = maxGbTotalLastBytes,
                Delay = delay,
                Domain = domain,
                Address = address,
                Id = id,
                LastTicks = lastTicks,
                Name = name,
                Public = Public,
                Url = url,
                Sync2Server = sync2Server,
                Version = version,
                WebPort = webport,
                PortRange = portrange
            };
            this.info = info;
        }

        public SerializableReverseServerNodeReportInfoOld(ReverseServerNodeReportInfoOld info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializableReverseServerNodeReportInfoOld(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ReverseServerNodeReportInfoOld value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableReverseServerNodeReportInfoOld>();
            value = wrapped.info;
        }
    }


}
