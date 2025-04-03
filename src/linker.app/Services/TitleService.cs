namespace linker.app.Services
{
    public class TitleService
    {
        public string CurrentTitle { get; private set; } = "linker";

        public event Action OnTitleChanged;

        public void SetTitle(string newTitle)
        {
            CurrentTitle = newTitle;
            OnTitleChanged?.Invoke();
        }
    }
}
