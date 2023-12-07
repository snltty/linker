using cmonitor.client.reports.system;
using MemoryPack;

namespace cmonitor.service.messengers.system
{
    public sealed class SystemMessenger : IMessenger
    {
        private Config config;
        private readonly SystemReport report;
        public SystemMessenger(Config config, SystemReport report)
        {
            this.config = config;
            this.report = report;
        }

        [MessengerId((ushort)SystemMessengerIds.Password)]
        public void Password(IConnection connection)
        {
            PasswordInputInfo passwordInputInfo = MemoryPackSerializer.Deserialize<PasswordInputInfo>(connection.ReceiveRequestWrap.Payload.Span);
            report.Password(passwordInputInfo);
        }

        [MessengerId((ushort)SystemMessengerIds.RegistryOptions)]
        public void RegistryOptions(IConnection connection)
        {
            SystemOptionUpdateInfo registryUpdateInfo = MemoryPackSerializer.Deserialize<SystemOptionUpdateInfo>(connection.ReceiveRequestWrap.Payload.Span);
            report.OptionUpdate(registryUpdateInfo);
        }


    }
}
