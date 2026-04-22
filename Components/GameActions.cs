using System.Collections;
using System.IO;
using MBakuon;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BakuonConfigMod
{
    // BakuonConfigMod が管理するゲームアクション (メニュー開閉など) を毎フレーム処理するコンポーネント。
    // DontDestroyOnLoad な GameObject にアタッチされる。
    public class GameActions : MonoBehaviour
    {
        public static bool IsUIVisible { get; private set; } = true;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // シーン移動時に UI 非表示状態が残っていたら自動復元する。
            // SwitchInterfaceVisible(false) が復元されないまま残るケース対策。
            if (!IsUIVisible)
            {
                IsUIVisible = true;
                LogHelper.LogInfo("[GameActions] シーンロード時に UI 表示を自動復元しました");
            }
        }

        private void Update()
        {
            ConfigModInput.UpdateState();

            // リバインド中は誤作動を防ぐためスキップ
            if (ConfigModInput.IsRebinding) return;

            HandleScreenshot();
            HandleUIToggle();
            HandleMenuToggle();
        }

        // ─── スクリーンショット ───────────────────────────────────────────

        private void HandleScreenshot()
        {
            if (!ConfigModInput.GetDown("Screenshot")) return;
            StartCoroutine(CaptureScreenshot());
        }

        private static IEnumerator CaptureScreenshot()
        {
            // フレームの描画が完全に終わってからキャプチャする
            yield return new WaitForEndOfFrame();

            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Screenshots"));
            Directory.CreateDirectory(dir);

            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename  = "screenshot_" + timestamp + ".png";
            string fullPath  = Path.Combine(dir, filename);

            Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            Object.Destroy(tex);

            LogHelper.LogInfo("Screenshot saved: " + fullPath);

            var gm = SingletonMonoBehaviour<GameManager>.Instance;
            if (gm != null)
                gm.ShowSystemMessage(filename + " を保存しました");
        }

        // ─── UI 表示/非表示 ──────────────────────────────────────────────

        private void HandleUIToggle()
        {
            if (!ConfigModInput.GetDown("UIToggle")) return;
            ToggleUIVisible();
        }

        public static void ToggleUIVisible()
        {
            var gm = SingletonMonoBehaviour<GameManager>.Instance;
            if (gm == null) return;
            if (gm.isForcusdInputField) return;
            if (!SingletonMonoBehaviour<MenuScreenManager>.Instance) return;

            IsUIVisible = !IsUIVisible;
            ApplyUIVisible(IsUIVisible);
        }

        public static void SetUIVisible(bool visible)
        {
            var gm = SingletonMonoBehaviour<GameManager>.Instance;
            if (gm == null) return;
            if (!SingletonMonoBehaviour<MenuScreenManager>.Instance) return;

            IsUIVisible = visible;
            ApplyUIVisible(visible);
        }

        private static void ApplyUIVisible(bool visible)
        {
            var gm = SingletonMonoBehaviour<GameManager>.Instance;
            gm.SwitchInterfaceVisible(visible);

            // SwitchInterfaceVisible は textLogCanvas を操作しないため明示的に制御する。
            var chatInputManager = SingletonMonoBehaviour<ChatInputManager>.Instance;
            if (chatInputManager != null && chatInputManager.textLogCanvas != null)
                chatInputManager.textLogCanvas.gameObject.SetActive(visible);
        }

        // ─── メニュー開閉 ────────────────────────────────────────────────

        private static void HandleMenuToggle()
        {
            if (!ConfigModInput.GetDown("MenuToggle")) return;

            var gm = SingletonMonoBehaviour<GameManager>.Instance;
            if (gm != null && gm.isForcusdInputField) return;

            // ダンジョン中は SuteageIsekiController の専用メニューを使う
            var suteage = Object.FindObjectOfType<SuteageIsekiController>();
            if (suteage != null)
            {
                suteage.PressedMenuOpenHandle();
                return;
            }

            var menu = SingletonMonoBehaviour<MenuScreenManager>.Instance;
            if (menu == null) return;
            menu.PressedMenuButton();
        }
    }
}
