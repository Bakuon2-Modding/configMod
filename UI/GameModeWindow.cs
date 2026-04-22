using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BakuonConfigMod
{
    public class GameModeWindow : MonoBehaviour
    {
        private string statusMessage = "";
        private float statusMessageTimer = 0f;

        private enum LoadType { Direct, System }

        private struct ModeEntry
        {
            public string displayName;
            public string sceneName;
            public string gameName;
            public LoadType loadType;

            public ModeEntry(string display, string scene, string game, LoadType type)
            {
                displayName = display;
                sceneName = scene;
                gameName = game;
                loadType = type;
            }
        }

        private static readonly ModeEntry[] Modes = new ModeEntry[]
        {
            new ModeEntry("国取り戦", "WarCarry_1",  "国取り戦", LoadType.System),
            new ModeEntry("防衛戦",   "DefenseGame", "防衛戦",   LoadType.Direct),
        };

        private void Update()
        {
            if (statusMessageTimer > 0f)
            {
                statusMessageTimer -= Time.deltaTime;
                if (statusMessageTimer <= 0f)
                    statusMessage = "";
            }
        }

        public void DrawTabContent()
        {
            GUILayout.Space(4);

            var gm = SingletonMonoBehaviour<GameManager>.Instance;
            if (gm == null)
            {
                GUILayout.Label("ゲーム未ロード中。フィールドに入ってから使用してください。");
                return;
            }

            GUILayout.Label("<b>--- シーン移動 ---</b>");
            GUILayout.Space(2);
            foreach (var mode in Modes)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(mode.displayName, GUILayout.Width(140));
                if (GUILayout.Button("開始", GUILayout.Width(60)))
                    LoadGameMode(mode);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);
            GUILayout.Label("<size=11><color=#888888>※ 未公開コンテンツは正常に動作しない場合があります。</color></size>");

            if (!string.IsNullOrEmpty(statusMessage))
            {
                GUILayout.Space(4);
                GUILayout.Label(statusMessage);
            }
        }

        private void LoadGameMode(ModeEntry mode)
        {
            var gm = SingletonMonoBehaviour<GameManager>.Instance;
            if (gm == null) return;

            LogHelper.LogInfo($"[GameMode] Loading: {mode.displayName} ({mode.sceneName})");
            gm.matchingRoomData.gameName = mode.gameName;
            gm.matchingRoomNextLoadSceneName = mode.sceneName;

            string sceneName = mode.sceneName;
            LoadType loadType = mode.loadType;

            gm.FadeOutEffect(delegate
            {
                try
                {
                    if (loadType == LoadType.System)
                    {
                        SceneManager.LoadScene(sceneName + "_System");
                        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
                    }
                    else
                    {
                        SceneManager.LoadScene(sceneName);
                    }
                    SceneManager.LoadScene("BaseSystemScene", LoadSceneMode.Additive);
                }
                catch (Exception ex)
                {
                    LogHelper.LogError($"[GameMode] シーンロードエラー: {ex}");
                }
            });
        }

        private void ShowStatus(string message)
        {
            statusMessage = message;
            statusMessageTimer = 5f;
        }
    }
}
