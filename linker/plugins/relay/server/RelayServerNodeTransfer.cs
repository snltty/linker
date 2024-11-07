using linker.config;
using linker.libs;
using linker.libs.extends;
using linker.plugins.relay.server.caching;
using linker.plugins.resolver;
using MemoryPack;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;

namespace linker.plugins.relay.server
{
    public class RelayServerNodeTransfer
    {
        private uint connectionNum = 0;


        private readonly IRelayCaching relayCaching;
        private readonly FileConfig fileConfig;

        private readonly ICrypto cryptoNode;
        private readonly ICrypto cryptoMaster;

        public RelayServerNodeTransfer(IRelayCaching relayCaching, FileConfig fileConfig)
        {
            this.relayCaching = relayCaching;
            this.fileConfig = fileConfig;

            cryptoNode = CryptoFactory.CreateSymmetric(fileConfig.Data.Server.Relay.Distributed.Node.MasterSecretKey);
            cryptoMaster = CryptoFactory.CreateSymmetric(fileConfig.Data.Server.Relay.Distributed.Master.SecretKey);
            ReportTask();
        }

        public void IncrementConnectionNum()
        {
            Interlocked.Increment(ref connectionNum);
        }
        public void DecrementConnectionNum()
        {
            Interlocked.Decrement(ref connectionNum);
        }
        public bool ValidateConnection()
        {
            return fileConfig.Data.Server.Relay.Distributed.Node.MaxConnection == 0 || fileConfig.Data.Server.Relay.Distributed.Node.MaxConnection * 2 > connectionNum;
        }

        public async ValueTask<RelayCache> TryGetRelayCache(string key, string nodeid)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(2 * 1024);
            try
            {
                IPEndPoint server = nodeid == RelayNodeInfo.MASTER_NODE_ID
                    ? new IPEndPoint(IPAddress.Loopback, fileConfig.Data.Server.ServicePort)
                    : await NetworkHelper.GetEndPointAsync(fileConfig.Data.Server.Relay.Distributed.Node.MasterHost, 1802);
                ICrypto crypto = nodeid == RelayNodeInfo.MASTER_NODE_ID ? cryptoMaster : cryptoNode;

                Socket socket = new Socket(server.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                long start = Environment.TickCount64;
                await socket.ConnectAsync(server).ConfigureAwait(false);
                long time = Environment.TickCount64 - start;

                await socket.SendAsync(new byte[] { (byte)ResolverType.RelayReport });
                await socket.SendAsync(key.ToBytes());
                int length = await socket.ReceiveAsync(buffer.AsMemory(), SocketFlags.None).AsTask().WaitAsync(TimeSpan.FromMilliseconds(Math.Max(time * 2, 500))).ConfigureAwait(false);
                socket.SafeClose();

                RelayCache result = MemoryPackSerializer.Deserialize<RelayCache>(crypto.Decode(buffer.AsMemory(0, length).ToArray()).Span);
                return result;
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error($"{ex}");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            return null;
        }

        public uint GetBandwidthLimit()
        {
            return (uint)Math.Ceiling((fileConfig.Data.Server.Relay.Distributed.Node.MaxBandwidth * 1024 * 1024) / 8.0);
        }
        private void ReportTask()
        {
            TimerHelper.SetInterval(async () =>
            {
                IEnumerable<RelayNodeInfo> nodes = new List<RelayNodeInfo>
                {
                    new RelayNodeInfo{
                        Id = RelayNodeInfo.MASTER_NODE_ID,
                        Host = new IPEndPoint(IPAddress.Any,fileConfig.Data.Server.ServicePort).ToString(),
                        MasterHost =  new IPEndPoint(IPAddress.Loopback,fileConfig.Data.Server.ServicePort).ToString(),
                        MasterSecretKey = fileConfig.Data.Server.Relay.Distributed.Master.SecretKey,
                        MaxBandwidth = 0,
                        MaxConnection = 0,
                        Name = "default",
                        Public = false
                    },
                    fileConfig.Data.Server.Relay.Distributed.Node
                }.Where(c => string.IsNullOrWhiteSpace(c.MasterHost) == false && string.IsNullOrWhiteSpace(c.MasterSecretKey) == false)
                .Where(c => string.IsNullOrWhiteSpace(c.Name) == false && string.IsNullOrWhiteSpace(c.Id) == false);



                foreach (var node in nodes)
                {
                    try
                    {
                        ICrypto crypto = node.Id == RelayNodeInfo.MASTER_NODE_ID ? cryptoMaster : cryptoNode;

                        IPEndPoint endPoint = await NetworkHelper.GetEndPointAsync(node.Host, fileConfig.Data.Server.ServicePort) ?? new IPEndPoint(IPAddress.Any, fileConfig.Data.Server.ServicePort);
                        int maxConnection = node.MaxConnection == 0 ? 65535 : node.MaxConnection;
                        double connectionRatio = connectionNum / 2.0 / maxConnection;
                        double maxBandwidth = node.MaxBandwidth == 0 ? 65535 : node.MaxBandwidth;

                        RelayNodeReportInfo relayNodeReportInfo = new RelayNodeReportInfo
                        {
                            Id = node.Id,
                            Name = node.Name,
                            Public = node.Public,
                            MaxBandwidth = maxBandwidth,
                            BandwidthRatio = 0,

                            MaxConnection = maxConnection,
                            ConnectionRatio = Math.Round(connectionRatio, 2),
                            EndPoint = endPoint,
                        };

                        IPEndPoint ep = await NetworkHelper.GetEndPointAsync(node.MasterHost, fileConfig.Data.Server.ServicePort);

                        byte[] content = crypto.Encode(MemoryPackSerializer.Serialize(relayNodeReportInfo));
                        byte[] data = new byte[content.Length + 1];
                        data[0] = (byte)ResolverType.RelayReport;
                        content.AsMemory().CopyTo(data.AsMemory(1));

                        using UdpClient udpClient = new UdpClient(AddressFamily.InterNetwork);
                        udpClient.Client.WindowsUdpBug();
                        await udpClient.SendAsync(data, ep);
                    }
                    catch (Exception)
                    {
                    }
                }


                return true;
            }, () => 5000);
        }
    }


}
