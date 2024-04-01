using MemoryPack;

namespace cmonitor.plugins.keyboard.report
{
    public interface IKeyboard
    {
        public void KeyBoard(KeyBoardInputInfo inputInfo);
        public void MouseSet(MouseSetInfo setInfo);
        public void MouseClick(MouseClickInfo clickInfo);

        public void CtrlAltDelete();
        public void WinL();
    }

    [MemoryPackable]
    public partial struct KeyBoardInputInfo
    {
        /// <summary>
        /// System.Windows.Forms.Keys
        /// </summary>
        public byte Key { get; set; }
        /// <summary>
        /// 0 down,2 up
        /// </summary>
        public int Type { get; set; }
    }

    [MemoryPackable]
    public partial struct MouseSetInfo
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
    [MemoryPackable]
    public partial struct MouseClickInfo
    {
        public uint Flag { get; set; }
        public int Data { get; set; }
    }
}
