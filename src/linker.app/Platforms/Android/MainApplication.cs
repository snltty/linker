using Android.App;
using Android.Runtime;
namespace linker.app
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
            Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping("MyCustomization", (handler, view) =>
            {
                handler.PlatformView.Settings.JavaScriptEnabled = true;
                handler.PlatformView.Settings.AllowFileAccess = true;
                handler.PlatformView.Settings.AllowFileAccessFromFileURLs = true;
                handler.PlatformView.Settings.AllowUniversalAccessFromFileURLs = true;
                handler.PlatformView.Settings.MixedContentMode = Android.Webkit.MixedContentHandling.AlwaysAllow;
                handler.PlatformView.ClearCache(true);
                handler.PlatformView.Settings.JavaScriptEnabled = true;
                handler.PlatformView.SetWebViewClient(new MyWebViewClient());
            });
        }

        public class MyWebViewClient : Android.Webkit.WebViewClient
        {
            public override bool ShouldOverrideUrlLoading(Android.Webkit.WebView view, Android.Webkit.IWebResourceRequest request)
            {
                if (request.Url.ToString() == "about:blank")
                {
                    return true;
                }
                return base.ShouldOverrideUrlLoading(view, request);
            }
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
