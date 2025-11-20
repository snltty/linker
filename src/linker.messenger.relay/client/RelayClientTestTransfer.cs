using linker.libs;
using linker.libs.timer;
using linker.messenger.relay.client.transport;
using linker.messenger.relay.server;
using linker.messenger.signin;
using System.Net;
using System.Net.NetworkInformation;

namespace linker.messenger.relay.client
{
    /// <summary>
    /// 中继
    /// </summary>
    public sealed class RelayClientTestTransfer
    {
        private readonly RelayClientTransfer relayTransfer;
        private readonly SignInClientState signInClientState;
        private readonly ISignInClientStore signInClientStore;
        private readonly IRelayClientStore relayClientStore;

        public List<RelayServerNodeReportInfo> Nodes { get; private set; } = new List<RelayServerNodeReportInfo>();

        public RelayClientTestTransfer(RelayClientTransfer relayTransfer, SignInClientState signInClientState, ISignInClientStore signInClientStore, IRelayClientStore relayClientStore)
        {
            this.relayTransfer = relayTransfer;
            this.signInClientState = signInClientState;
            this.signInClientStore = signInClientStore;
            this.relayClientStore = relayClientStore;

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
                IRelayClientTransport transport = relayTransfer.Transports.FirstOrDefault(d => d.Type == relayClientStore.Server.RelayType);
                if (transport != null)
                {
                    Nodes = await transport.RelayTestAsync().ConfigureAwait(false);
                    var tasks = Nodes.Select(async (c) =>
                    {
                        c.EndPoint = c.EndPoint == null || c.EndPoint.Address.Equals(IPAddress.Any) ? signInClientState.Connection.Address : c.EndPoint;

                        using Ping ping = new Ping();
                        var resp = await ping.SendPingAsync(c.EndPoint.Address, 1000);
                        c.Delay = resp.Status == IPStatus.Success ? (int)resp.RoundtripTime : -1;
                    });
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
            }
        }
        private void TestTask()
        {
            TimerHelper.SetIntervalLong(async () =>
            {
                if ((lastTicksManager.DiffLessEqual(3000) || Nodes.Count <= 0) && signInClientState.Connected)
                {
                    await TaskRelay().ConfigureAwait(false);
                }
            }, 3000);
        }

    }
}