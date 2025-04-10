namespace linker.app.Services
{
    public class InitializeService
    {
        public event Action OnInitialized;

        public bool IsInitialized { get; private set; }
        public void SendOnInitialized()
        {
            IsInitialized = true;
            OnInitialized?.Invoke();
        }
    }
}
