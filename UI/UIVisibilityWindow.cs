using BepInEx.Configuration;
using UnityEngine;

namespace ConfigMod
{
    public class UIVisibilityWindow : MonoBehaviour
    {
        private ConfigEntry<bool> cfgUIToggleButton;
        private ConfigEntry<bool> cfgActionButtons;
        private ConfigEntry<bool> cfgMyCard;
        private ConfigEntry<bool> cfgRoomList;

        private bool lastUIToggleButton;
        private bool lastActionButtons;
        private bool lastMyCard;
        private bool lastRoomList;

        public void Initialize(ConfigFile config)
        {
            cfgUIToggleButton = config.Bind("UIVisibility", "ShowUIToggleButton", true, "UIのオンオフボタン");
            cfgActionButtons  = config.Bind("UIVisibility", "ShowActionButtons",  true, "アクションボタン");
            cfgMyCard         = config.Bind("UIVisibility", "ShowMyCardButton",   true, "マイカードボタン");
            cfgRoomList       = config.Bind("UIVisibility", "ShowRoomList",       true, "ルーム一覧ボタン");

            lastUIToggleButton = cfgUIToggleButton.Value;
            lastActionButtons  = cfgActionButtons.Value;
            lastMyCard         = cfgMyCard.Value;
            lastRoomList       = cfgRoomList.Value;
        }

        private void LateUpdate()
        {
            ApplyEntry(cfgUIToggleButton, ref lastUIToggleButton,
                () => { var m = SingletonMonoBehaviour<MenuScreenManager>.Instance; return m != null ? m.UIVisbleSwitchButtonCanvas : null; });

            ApplyEntry(cfgActionButtons, ref lastActionButtons,
                () => InputManagerController.Instance != null ? InputManagerController.Instance.inputButtonCanvas : null);

            ApplyEntry(cfgMyCard, ref lastMyCard,
                () => { var m = SingletonMonoBehaviour<MenuScreenManager>.Instance; return m != null ? m.targetPlayerMyCardOpenButton : null; });

            ApplyEntry(cfgRoomList, ref lastRoomList,
                () => { var m = SingletonMonoBehaviour<MenuScreenManager>.Instance; return m != null ? m.showRoomListButton : null; });
        }

        private static void ApplyEntry(ConfigEntry<bool> cfg, ref bool last, System.Func<GameObject> getter)
        {
            bool desired = cfg.Value;
            bool changed = (desired != last);
            if (changed) last = desired;

            var go = getter();
            if (go == null) return;

            if (changed)
                go.SetActive(desired);
            else if (!desired && go.activeSelf)
                go.SetActive(false);
        }

        // KeyConfigWindow の UI表示設定タブから呼ばれる
        public void DrawTabContent()
        {
            GUILayout.Space(4);
            DrawToggle(cfgUIToggleButton, "UIのオンオフボタン");
            DrawToggle(cfgActionButtons,   "アクションボタン");
            DrawToggle(cfgMyCard,          "マイカードボタン");
            DrawToggle(cfgRoomList,        "ルーム一覧ボタン");
        }

        private static void DrawToggle(ConfigEntry<bool> cfg, string label)
        {
            bool newVal = GUILayout.Toggle(cfg.Value, "  " + label);
            if (newVal != cfg.Value) cfg.Value = newVal;
        }
    }
}
