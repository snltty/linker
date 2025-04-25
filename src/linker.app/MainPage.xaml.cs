
using linker.app.Services;

namespace linker.app
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            LoadingOverlay.IsVisible = true;
            webview.IsVisible = false;
            IPlatformApplication.Current.Services.GetService<InitializeService>().OnInitialized += () =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LoadingOverlay.IsVisible = false;
                    webview.IsVisible = true;
                    webview.Source = new Uri($"http://127.0.0.1:1804?t={DateTime.Now.Ticks}");
                });
               
            };
        }
    }
}
