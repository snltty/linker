using linker.messenger.entry;
using linker.messenger.tuntap;

namespace linker.app
{

    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();

        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);

            ITuntapClientStore tuntapClientStore = LinkerMessengerEntry.GetService<ITuntapClientStore>();
            tuntapClientStore.Info.IP = System.Net.IPAddress.Parse(IPEntry.Text);
            tuntapClientStore.Confirm();

#if ANDROID

             AndroidLinkerVpnService androidLinkerVpnService = new AndroidLinkerVpnService();
             if(androidLinkerVpnService.Running)
             {
                androidLinkerVpnService.StopVpnService();
             }
             else
             {
                androidLinkerVpnService.StartVpnService();
             }
#endif

        }
    }

}
