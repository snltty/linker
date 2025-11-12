using linker.libs;
using linker.libs.timer;
using linker.messenger.sforward.messenger;
using linker.messenger.sforward.server;
using linker.messenger.signin;
using System.Net;
using System.Net.NetworkInformation;

namespace linker.messenger.sforward.client
{
    /// <summary>
    /// 穿透测试
    /// </summary>
    public sealed class SForwardClientTestTransfer
    {
        private readonly SignInClientState signInClientState;
        private readonly ISignInClientStore signInClientStore;
        private readonly IMessengerSender messengerSender;
        private readonly ISerializer serializer;

        public List<SForwardServerNodeReportInfo> Nodes { get; private set; } = new List<SForwardServerNodeReportInfo>();

        public SForwardClientTestTransfer(SignInClientState signInClientState, ISignInClientStore signInClientStore, IMessengerSender messengerSender, ISerializer serializer)
        {
            this.signInClientState = signInClientState;
            this.signInClientStore = signInClientStore;
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            TestTask();

        }

        public string DefaultId()
        {
            if (Nodes.Count > 0) return Nodes[0].Id;
            return string.Empty;
        }


        private readonly LastTicksManager lastTicksManager = new LastTicksManager();
        public void Subscribe()
        {
            lastTicksManager.Update();
        }
        public async Task TaskNodes()
        {
            try
            {
                var resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)SForwardMessengerIds.Nodes
                });
                if(resp.Code == MessageResponeCodes.OK)
                {
                    Nodes = serializer.Deserialize<List<SForwardServerNodeReportInfo>>(resp.Data.Span);
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
        }
        private async Task PingNodes()
        {
            try
            {
                var tasks = Nodes.Select(async (c) =>
                {
                    c.Address = c.Address == null || c.Address.Equals(IPAddress.Any) ? signInClientState.Connection.Address.Address : c.Address;

                    using Ping ping = new Ping();
                    var resp = await ping.SendPingAsync(c.Address, 1000);
                    c.Delay = resp.Status == IPStatus.Success ? (int)resp.RoundtripTime : -1;
                });
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
        }


        private void TestTask()
        {
            TimerHelper.SetIntervalLong(async () =>
            {
                if ((lastTicksManager.DiffLessEqual(3000) || Nodes.Count <= 0) && signInClientState.Connected)
                {
                    await TaskNodes().ConfigureAwait(false);
                    await PingNodes().ConfigureAwait(false);
                }
            }, 3000);
        }

    }
}