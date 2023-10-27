using cmonitor.server.client.reports.screen;
using MemoryPack;
using System.Runtime.InteropServices;

namespace cmonitor.server.client.reports.command
{
    public sealed class CommandReport : IReport
    {
        public string Name => "Command";

        public object GetReports(ReportType reportType)
        {
            return null;
        }

        /// <summary>
        /// 键盘输入
        /// </summary>
        /// <param name="inputInfo"></param>
        public void KeyBoard(KeyBoardInputInfo inputInfo)
        {
            WinApi.keybd_event(inputInfo.Key, 0, inputInfo.Type, 0);
        }

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
}
