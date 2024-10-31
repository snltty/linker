using linker.config;
using linker.libs;
using linker.libs.extends;
using linker.plugins.relay.server.caching;
using linker.plugins.resolver;
using MemoryPack;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace linker.plugins.relay.server
{
    public class RelayServerTransfer
    {
        private uint connectionNum = 0;
        private ulong relayFlowingId = 0;


        private readonly IRelayCaching relayCaching;
        private readonly FileConfig fileConfig;

        private readonly ICrypto cryptoMaster;
        private readonly ICrypto cryptoNode;
        private readonly ConcurrentDictionary<string, RelayNodeReportInfo> reports = new ConcurrentDictionary<string, RelayNodeReportInfo>();

        public RelayServerTransfer(IRelayCaching relayCaching, FileConfig fileConfig)
        {
            this.relayCaching = relayCaching;
            this.fileConfig = fileConfig;

            if (fileConfig.Data.Server.Relay.Distributed.Type == "master" && string.IsNullOrWhiteSpace(fileConfig.Data.Server.Relay.Distributed.Master.SecretKey) == false)
            {
                cryptoMaster = CryptoFactory.CreateSymmetric(fileConfig.Data.Server.Relay.Distributed.Master.SecretKey);
            }
            if (fileConfig.Data.Server.Relay.Distributed.Type == "node" && string.IsNullOrWhiteSpace(fileConfig.Data.Server.Relay.Distributed.Node.MasterSecretKey) == false)
            {
                cryptoNode = CryptoFactory.CreateSymmetric(fileConfig.Data.Server.Relay.Distributed.Node.MasterSecretKey);
            }
            if (cryptoNode != null)
            {
                ReportTask();
            }
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
            return fileConfig.Data.Server.Relay.Distributed.Type == "master"
                || (fileConfig.Data.Server.Relay.Distributed.Type == "node" && fileConfig.Data.Server.Relay.Distributed.Node.MaxConnection * 2 > connectionNum);
        }

        public async Task<ulong> AddRelay(string fromid, string fromName, string toid, string toName)
        {
            ulong flowingId = Interlocked.Increment(ref relayFlowingId);

            RelayCache cache = new RelayCache
            {
                FlowId = flowingId,
                FromId = fromid,
                FromName = fromName,
                ToId = toid,
                ToName = toName
            };
            bool added = await relayCaching.TryAdd($"{fromid}->{toid}", cache, 5000);
            if (added == false) return 0;

            return flowingId;
        }
        public async ValueTask<bool> TryGetRelay(string key, RelayCachingValue<RelayCache> wrap)
        {
            return await relayCaching.TryGetValue(key, wrap);
        }


        public virtual void AddReceive(ulong bytes)
        {
        }

        public virtual void AddSendt(ulong bytes)
        {
        }


        public void SetNodeReport(IPEndPoint ep, Memory<byte> data)
        {
            AddReceive((ulong)data.Length);

            if (cryptoMaster == null) return;

            data = cryptoMaster.Decode(data.ToArray());
            RelayNodeReportInfo relayNodeReportInfo = MemoryPackSerializer.Deserialize<RelayNodeReportInfo>(data.Span);

            if (relayNodeReportInfo.EndPoint.Address.Equals(IPAddress.Any))
            {
                relayNodeReportInfo.EndPoint.Address = ep.Address;
            }
            reports.AddOrUpdate(relayNodeReportInfo.Id, relayNodeReportInfo, (a, b) => relayNodeReportInfo);
        }

        public List<RelayNodeReportInfo> GetNodes(bool validated)
        {
            List<RelayNodeReportInfo> result = reports.Values.Where(c => c.Public || (c.Public == false && validated)).ToList();
            if (validated)
            {
                result.Add(new RelayNodeReportInfo { Id = string.Empty, Name = "server", BandwidthRatio = 1, ConnectionRatio = 1, Public = false });
            }
            return result.OrderBy(c => c.ConnectionRatio).ToList();
        }
        public bool NodeValidate(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId)) return true;

            return reports.TryGetValue(nodeId, out RelayNodeReportInfo relayNodeReportInfo) && relayNodeReportInfo.Public == false;
        }

        private void ReportTask()
        {
            TimerHelper.SetInterval(async () =>
            {
                try
                {
                    IPEndPoint endPoint = await NetworkHelper.GetEndPointAsync(fileConfig.Data.Server.Relay.Distributed.Node.Host, fileConfig.Data.Server.ServicePort) ?? new IPEndPoint(IPAddress.Any, fileConfig.Data.Server.ServicePort);

                    double connectionRatio = fileConfig.Data.Server.Relay.Distributed.Node.MaxConnection == 0 ? 0 : connectionNum / fileConfig.Data.Server.Relay.Distributed.Node.MaxConnection;
                    RelayNodeReportInfo relayNodeReportInfo = new RelayNodeReportInfo
                    {
                        Id = fileConfig.Data.Server.Relay.Distributed.Node.Id,
                        Name = fileConfig.Data.Server.Relay.Distributed.Node.Name,
                        Public = fileConfig.Data.Server.Relay.Distributed.Node.Public,
                        BandwidthRatio = 0,
                        ConnectionRatio = Math.Round(connectionRatio, 2),
                        EndPoint = endPoint,
                    };


                    IPEndPoint ep = await NetworkHelper.GetEndPointAsync(fileConfig.Data.Server.Relay.Distributed.Node.MasterHost, 1802);

                    byte[] content = cryptoMaster.Encode(MemoryPackSerializer.Serialize(relayNodeReportInfo));
                    byte[] data = new byte[content.Length + 1];
                    data[0] = (byte)ResolverType.RelayReport;

                    content.AsMemory().CopyTo(data.AsMemory(1));

                    using UdpClient udpClient = new UdpClient(AddressFamily.InterNetwork);
                    udpClient.Client.WindowsUdpBug();

                    AddSendt((ulong)data.Length);
                    await udpClient.SendAsync(data, ep);
                }
                catch (Exception)
                {
                }

                return true;
            }, () => 5000);
        }
    }


}
