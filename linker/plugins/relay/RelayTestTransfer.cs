using linker.config;
using linker.plugins.relay.transport;
using linker.libs;

namespace linker.plugins.relay
{
    /// <summary>
    /// 中继
    /// </summary>
    public sealed class RelayTestTransfer
    {
        private readonly FileConfig fileConfig;
        private readonly RelayTransfer relayTransfer;

        public RelayTestTransfer(FileConfig fileConfig, RelayTransfer relayTransfer)
        {
            this.fileConfig = fileConfig;
            this.relayTransfer = relayTransfer;
            TestTask();
        }


        private readonly LastTicksManager lastTicksManager = new LastTicksManager();
        public void Subscribe()
        {
            lastTicksManager.Update();
        }
        private async Task TaskRelay()
        {
            try
            {
                foreach (var server in fileConfig.Data.Client.Relay.Servers)
                {
                    ITransport transport = relayTransfer.Transports.FirstOrDefault(d => d.Type == server.RelayType);
                    if (transport == null) continue;

                    server.Delay = await transport.RelayTestAsync(new RelayTestInfo
                    {
                        MachineId = fileConfig.Data.Client.Id,
                        SecretKey = server.SecretKey
                    });
                }
            }
            catch (Exception)
            {
            }
        }
        private void TestTask()
        {
            TimerHelper.SetInterval(async () =>
            {
                if (lastTicksManager.DiffLessEqual(3000))
                {
                    await TaskRelay();
                }
                return true;
            }, () => lastTicksManager.DiffLessEqual(3000) ? 3000 : 30000);
        }
      
    }
}