using linker.tunnel;
using linker.tunnel.transport;
using System.Net;

namespace linker.messenger.tunnel.client
{
    /// <summary>
    /// 打洞信标适配存储
    /// </summary>
    public interface ITunnelClientStore
    {
        public int TransportMachineIdCount { get; }
        /// <summary>
        /// 配置的额外网络层级
        /// </summary>
        public int RouteLevelPlus { get; }
        /// <summary>
        /// 设置额外的网关层级
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public Task<bool> SetRouteLevelPlus(int level);

        /// <summary>
        /// 端口映射内外端口
        /// </summary>
        public int PortMapPrivate { get; }
        /// <summary>
        /// 端口映射外网端口
        /// </summary>
        public int PortMapPublic { get; }


        public IPAddress InIp { get; }
        public TunnelMeshInfo Relay { get; }

        /// <summary>
        /// 设置映射端口
        /// </summary>
        /// <param name="privatePort"></param>
        /// <param name="publicPort"></param>
        /// <returns></returns>
        public Task<bool> SetPortMap(int privatePort, int publicPort);


        public Task<List<string>> GetTunnelTransportMachineIds();
        /// <summary>
        /// 获取打洞协议列表
        /// </summary>
        /// <returns></returns>
        public Task<List<TunnelTransportItemInfo>> GetTunnelTransports(string machineId);
        /// <summary>
        /// 保存打洞协议列表
        /// </summary>
        /// <param name="list"></param>
        public Task<bool> SetTunnelTransports(string machineId, List<TunnelTransportItemInfo> list);
        /// <summary>
        /// 保存打洞协议列表
        /// </summary>
        /// <param name="machineId"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public Task<bool> SetTunnelTransports(string machineId, List<ITunnelTransport> list);

        public Action OnChanged { get; set; }

        /// <summary>
        /// 配置的额外网络层级
        /// </summary>
        public TunnelPublicNetworkInfo Network { get; }
        /// <summary>
        /// 设置额外的网关层级
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public Task<bool> SetNetwork(TunnelPublicNetworkInfo network);

        public Task<bool> SetInIp(IPAddress ip);
        public Task<bool> SetMesh(TunnelMeshInfo mesh);

        public Task<List<PublicEndpointSample>> LoadRadarSamples();
        public Task<bool> SaveRadarSamples(List<PublicEndpointSample> samples);
    }
}
