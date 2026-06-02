using HarmonyLib;

namespace ConfigMod
{
    // UI 非表示時 (SwitchAllCanvasVisible(false)) でも
    // chatLabelCanvas (頭上ショートカットメッセージ) だけは表示し続けるパッチ。
    [HarmonyPatch(typeof(FieldUIScreenManager), "SwitchAllCanvasVisible")]
    public static class FieldUIScreenManager_SwitchAllCanvasVisible_Patch
    {
        static void Postfix(FieldUIScreenManager __instance, bool _isVisible)
        {
            if (!_isVisible && SettingsWindow.IsShowChatLabelWhenUIHidden)
                __instance.chatLabelCanvas.SetActive(true);
        }
    }
}
