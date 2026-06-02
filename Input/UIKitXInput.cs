using System.Runtime.InteropServices;

namespace ConfigMod
{
    // XInput (Xbox コントローラー) のボタン状態を毎フレーム追跡するクラス。
    // xinput9_1_0.dll を P/Invoke で呼び出す。コントローラー未接続時は無害に無効化される。
    public static class ConfigModXInput
    {
        // ボタンマスク定数 (XINPUT_GAMEPAD wButtons)
        public const ushort DPAD_UP    = 0x0001;
        public const ushort DPAD_DOWN  = 0x0002;
        public const ushort DPAD_LEFT  = 0x0004;
        public const ushort DPAD_RIGHT = 0x0008;
        public const ushort BTN_START  = 0x0010;
        public const ushort BTN_BACK   = 0x0020;
        public const ushort BTN_L3     = 0x0040;
        public const ushort BTN_R3     = 0x0080;
        public const ushort BTN_LB     = 0x0100;
        public const ushort BTN_RB     = 0x0200;
        public const ushort BTN_A      = 0x1000;
        public const ushort BTN_B      = 0x2000;
        public const ushort BTN_X      = 0x4000;
        public const ushort BTN_Y      = unchecked((ushort)0x8000);

        // 検出対象のボタン一覧
        public static readonly ushort[] AllButtons = new ushort[]
        {
            DPAD_UP, DPAD_DOWN, DPAD_LEFT, DPAD_RIGHT,
            BTN_START, BTN_BACK, BTN_L3, BTN_R3,
            BTN_LB, BTN_RB, BTN_A, BTN_B, BTN_X, BTN_Y
        };

        [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
        private static extern uint XInputGetState_Impl(int dwUserIndex, out XINPUT_STATE pState);

        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_STATE
        {
            public uint dwPacketNumber;
            public XINPUT_GAMEPAD Gamepad;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_GAMEPAD
        {
            public ushort wButtons;
            public byte bLeftTrigger;
            public byte bRightTrigger;
            public short sThumbLX;
            public short sThumbLY;
            public short sThumbRX;
            public short sThumbRY;
        }

        private static ushort prevButtons = 0;
        private static ushort curButtons  = 0;
        private static bool available = true;

        public static bool IsAvailable { get { return available; } }

        // 毎フレーム Update() から呼ぶ
        public static void UpdateState()
        {
            if (!available) return;
            try
            {
                XINPUT_STATE state;
                uint result = XInputGetState_Impl(0, out state);
                prevButtons = curButtons;
                curButtons  = (result == 0) ? state.Gamepad.wButtons : (ushort)0;
            }
            catch
            {
                available   = false;
                prevButtons = 0;
                curButtons  = 0;
            }
        }

        // mask に含まれるすべてのボタンが現在押されており、
        // かつ mask の中で少なくとも1ボタンがこのフレームで新たに押されたか。
        // 単押し (1ビット) でも同時押し (複数ビット) でも正しく動作する。
        public static bool IsButtonDown(ushort mask)
        {
            if ((curButtons & mask) != mask) return false;
            ushort newlyPressed = (ushort)(curButtons & ~prevButtons);
            return (newlyPressed & mask) != 0;
        }

        // 押しっぱなしかどうか (全ビット一致)
        public static bool IsButtonHeld(ushort mask)
        {
            return (curButtons & mask) == mask;
        }

        // このフレームで新たに押されたボタンがあれば、現在押されているボタン全体のマスクを返す。
        // なければ 0。リバインド時の同時押し検出に使用。
        public static ushort GetAnyButtonDown()
        {
            ushort newlyPressed = (ushort)(curButtons & ~prevButtons);
            if (newlyPressed == 0) return 0;
            return curButtons;
        }
    }
}
