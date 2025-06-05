
using linker.app.Services;
using linker.libs;
using linker.messenger.api;
using linker.messenger.entry;

namespace linker.app
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            LoadingOverlay.IsVisible = true;
            webview.IsVisible = false;
            webview.Loaded += Webview_Loaded;
            IPlatformApplication.Current.Services.GetService<InitializeService>().OnInitialized += () =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IApiStore apiStore = LinkerMessengerEntry.GetService<IApiStore>();
                    webview.Source = new Uri($"http://127.0.0.1:{apiStore.Info.WebPort}?t={DateTime.Now.Ticks}/#/?api={apiStore.Info.WebPort}&psd={Helper.GlobalString}");
                });
            };
        }

        private void Webview_Loaded(object sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                while (string.IsNullOrWhiteSpace(await webview.EvaluateJavaScriptAsync("document.getElementById('app').innerText")))
                {
                    await Task.Delay(500);
                }
                await Task.WhenAll(webview.FadeTo(1, 500), LoadingOverlay.FadeTo(0, 500));
                webview.IsVisible = true;
                LoadingOverlay.IsVisible = false;

            });
        }
    }
}
