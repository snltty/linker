
using linker.app.Services;

namespace linker.app
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            IPlatformApplication.Current.Services.GetService<InitializeService>().OnInitialized += () =>
            {
                webview.Source = new Uri($"http://127.0.0.1:1804?t={DateTime.Now.Ticks}");
            };
        }
    }

}
