using linker.libs;
using linker.messenger.sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace linker.messenger.action
{
    public sealed class ActionSync : ISync
    {
        public string Name => "ActionStatic";

        private readonly ActionTransfer actionTransfer;
        private readonly ISerializer serializer;
        public ActionSync(ActionTransfer actionTransfer, ISerializer serializer)
        {
            this.actionTransfer = actionTransfer;
            this.serializer = serializer;
        }

        public Memory<byte> GetData()
        {
            return serializer.Serialize(actionTransfer.GetActionStaticArg());
        }

        public void SetData(Memory<byte> data)
        {
            actionTransfer.SetActionStaticArg(serializer.Deserialize<string>(data.Span));
        }
    }
}
