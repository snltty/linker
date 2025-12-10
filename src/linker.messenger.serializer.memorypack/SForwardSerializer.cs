using linker.messenger.relay.server;
using linker.messenger.sforward;
using linker.messenger.sforward.server;
using linker.tunnel.connection;
using MemoryPack;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.serializer.memorypack
{
    [MemoryPackable]
    public readonly partial struct SerializableSForwardAddResultInfo
    {
        [MemoryPackIgnore]
        public readonly SForwardAddResultInfo info;

        [MemoryPackInclude]
        bool Success => info.Success;

        [MemoryPackInclude]
        string Message => info.Message;

        [MemoryPackInclude]
        byte BufferSize => info.BufferSize;

        [MemoryPackConstructor]
        SerializableSForwardAddResultInfo(bool success, string message, byte bufferSize)
        {
            this.info = new SForwardAddResultInfo
            {
                BufferSize = bufferSize,
                Message = message,
                Success = success
            };
        }

        public SerializableSForwardAddResultInfo(SForwardAddResultInfo info)
        {
            this.info = info;
        }
    }
    public class SForwardAddResultInfoFormatter : MemoryPackFormatter<SForwardAddResultInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SForwardAddResultInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSForwardAddResultInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SForwardAddResultInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new SForwardAddResultInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Success = reader.ReadValue<bool>();
            value.Message = reader.ReadValue<string>();
            value.BufferSize = reader.ReadValue<byte>();
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableSForwardAddForwardInfo
    {
        [MemoryPackIgnore]
        public readonly SForwardAddForwardInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        SForwardInfo Data => info.Data;

        [MemoryPackConstructor]
        SerializableSForwardAddForwardInfo(string machineId, SForwardInfo data)
        {
            this.info = new SForwardAddForwardInfo
            {
                MachineId = machineId,
                Data = data
            };
        }

        public SerializableSForwardAddForwardInfo(SForwardAddForwardInfo info)
        {
            this.info = info;
        }
    }
    public class SForwardAddForwardInfoFormatter : MemoryPackFormatter<SForwardAddForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SForwardAddForwardInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSForwardAddForwardInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SForwardAddForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new SForwardAddForwardInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Data = reader.ReadValue<SForwardInfo>();
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableSForwardRemoveForwardInfo
    {
        [MemoryPackIgnore]
        public readonly SForwardRemoveForwardInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        int Id => info.Id;

        [MemoryPackConstructor]
        SerializableSForwardRemoveForwardInfo(string machineId, int id)
        {
            this.info = new SForwardRemoveForwardInfo
            {
                MachineId = machineId,
                Id = id
            };
        }

        public SerializableSForwardRemoveForwardInfo(SForwardRemoveForwardInfo info)
        {
            this.info = info;
        }
    }
    public class SForwardRemoveForwardInfoFormatter : MemoryPackFormatter<SForwardRemoveForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SForwardRemoveForwardInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSForwardRemoveForwardInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SForwardRemoveForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new SForwardRemoveForwardInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Id = reader.ReadValue<int>();

        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableSForwardProxyInfo
    {
        [MemoryPackIgnore]
        public readonly SForwardProxyInfo info;

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
        SerializableSForwardProxyInfo(ulong id, string domain, int remotePort, byte bufferSize, string machineId, string nodeid, ProtocolType protocolType, IPAddress addr)
        {
            this.info = new SForwardProxyInfo
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
        public SerializableSForwardProxyInfo(SForwardProxyInfo info)
        {
            this.info = info;
        }
    }
    public class SForwardProxyInfoFormatter : MemoryPackFormatter<SForwardProxyInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SForwardProxyInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSForwardProxyInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SForwardProxyInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new SForwardProxyInfo();
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
    public readonly partial struct SerializableSForwardAddInfo
    {
        [MemoryPackIgnore]
        public readonly SForwardAddInfo info;

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
        SerializableSForwardAddInfo(string domain, int remotePort, string nodeid, string machineid, string groupid, bool super, double bandwidth)
        {
            this.info = new SForwardAddInfo
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

        public SerializableSForwardAddInfo(SForwardAddInfo info)
        {
            this.info = info;
        }
    }
    public class SForwardAddInfoFormatter : MemoryPackFormatter<SForwardAddInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SForwardAddInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSForwardAddInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SForwardAddInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new SForwardAddInfo();
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
    public readonly partial struct SerializableSForwardInfo
    {
        [MemoryPackIgnore]
        public readonly SForwardInfo info;

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
        SerializableSForwardInfo(long id, string name, string domain, int remotePort, byte bufferSize, IPEndPoint localEP,
            bool started, string msg, string localMsg, int remotePortMin, int remotePortMax, string nodeid)
        {
            this.info = new SForwardInfo
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

        public SerializableSForwardInfo(SForwardInfo info)
        {
            this.info = info;
        }
    }
    public class SForwardInfoFormatter : MemoryPackFormatter<SForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SForwardInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSForwardInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new SForwardInfo();
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
    public readonly partial struct SerializableSForwardServerNodeReportInfo
    {
        [MemoryPackIgnore]
        public readonly SForwardServerNodeReportInfo info;

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
        SerializableSForwardServerNodeReportInfo(string nodeId, string name, string host, string domain, int webport, string tunnelports, int connections, int bandwidth, int dataEachMonth,
            long dataRemain, string url, string logo, string masterKey, string version, int connectionsRatio, double bandwidthRatio,int masterCount)
        {
            var info = new SForwardServerNodeReportInfo
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

        public SerializableSForwardServerNodeReportInfo(SForwardServerNodeReportInfo info)
        {
            this.info = info;
        }
    }
    public class SForwardServerNodeReportInfoFormatter : MemoryPackFormatter<SForwardServerNodeReportInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SForwardServerNodeReportInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSForwardServerNodeReportInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SForwardServerNodeReportInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new SForwardServerNodeReportInfo();
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
    public readonly partial struct SerializableSForwardServerNodeStoreInfo
    {
        [MemoryPackIgnore]
        public readonly SForwardServerNodeStoreInfo info;

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
        SerializableSForwardServerNodeStoreInfo(string nodeId, string name, string host, string domain, int webport, string tunnelports, int connections, int bandwidth, int dataEachMonth,
            long dataRemain, string url, string logo, string masterKey, string version, int connectionsRatio, double bandwidthRatio,int masterCount,
           int id, int bandwidthEachConnection, bool Public, long lastTicks, bool manageable)
        {
            var info = new SForwardServerNodeStoreInfo
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

        public SerializableSForwardServerNodeStoreInfo(SForwardServerNodeStoreInfo info)
        {
            this.info = info;
        }
    }
    public class SForwardServerNodeStoreInfoFormatter : MemoryPackFormatter<SForwardServerNodeStoreInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SForwardServerNodeStoreInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSForwardServerNodeStoreInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SForwardServerNodeStoreInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new SForwardServerNodeStoreInfo();
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
    public readonly partial struct SerializableSForwardServerNodeReportInfoOld
    {
        [MemoryPackIgnore]
        public readonly SForwardServerNodeReportInfoOld info;

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
        SerializableSForwardServerNodeReportInfoOld(
            string id, string name, double maxBandwidth, double maxBandwidthTotal,
            double maxGbTotal, long maxGbTotalLastBytes, double bandwidthRatio,
            bool Public, int delay,
            string domain, IPAddress address, long lastTicks, string url, bool sync2Server, string version, int webport, int[] portrange)
        {
            var info = new SForwardServerNodeReportInfoOld
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

        public SerializableSForwardServerNodeReportInfoOld(SForwardServerNodeReportInfoOld info)
        {
            this.info = info;
        }
    }
    public class SForwardServerNodeReportInfoOldFormatter : MemoryPackFormatter<SForwardServerNodeReportInfoOld>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SForwardServerNodeReportInfoOld value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSForwardServerNodeReportInfoOld(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SForwardServerNodeReportInfoOld value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSForwardServerNodeReportInfoOld>();
            value = wrapped.info;
        }
    }


}
