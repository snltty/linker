using cmonitor.plugins.system.report;
using cmonitor.server;
using MemoryPack;

namespace cmonitor.plugins.system.messenger
{
    public sealed class SystemClientMessenger : IMessenger
    {
        private readonly SystemReport report;
        public SystemClientMessenger(SystemReport report)
        {
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
