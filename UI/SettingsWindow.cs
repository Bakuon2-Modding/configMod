using BepInEx.Configuration;
using MBakuon;
using UnityEngine;

namespace ConfigMod
{
    public class SettingsWindow : MonoBehaviour
    {
        private ConfigEntry<bool> showFPS;
        private ConfigEntry<bool> muteOnFocusLoss;
        private ConfigEntry<bool> hideMyAccessories;
        private ConfigEntry<bool> showChatLabelWhenUIHidden;

        // FPS文字列キャッシュ。毎フレームのstring生成を避けるため30フレームごとに更新。
        private string fpsText = "FPS: --";
        private int fpsFrameCounter = 0;

        public void Initialize(ConfigFile config)
        {
            showFPS                  = config.Bind("Settings", "ShowFPS",                  false, "FPS表示");
            muteOnFocusLoss          = config.Bind("Settings", "MuteOnFocusLoss",          false, "非アクティブ時にミュート");
            hideMyAccessories        = config.Bind("Settings", "HideMyAccessories",        false, "自分のアクセサリーを非表示");
            showChatLabelWhenUIHidden = config.Bind("Settings", "ShowChatLabelWhenUIHidden", false, "UI非表示時もショートカットメッセージを表示");
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (muteOnFocusLoss.Value)
                AudioListener.pause = !hasFocus;
        }

        private void Update()
        {
            if (!showFPS.Value) return;

            if (++fpsFrameCounter >= 30)
            {
                fpsFrameCounter = 0;
                fpsText = "FPS: " + ((int)(1f / Time.smoothDeltaTime)).ToString();
            }
        }

        private void OnGUI()
        {
            // FPS オーバーレイ (UI非表示時は隠す)
            if (!showFPS.Value || !GameActions.IsUIVisible) return;

            float scale = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float vw = Screen.width / scale;
            GUI.Box(new Rect(vw - 80, 5, 75, 24), "");
            GUI.Label(new Rect(vw - 76, 8, 70, 20), fpsText);
        }

        // KeyConfigWindow の設定タブから呼ばれる
        public void DrawTabContent()
        {
            GUILayout.Space(4);

            bool newShowFPS = GUILayout.Toggle(showFPS.Value, "  FPS 表示");
            if (newShowFPS != showFPS.Value)
                showFPS.Value = newShowFPS;

            bool newMute = GUILayout.Toggle(muteOnFocusLoss.Value, "  非アクティブ時にミュート");
            if (newMute != muteOnFocusLoss.Value)
            {
                muteOnFocusLoss.Value = newMute;
                // 設定を OFF にした場合は即座にミュート解除
                if (!newMute)
                    AudioListener.pause = false;
            }

            bool newHide = GUILayout.Toggle(hideMyAccessories.Value, "  自分のアクセサリーを非表示");
            if (newHide != hideMyAccessories.Value)
            {
                hideMyAccessories.Value = newHide;
                ApplyMyAccessoryVisibility(newHide);
            }

            bool newChatLabel = GUILayout.Toggle(showChatLabelWhenUIHidden.Value, "  UI非表示時もショートカットメッセージを表示");
            if (newChatLabel != showChatLabelWhenUIHidden.Value)
            {
                showChatLabelWhenUIHidden.Value = newChatLabel;
                IsShowChatLabelWhenUIHidden = newChatLabel;
            }
        }

        // ─── アクセサリー表示制御 ─────────────────────────────────────────

        // パッチからも呼べるよう static で公開
        public static bool IsHideMyAccessories { get; private set; }
        public static bool IsShowChatLabelWhenUIHidden { get; private set; }

        // Initialize 後に static フィールドを同期するため Start で呼ぶ
        private void Start()
        {
            IsHideMyAccessories        = hideMyAccessories.Value;
            IsShowChatLabelWhenUIHidden = showChatLabelWhenUIHidden.Value;
        }

        public static void ApplyMyAccessoryVisibility(bool hide)
        {
            IsHideMyAccessories = hide;

            var gm = SingletonMonoBehaviour<GameManager>.Instance;
            if (gm == null || gm.myPlayerObject == null) return;

            var ctrl = gm.myPlayerObject.GetComponent<FieldCharacterAccessoryController>();
            if (ctrl == null) return;

            foreach (var acc in ctrl.settedAccessories)
            {
                if (acc != null) acc.SetActive(!hide);
            }
        }
    }
}
