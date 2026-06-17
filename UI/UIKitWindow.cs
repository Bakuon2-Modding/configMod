using BepInEx.Configuration;
using UnityEngine;

namespace ConfigMod
{
    public class ConfigModWindow : MonoBehaviour
    {
        // ウィンドウ開閉キー
        public static ConfigEntry<KeyCode> ToggleKey;

        private SettingsWindow settingsWindow;
        private UIVisibilityWindow visibilityWindow;

        private bool isVisible = false;
        private string rebindingAction = null;  // null = リバインド待ちなし

        // 仮想座標 (1080p 基準) でのウィンドウ位置・サイズ
        private Rect windowRect = new Rect(100, 100, 500, 320);
        private Vector2 scrollPos = Vector2.zero;

        // タブ: 0=キーコンフィグ, 1=設定, 2=UI表示設定, 3=難易度
        private int activeTab = 0;
        private static readonly string[] TabLabels = { "キーコンフィグ", "設定", "UI表示設定", "難易度" };

        // タブごとのウィンドウ高さ (1080p 基準)
        private static readonly float[] TabWindowHeights = { 320f, 260f, 260f, 240f };

        private void Start()
        {
            settingsWindow   = GetComponent<SettingsWindow>();
            visibilityWindow = GetComponent<UIVisibilityWindow>();
        }

        public void Initialize(ConfigFile config)
        {
            ToggleKey = config.Bind(
                "General", "ToggleKey", KeyCode.F1,
                "MODウィンドウの開閉キー");
        }

        private void Update()
        {
            ConfigModInput.UpdateState();

            // リバインド状態を GameActions に伝える
            ConfigModInput.IsRebinding = (rebindingAction != null);

            // ウィンドウ開閉
            if (Input.GetKeyDown(ToggleKey.Value))
            {
                isVisible = !isVisible;
                rebindingAction = null;
            }

            // リバインド中のキー入力待ち
            if (rebindingAction != null)
            {
                InputBinding detected = ConfigModInput.DetectInput();
                if (detected == null) return;

                // Escape でキャンセル
                if (detected.Type == InputBinding.BindingType.Key &&
                    detected.KeyCode == KeyCode.Escape)
                {
                    rebindingAction = null;
                    return;
                }

                ConfigModInput.SetBinding(rebindingAction, detected);
                rebindingAction = null;
            }
        }

        private void OnGUI()
        {
            if (!isVisible) return;

            // 4K 等の高解像度でも読みやすいよう 1080p 基準でスケーリング
            float scale = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.TRS(
                Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            // 背景の不透明度を上げて視認性を改善
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.92f);

            windowRect = GUILayout.Window(
                98765, windowRect, DrawWindow,
                "[" + ToggleKey.Value + " で開閉]");

            GUI.backgroundColor = prevBg;
        }

        private void DrawWindow(int id)
        {
            // タブ切り替え
            int newTab = GUILayout.Toolbar(activeTab, TabLabels);
            if (newTab != activeTab)
            {
                activeTab = newTab;
                scrollPos = Vector2.zero;
                windowRect.height = TabWindowHeights[activeTab];
            }

            GUILayout.Space(4);

            switch (activeTab)
            {
                case 0: DrawKeyConfigTab(); break;
                case 1:
                    if (settingsWindow != null) settingsWindow.DrawTabContent();
                    break;
                case 2:
                    if (visibilityWindow != null) visibilityWindow.DrawTabContent();
                    break;
                case 3:
                    if (settingsWindow != null) settingsWindow.DrawDifficultyTab();
                    break;
            }

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("閉じる", GUILayout.Width(80)))
            {
                isVisible = false;
                rebindingAction = null;
            }
            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }

        private void DrawKeyConfigTab()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(180));

            foreach (string action in ConfigModInput.ActionOrder)
            {
                InputBinding binding = ConfigModInput.GetBinding(action);
                string bindingName = binding != null ? binding.GetDisplayName() : "---";

                GUILayout.BeginHorizontal();
                GUILayout.Label(GetActionLabel(action), GUILayout.Width(160));

                if (rebindingAction == action)
                {
                    GUILayout.Label("▶ キー/ボタンを押してください...", GUILayout.Width(200));
                    if (GUILayout.Button("キャンセル", GUILayout.Width(70)))
                        rebindingAction = null;
                }
                else
                {
                    GUILayout.Label(bindingName, GUILayout.Width(130));
                    if (GUILayout.Button("変更", GUILayout.Width(45)))
                        rebindingAction = action;
                    if (GUILayout.Button("↩", GUILayout.Width(25)))
                        ConfigModInput.ResetToDefault(action);
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("すべてデフォルトに戻す", GUILayout.ExpandWidth(false)))
            {
                foreach (string action in ConfigModInput.ActionOrder)
                    ConfigModInput.ResetToDefault(action);
            }
            GUILayout.EndHorizontal();
        }

        private static string GetActionLabel(string action)
        {
            switch (action)
            {
                case "Screenshot":   return "スクリーンショット";
                case "UIToggle":     return "UI 表示/非表示";
                case "MenuToggle":   return "メニュー 開閉";
                case "SuteageItem1": return "アイテム1使用";
                case "SuteageItem2": return "アイテム2使用";
                case "SuteageItem3": return "アイテム3使用";
                default:             return action;
            }
        }
    }
}
