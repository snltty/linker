using System.Runtime.InteropServices;

namespace cmonitor.clipboard.win
{
    public partial class MainForm : Form
    {
        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_APPWINDOW = 0x40000;
                const int WS_EX_TOOLWINDOW = 0x80;
                CreateParams cp = base.CreateParams;
                cp.ExStyle &= (~WS_EX_APPWINDOW);
                cp.ExStyle |= WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        private IntPtr nextClipboardViewer;
        const int WM_DRAWCLIPBOARD = 0x0308;
        const uint CF_TEXT = 1;
        const uint CF_HDROP = 15;
        public MainForm()
        {
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;

            InitializeComponent();
            nextClipboardViewer = SetClipboardViewer(this.Handle);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_DRAWCLIPBOARD)
            {
                OnClipboardChanged();
            }
        }
        private void OnClipboardChanged()
        {
            if (OpenClipboard(IntPtr.Zero))
            {
                uint format = 0;
                while ((format = EnumClipboardFormats(format)) != 0)
                {
                    switch (format)
                    {
                        case CF_TEXT:
                            HandleTextData();
                            break;
                        case CF_HDROP:
                            HandleFileListData();
                            break;
                    }
                }
                CloseClipboard();
            }
        }
        private void HandleTextData()
        {
            IntPtr clipboardData = GetClipboardData(CF_TEXT);
            if (clipboardData != IntPtr.Zero)
            {
                string text = Marshal.PtrToStringAnsi(clipboardData);
                this.Invoke(() =>
                {
                    textBox1.Text = text;
                });
            }
        }
        private void HandleFileListData()
        {
            IntPtr clipboardData = GetClipboardData(CF_HDROP);
            if (clipboardData != IntPtr.Zero)
            {
                IntPtr fileDropHandle = Marshal.ReadIntPtr(clipboardData);
                uint fileCount = DragQueryFile(fileDropHandle, 0xFFFFFFFF, null, 0);

                List<string> fileList = new List<string>();
                for (uint i = 0; i < fileCount; i++)
                {
                    uint bufferSize = DragQueryFile(fileDropHandle, i, null, 0) + 1;
                    char[] fileName = new char[bufferSize];

                    DragQueryFile(fileDropHandle, i, fileName, bufferSize);
                    string filePath = new string(fileName);

                    fileList.Add(filePath);
                }

                DragFinish(fileDropHandle);

                this.Invoke(() =>
                {
                    textBox1.Text = string.Join("\r\n", fileList);
                });
            }
        }



        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool IsClipboardFormatAvailable(uint format);
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint EnumClipboardFormats(uint format);
        [DllImport("shell32.dll")]
        static extern uint DragQueryFile(IntPtr hDrop, uint iFile, char[] lpszFile, uint cch);

        [DllImport("shell32.dll")]
        static extern void DragFinish(IntPtr hDrop);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool CloseClipboard();

        [DllImport("kernel32.dll")]
        static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        static extern bool GlobalUnlock(IntPtr hMem);

    }
}
