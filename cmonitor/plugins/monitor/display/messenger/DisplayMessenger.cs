using cmonitor.plugins.display.report;
using cmonitor.server;

namespace cmonitor.plugins.display.messenger
{
    public sealed class DisplayClientMessenger : IMessenger
    {
        private readonly DisplayReport displayReport;


        public DisplayClientMessenger(DisplayReport displayReport)
        {
            this.displayReport = displayReport;
        }

        [MessengerId((ushort)DisplayMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            if (connection.ReceiveRequestWrap.Payload.Length == 1)
            {
                byte state = connection.ReceiveRequestWrap.Payload.Span[0];
                displayReport.SetDisplayState(state == 1);
            }
        }
    }

}
