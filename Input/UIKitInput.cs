using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace ConfigMod
{
    // ConfigMod が管理するアクション (SuteageItem1/2/3) の入力管理。
    // KeyCode に加えて XInput (Xbox 十字キー等) もサポートする。
    // ConfigurableInput は KeyCode 専用なので、これらのアクションは別管理にする。
    public static class ConfigModInput
    {
        private static readonly Dictionary<string, InputBinding> bindings
            = new Dictionary<string, InputBinding>();

        private static readonly Dictionary<string, ConfigEntry<string>> configEntries
            = new Dictionary<string, ConfigEntry<string>>();

        // 表示順 (キーコンフィグタブで使用)
        public static readonly string[] ActionOrder = new string[]
        {
            "Screenshot",
            "UIToggle",
            "MenuToggle",
            "SuteageItem1", "SuteageItem2", "SuteageItem3"
        };

        // リバインド中は GameActions がアクションを実行しないためのフラグ
        public static bool IsRebinding { get; set; }

        // アクションを登録し、保存済みバインドを復元する
        public static void RegisterAction(
            string action, InputBinding defaultBinding, ConfigFile config, string desc)
        {
            string defaultStr = defaultBinding.ToConfigString();
            var entry = config.Bind("KeyBindings", action, defaultStr, desc);
            configEntries[action] = entry;

            InputBinding loaded = InputBinding.FromConfigString(entry.Value) ?? defaultBinding;
            bindings[action] = loaded;
        }

        // 毎フレーム Update() から呼ぶ。複数コンポーネントから呼ばれても1フレーム1回だけ更新する。
        private static int lastUpdatedFrame = -1;
        public static void UpdateState()
        {
            int frame = UnityEngine.Time.frameCount;
            if (frame == lastUpdatedFrame) return;
            lastUpdatedFrame = frame;
            ConfigModXInput.UpdateState();
        }

        // 指定アクションがこのフレームで押された瞬間かどうか。
        // Key: Modifier が None でなければ Modifier を押しながら KeyCode を押した瞬間。
        // XInput: 複数ビットマスクは全ボタン押下中に新規ボタンが押された瞬間。
        public static bool GetDown(string action)
        {
            if (!bindings.ContainsKey(action)) return false;
            var b = bindings[action];
            if (b.Type == InputBinding.BindingType.Key)
            {
                if (b.Modifier != KeyCode.None)
                    return Input.GetKey(b.Modifier) && Input.GetKeyDown(b.KeyCode);
                return Input.GetKeyDown(b.KeyCode);
            }
            return ConfigModXInput.IsButtonDown(b.XInputMask);
        }

        // バインドを変更して Config に保存
        public static void SetBinding(string action, InputBinding binding)
        {
            bindings[action] = binding;
            if (configEntries.ContainsKey(action))
                configEntries[action].Value = binding.ToConfigString();
        }

        // デフォルトに戻す
        public static void ResetToDefault(string action)
        {
            if (!configEntries.ContainsKey(action)) return;
            string defaultStr = (string)configEntries[action].DefaultValue;
            InputBinding def = InputBinding.FromConfigString(defaultStr);
            if (def == null) return;
            bindings[action] = def;
            configEntries[action].Value = defaultStr;
        }

        public static InputBinding GetBinding(string action)
        {
            return bindings.ContainsKey(action) ? bindings[action] : null;
        }

        public static string GetDefaultDisplayName(string action)
        {
            if (!configEntries.ContainsKey(action)) return "?";
            string defaultStr = (string)configEntries[action].DefaultValue;
            InputBinding def = InputBinding.FromConfigString(defaultStr);
            return def != null ? def.GetDisplayName() : "?";
        }

        // リバインドモード用: このフレームに押された入力 (同時押し含む) を返す。
        // 何も押されていなければ null。KeyCode.Escape が返ったらキャンセル扱いにすること。
        public static InputBinding DetectInput()
        {
            // 1. KeyCode スキャン
            if (Input.anyKeyDown)
            {
                // 修飾キーが押されているか確認
                KeyCode modifier = KeyCode.None;
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    modifier = KeyCode.LeftControl;
                else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                    modifier = KeyCode.LeftAlt;
                else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    modifier = KeyCode.LeftShift;

                foreach (KeyCode kc in Enum.GetValues(typeof(KeyCode)))
                {
                    if (!Input.GetKeyDown(kc)) continue;
                    // 修飾キー自体はメインキーにしない
                    if (kc == KeyCode.LeftControl  || kc == KeyCode.RightControl ||
                        kc == KeyCode.LeftAlt      || kc == KeyCode.RightAlt     ||
                        kc == KeyCode.LeftShift    || kc == KeyCode.RightShift)
                        continue;
                    return new InputBinding(modifier, kc);
                }
            }

            // 2. XInput ボタン (現在押されている全ボタンを同時押しコンボとして返す)
            ushort xbtn = ConfigModXInput.GetAnyButtonDown();
            if (xbtn != 0)
                return new InputBinding(xbtn);

            return null;
        }
    }
}
