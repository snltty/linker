using MemoryPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace linker.plugins.turn.config
{
    [MemoryPackable]
    public sealed partial class TunnelsInfo
    {
        /// <summary>
        /// 谁
        /// </summary>
        public string MachineId { get; set; }
        /// <summary>
        /// 跟谁打洞成功过
        /// </summary>
        public List<string> MachineIds { get; set; }
    }
}
