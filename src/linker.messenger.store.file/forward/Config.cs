
using linker.messenger.forward;
using LiteDB;
using System.Text.Json.Serialization;

namespace linker.messenger.store.file
{
    public sealed partial class RunningConfigInfo
    {
        /// <summary>
        /// 端口转发配置
        /// </summary>
        public List<ForwardInfo> Forwards { get; set; } = new List<ForwardInfo>();
    }
}
