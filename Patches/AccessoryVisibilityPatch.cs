using HarmonyLib;

namespace ConfigMod
{
    // フィールド入場時に自分のアクセサリーが syncMyAccessory() で生成された直後、
    // 「自分のアクセサリーを非表示」設定を適用する。
    [HarmonyPatch(typeof(FieldCharacterAccessoryController), "syncMyAccessory")]
    public static class FieldCharacterAccessoryController_syncMyAccessory_Patch
    {
        static void Postfix()
        {
            if (SettingsWindow.IsHideMyAccessories)
                SettingsWindow.ApplyMyAccessoryVisibility(true);
        }
    }
}
