using System;
using System.Runtime.InteropServices;

namespace linker.libs.winapis
{
    internal sealed class Powrprof
    {

        [DllImport("powrprof.dll")]
        public static extern uint GetActivePwrScheme(out IntPtr pActivePolicy);
        [DllImport("powrprof.dll", SetLastError = true)]
        public static extern UInt32 PowerGetActiveScheme(IntPtr UserRootPowerKey, out IntPtr ActivePolicyGuid);

        [DllImport("powrprof.dll")]
        public static extern uint PowerWriteACValueIndex(IntPtr RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid,
    ref Guid PowerSettingGuid, uint AcValueIndex);

       public static  Guid powerButtonGuid = new Guid("4f971e89-eebd-4455-a8de-9e59040e7347"); // 电源按钮设置的GUID
        public static Guid sleepButtonGuid = new Guid("96996bc0-ad50-47ec-923b-6f418386bca1"); // 睡眠按钮设置的GUID
    }
}
