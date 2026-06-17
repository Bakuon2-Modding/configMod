using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MBakuon;

namespace ConfigMod
{
    // ──────────────────────────────────────────────────────────────────────
    // 仮想人数による PvE 難易度スケーリング
    //
    // 各 PvE コンテンツ（遺跡 / ボス=アトラクション / 協力ゲーム / 防衛戦）の難易度は、
    // ゲーム側が PhotonNetwork.room.PlayerCount を読んで決めている:
    //   ・敵 HP / シールド / スコア : num = 0.5f + PlayerCount * 0.5f を乗算
    //   ・遺跡クリアゲージ          : 1f + (PlayerCount - 1) * 0.5f を乗算
    //   ・敵の同時出現数 / スポーン数 : Random.Range(1, PlayerCount(+1))
    //
    // この機能は【ソロプレイ専用】。上記メソッド内の PlayerCount 読み取り値だけを仮想人数に
    // 底上げし、PlayerCount 自体は書き換えない（マッチングUI・プレイヤーリスト等は実人数のまま）。
    // マルチプレイ（実プレイヤー2人以上）では一切適用せず、ゲーム本来の人数スケールに任せる。
    // ──────────────────────────────────────────────────────────────────────
    public static class DifficultyScaling
    {
        // SettingsWindow / チャットコマンドから同期される実効値。1 = 無効（バニラ動作）。
        public static int VirtualPlayerCount = 1;

        // Transpiler が PlayerCount getter 直後に差し込む変換関数。
        // ソロ（実人数1以下）のときだけ仮想人数を返す。
        // マルチプレイ（実人数2以上）では実人数をそのまま使う＝この機能を無効化する。
        public static int Adjust(int actualPlayerCount)
        {
            int v = VirtualPlayerCount;
            if (v <= 1 || actualPlayerCount >= 2) return actualPlayerCount;
            return v;
        }
    }

    // PhotonNetwork.room.PlayerCount の getter 呼び出し直後に
    // DifficultyScaling.Adjust(int) を差し込む共通 Transpiler。
    // 対象メソッドは難易度計算メソッドのみに限定する（UI/マッチング系には適用しない）。
    internal static class PlayerCountBoostTranspiler
    {
        private static readonly MethodInfo AdjustMethod =
            AccessTools.Method(typeof(DifficultyScaling), nameof(DifficultyScaling.Adjust));

        public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            int patched = 0;
            foreach (var ins in instructions)
            {
                yield return ins;

                // Photon の Room.PlayerCount getter 呼び出しを検出。
                // Room 型を直接参照せず、メソッド名と宣言型名で判定する（Photon DLL 依存を避ける）。
                // PUN のバージョンにより getter が Room / RoomInfo どちらで宣言されるか異なるため両対応。
                if ((ins.opcode == OpCodes.Call || ins.opcode == OpCodes.Callvirt)
                    && ins.operand is MethodInfo m
                    && m.Name == "get_PlayerCount"
                    && m.DeclaringType != null
                    && m.DeclaringType.Name.Contains("Room"))
                {
                    yield return new CodeInstruction(OpCodes.Call, AdjustMethod);
                    patched++;
                }
            }

            if (patched == 0)
                LogHelper.LogWarning("[DifficultyScaling] PlayerCount 参照が見つかりませんでした（ゲーム更新で実装が変わった可能性）");
        }
    }

    // ─── 適用対象メソッド（全 PvE コンテンツ）─────────────────────────────

    // 遺跡: 敵の同時出現数・1スポーンあたりの体数（2 + 人数 / 人数 * 4 / Random(1, 人数+1)）
    [HarmonyPatch(typeof(SuteageIsekiState_Maingame), "ProcessFixedUpdate")]
    public static class SuteageIsekiState_Maingame_ProcessFixedUpdate_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            => PlayerCountBoostTranspiler.Transpile(instructions);
    }

    // 遺跡: マルチプレイ時のクリア必要敵ゲージ（1 + (人数-1) * 0.5）
    [HarmonyPatch(typeof(SuteageIsekiController), "CheckGameClear")]
    public static class SuteageIsekiController_CheckGameClear_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            => PlayerCountBoostTranspiler.Transpile(instructions);
    }

    // ボス（アトラクション）: 敵 HP / シールド HP（0.5 + 人数 * 0.5）
    [HarmonyPatch(typeof(BossBattleController), "GenerateMobOnlyMasterClient")]
    public static class BossBattleController_GenerateMobOnlyMasterClient_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            => PlayerCountBoostTranspiler.Transpile(instructions);
    }

    // 協力ゲーム: ボール生成数（Random(1, 人数)）
    [HarmonyPatch(typeof(CoopGameController), "GenerateBall")]
    public static class CoopGameController_GenerateBall_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            => PlayerCountBoostTranspiler.Transpile(instructions);
    }

    // 協力ゲーム: 敵スポーン数 + 強化敵の HP/スコア（0.5 + 人数 * 0.5）
    [HarmonyPatch(typeof(CoopGameController), "GenerateMob")]
    public static class CoopGameController_GenerateMob_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            => PlayerCountBoostTranspiler.Transpile(instructions);
    }

    // 防衛戦: 強制生成敵の HP/スコア（0.5 + 人数 * 0.5）
    [HarmonyPatch(typeof(DefenseGameController), "ForceGenerateMob")]
    public static class DefenseGameController_ForceGenerateMob_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            => PlayerCountBoostTranspiler.Transpile(instructions);
    }

    // 防衛戦: 敵スポーン数 + 強化敵の HP/スコア（Random(1, 人数) / 0.5 + 人数 * 0.5）
    [HarmonyPatch(typeof(DefenseGameController), "GenerateMob")]
    public static class DefenseGameController_GenerateMob_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            => PlayerCountBoostTranspiler.Transpile(instructions);
    }
}
