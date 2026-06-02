using HarmonyLib;
using UnityEngine;

namespace ConfigMod
{
    // SuteageItemController.Update の Alpha1/2/3 直接チェックを
    // ConfigModInput 経由に差し替えるパッチ。
    // ConfigModInput は KeyCode と XInput (Xbox 十字キー等) の両方をサポートする。
    [HarmonyPatch(typeof(SuteageItemController), "Update")]
    public static class SuteageItemController_Update_Patch
    {
        static bool Prefix(SuteageItemController __instance)
        {
            var gm = SingletonMonoBehaviour<GameManager>.Instance;
            if (gm == null) return true;

            bool focused = gm.isForcusdInputField;

            if (ConfigModInput.GetDown("SuteageItem1") && !focused)
            {
                if ((bool)__instance.controller.playerData.stockSuteageItemDataList[0])
                    __instance.UseSuteageItem(__instance.controller.playerData.stockSuteageItemDataList[0]);
                else
                    gm.ShowSystemMessage("使用可能なアイテムがありません");
            }
            if (ConfigModInput.GetDown("SuteageItem2") && !focused)
            {
                if ((bool)__instance.controller.playerData.stockSuteageItemDataList[1])
                    __instance.UseSuteageItem(__instance.controller.playerData.stockSuteageItemDataList[1]);
                else
                    gm.ShowSystemMessage("使用可能なアイテムがありません");
            }
            if (ConfigModInput.GetDown("SuteageItem3") && !focused)
            {
                if ((bool)__instance.controller.playerData.stockSuteageItemDataList[2])
                    __instance.UseSuteageItem(__instance.controller.playerData.stockSuteageItemDataList[2]);
                else
                    gm.ShowSystemMessage("使用可能なアイテムがありません");
            }
            return false;
        }
    }
}
