using System.Runtime.InteropServices;
using static linker.libs.winapis.User32;

namespace linker.libs.winapis
{
    internal sealed class MouseHelper
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
        public static bool MouseSet(int x, int y)
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = InputType.MOUSE;
            inputs[0].U.mi.dx = x;
            inputs[0].U.mi.dy = y;
            inputs[0].U.mi.dwFlags = MOUSEEVENTF.ABSOLUTE;

            // 调用SendInput发送输入事件
            uint result = SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));

            return result != 0;
        }
        public static bool MouseClick(uint flag, int mouseData)
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = InputType.MOUSE;
            inputs[0].U.mi.dx = 0;
            inputs[0].U.mi.dy = 0;
            inputs[0].U.mi.dwFlags = (MOUSEEVENTF)flag;
            inputs[0].U.mi.mouseData = mouseData;
            // 调用SendInput发送输入事件
            uint result = SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
            return result != 0;
        }
    }
}
