using cmonitor.plugins.viewer.config;
using common.libs;
using MemoryPack;
using System.Net;

namespace cmonitor.plugins.viewer.config
{
    [MemoryPackable]
    public sealed partial class ViewerRunningConfigInfo
    {
        public ViewerMode Mode { get; set; }

        public bool Open { get; set; }

        public string ShareId { get; set; } = Helper.GlobalString;

        /// <summary>
        /// 共享服务端机器名，在通知消息和代理时需要
        /// </summary>
        public string ServerMachine { get; set; } = string.Empty;
        /// <summary>
        /// 共享客户端机器名列表
        /// </summary>
        public string[] ClientMachines { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 共享连接串，提供给客户端的共享桌面工具，去连接服务
        /// </summary>
        public string ConnectStr { get; set; } = string.Empty;
        /// <summary>
        /// 共享服务的连接地址，在代理时需要
        /// 比如 B 是共享服务端，A是共享客户端
        /// 当连不上时，会需要代理，由B去连接A或者B去连接服务器，形成通道，那B这边还需要手动连接共享服务，就用这个去连
        /// </summary>
        [MemoryPackAllowSerialize]
        public IPEndPoint ConnectEP { get; set; }
    }

    public enum ViewerMode : byte
    {
        Client = 0,
        Server = 1,
    }

}



namespace cmonitor.client.running
{
    public sealed partial class RunningConfigInfo
    {
        private ViewerRunningConfigInfo viewer = new ViewerRunningConfigInfo();
        public ViewerRunningConfigInfo Viewer
        {
            get => viewer; set
            {
                Updated++;
                viewer = value;
            }
        }
    }
}