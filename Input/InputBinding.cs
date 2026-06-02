using System;
using System.Collections.Generic;
using UnityEngine;

namespace ConfigMod
{
    // キーバインドを表すクラス。キーボード/マウス/ジョイスティックボタン (Key) か
    // XInput (Xbox系コントローラー) ボタン (XInput) の2種類をサポートする。
    // Key: Modifier + KeyCode の同時押し対応 (Modifier == None なら単押し)
    // XInput: XInputMask に複数ビットを立てることで同時押し対応
    public class InputBinding
    {
        public enum BindingType { Key, XInput }

        public readonly BindingType Type;
        public readonly KeyCode KeyCode;     // Type == Key のとき有効 (メインキー)
        public readonly KeyCode Modifier;    // Type == Key のとき有効 (修飾キー、None = 単押し)
        public readonly ushort XInputMask;   // Type == XInput のとき有効 (複数ビット = 同時押し)

        public InputBinding(KeyCode keyCode) : this(KeyCode.None, keyCode) { }

        public InputBinding(KeyCode modifier, KeyCode keyCode)
        {
            Type = BindingType.Key;
            Modifier = modifier;
            KeyCode = keyCode;
        }

        public InputBinding(ushort xInputMask)
        {
            Type = BindingType.XInput;
            XInputMask = xInputMask;
        }

        // BepInEx Config 保存用文字列
        // Key単押し: "Key:Alpha1"
        // Keyコンボ: "Key:LeftControl+Alpha1"
        // XInput:   "XInput:4352" (複数ビットも同じフォーマット)
        public string ToConfigString()
        {
            if (Type == BindingType.Key)
            {
                if (Modifier != KeyCode.None)
                    return "Key:" + Modifier.ToString() + "+" + KeyCode.ToString();
                return "Key:" + KeyCode.ToString();
            }
            return "XInput:" + XInputMask.ToString();
        }

        public static InputBinding FromConfigString(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            var parts = s.Split(':');
            if (parts.Length < 2) return null;

            if (parts[0] == "Key")
            {
                // コンボ形式: "LeftControl+Alpha1"
                var keys = parts[1].Split('+');
                if (keys.Length == 2)
                {
                    KeyCode mod, main;
                    if (Enum.TryParse<KeyCode>(keys[0], out mod) &&
                        Enum.TryParse<KeyCode>(keys[1], out main))
                        return new InputBinding(mod, main);
                }
                KeyCode kc;
                if (Enum.TryParse<KeyCode>(parts[1], out kc))
                    return new InputBinding(kc);
            }
            else if (parts[0] == "XInput")
            {
                ushort mask;
                if (ushort.TryParse(parts[1], out mask))
                    return new InputBinding(mask);
            }
            return null;
        }

        // UI 表示用の名前
        public string GetDisplayName()
        {
            if (Type == BindingType.Key)
            {
                if (Modifier != KeyCode.None)
                    return Modifier.ToString() + " + " + KeyCode.ToString();
                return KeyCode.ToString();
            }

            // XInput: 複数ビット対応
            var parts = new List<string>();
            foreach (ushort btn in ConfigModXInput.AllButtons)
            {
                if ((XInputMask & btn) == 0) continue;
                parts.Add(GetXInputButtonName(btn));
            }
            return parts.Count > 0
                ? string.Join(" + ", parts.ToArray())
                : "XInput:0x" + XInputMask.ToString("X4");
        }

        private static string GetXInputButtonName(ushort mask)
        {
            switch (mask)
            {
                case ConfigModXInput.DPAD_UP:    return "D-pad ↑";
                case ConfigModXInput.DPAD_DOWN:  return "D-pad ↓";
                case ConfigModXInput.DPAD_LEFT:  return "D-pad ←";
                case ConfigModXInput.DPAD_RIGHT: return "D-pad →";
                case ConfigModXInput.BTN_START:  return "Start";
                case ConfigModXInput.BTN_BACK:   return "Back";
                case ConfigModXInput.BTN_L3:     return "L3";
                case ConfigModXInput.BTN_R3:     return "R3";
                case ConfigModXInput.BTN_LB:     return "LB";
                case ConfigModXInput.BTN_RB:     return "RB";
                case ConfigModXInput.BTN_A:      return "A";
                case ConfigModXInput.BTN_B:      return "B";
                case ConfigModXInput.BTN_X:      return "X";
                case ConfigModXInput.BTN_Y:      return "Y";
                default:                     return "XInput:0x" + mask.ToString("X4");
            }
        }
    }
}
