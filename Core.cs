using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using UnityEngine;

namespace BakuonConfigMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.bakuon.offlinepatch")]
    public class ConfigModPlugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        private Harmony harmony;

        private void Awake()
        {
            Logger = base.Logger;

            // ConfigMod 管理アクションを登録 (XInput D-pad 対応のため ConfigurableInput ではなく独自管理)
            ConfigModInput.RegisterAction("Screenshot",   new InputBinding(KeyCode.None),   Config, "スクリーンショット");
            ConfigModInput.RegisterAction("UIToggle",     new InputBinding(KeyCode.None),   Config, "UI表示切替");
            ConfigModInput.RegisterAction("MenuToggle",   new InputBinding(KeyCode.None),   Config, "メニュー開閉");
            ConfigModInput.RegisterAction("SuteageItem1", new InputBinding(KeyCode.Alpha1), Config, "アイテム1使用");
            ConfigModInput.RegisterAction("SuteageItem2", new InputBinding(KeyCode.Alpha2), Config, "アイテム2使用");
            ConfigModInput.RegisterAction("SuteageItem3", new InputBinding(KeyCode.Alpha3), Config, "アイテム3使用");

            // 永続 GameObject にコンポーネントをアタッチ
            var go = new GameObject("BakuonConfigMod");
            DontDestroyOnLoad(go);
            go.AddComponent<GameActions>();
            var settings = go.AddComponent<SettingsWindow>();
            settings.Initialize(Config);
            var vis = go.AddComponent<UIVisibilityWindow>();
            vis.Initialize(Config);
            go.AddComponent<RingWindow>();
            go.AddComponent<ItemWindow>();
            go.AddComponent<AccessoryWindow>();
            go.AddComponent<RankWindow>();
            go.AddComponent<GameModeWindow>();
            var window = go.AddComponent<ConfigModWindow>();
            window.Initialize(Config);

            harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            Logger.LogInfo($"BakuonConfigMod v{PluginInfo.PLUGIN_VERSION} loaded");
        }

        private void OnDestroy()
        {
            if (harmony != null)
            {
                harmony.UnpatchSelf();
            }
        }
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.bakuon.configmod";
        public const string PLUGIN_NAME = "BakuonConfigMod";
        public const string PLUGIN_VERSION = "1.0.0";
    }

    public static class LogHelper
    {
        public static void LogInfo(string message)
        {
            ConfigModPlugin.Logger.LogInfo($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }

        public static void LogWarning(string message)
        {
            ConfigModPlugin.Logger.LogWarning($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }

        public static void LogError(string message)
        {
            ConfigModPlugin.Logger.LogError($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }
    }
}
