using linker.libs;

namespace linker.app
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // 注册全局异常处理
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            MainPage = new MainPage();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            HandleException(ex);
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
        }
        private void HandleException(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"全局异常: {ex}");
            LoggerHelper.Instance.Error($"app全局异常: {ex}");
        }
    }
}
