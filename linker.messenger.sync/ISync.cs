using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace linker.messenger.sync
{
    public interface ISync
    {
        public string Name { get; }
        public Memory<byte> GetData();
        public void SetData(Memory<byte> data);
    }

    public sealed partial class SyncInfo
    {
        public SyncInfo() { }
        public string Name { get; set; }
        public Memory<byte> Data { get; set; }
    }
}
