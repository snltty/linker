using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static cmonitor.server.client.reports.screen.winapiss.User32;

namespace cmonitor.server.client.reports.screen.helpers
{
    public class MouseHelper
    {
        public static bool MouseMove(int x, int y)
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = InputType.MOUSE;
            inputs[0].U.mi.dx = x;
            inputs[0].U.mi.dy = y;
            inputs[0].U.mi.dwFlags = MOUSEEVENTF.MOVE;

            // 调用SendInput发送输入事件
            uint result = SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));

            return result != 0;
        }
    }
}
