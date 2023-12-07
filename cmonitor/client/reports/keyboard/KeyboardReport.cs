namespace cmonitor.client.reports.keyboard
{
    public sealed class KeyboardReport : IReport
    {
        public string Name => "Keyboard";

        private readonly IKeyboard keyboard;

        public KeyboardReport(IKeyboard keyboard)
        {
            this.keyboard = keyboard;
        }
        public object GetReports(ReportType reportType)
        {
            if (reportType == ReportType.Full)
            {
            }
            return null;
        }

        public void KeyBoard(KeyBoardInputInfo inputInfo)
        {
            keyboard.KeyBoard(inputInfo);
        }
        public void MouseSet(MouseSetInfo setInfo)
        {
            keyboard.MouseSet(setInfo);
        }
        public void MouseClick(MouseClickInfo clickInfo)
        {
            keyboard.MouseClick(clickInfo);
        }

        public void CtrlAltDelete()
        {
            keyboard.CtrlAltDelete();
        }
    }


}
