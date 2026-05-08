using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using KillMeForMyPower.Restrictions.BossNameManagement;
using UnityEngine;

namespace KillMeForMyPower.Restrictions
{
    [HarmonyPatch(typeof(Character), "OnDeath")]
    public class RegisterBossDefeatPatch
    {

        // This will be executed in the player that hits last when the boss dies
        public static void Postfix(Character __instance)
        {
            if (__instance == null || !__instance.IsBoss())
                return;

            string bossName = __instance.name.Replace("(Clone)", "");

            List<string> playersToGrant = new List<string>();

            // En dedicado no existe Player.m_localPlayer.
            // Detectamos jugadores desde la posición del boss.
            Character boss = __instance;
            BaseAI ai = boss.GetComponent<BaseAI>();
            float aggroRange = ConfigurationFile.bossRewardDetectionRange.Value;

            Logger.LogInfo($"Detection boss range for {boss.name.Replace("(Clone)", "")} is {aggroRange} meters");

            Vector3 bossPosition = boss.transform.position;

            List<Player> nearbyPlayers = Player.GetAllPlayers();

            foreach (Player player in nearbyPlayers)
            {
                if (player == null)
                    continue;

                if (Vector3.Distance(player.transform.position, bossPosition) <= aggroRange)
                {
                    string playerName = player.GetPlayerName();

                    if (!string.IsNullOrEmpty(playerName) && !playersToGrant.Contains(playerName))
                        playersToGrant.Add(playerName);
                }
            }

            if (playersToGrant.Count == 0)
            {
                Logger.LogWarning($"No nearby players found for boss kill grant: {bossName}");
                return;
            }

            Logger.LogInfo($"Granting {bossName} kill to: {string.Join(", ", playersToGrant)}");

            ZRoutedRpc.instance.InvokeRoutedRPC(
                0L,
                "RPC_BossPowerGrantServer",
                bossName,
                string.Join(",", playersToGrant)
            );
        }
    }

    [HarmonyPatch(typeof(Player), "Save")]
    public class Player_Save_Null_Key_Clean_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(Player __instance)
        {
            var invalidKeys = __instance.m_customData.Keys
                .Where(k => string.IsNullOrEmpty(k) || k == "null")
                .ToList();

            foreach (var key in invalidKeys)
            {
                Logger.LogWarning($"Removing null key from player {__instance.GetPlayerName()}");
                __instance.RemoveUniqueKey(key);
            }
        }
    }

    public class RPC_BossPowerGrantCalls
    {
        public static void RPC_BossPowerGrantServer(long sender, string bossEnumStr, string playersToGrant)
        {
            //Message to the host to sync powers list
            BossNameEnum bossNameEnum = KillMeForMyPowerUtils.findBossNameByPrefabName(bossEnumStr, true);
            if (bossNameEnum == BossNameEnum.None) return;
            
            Logger.Log($"[RPC_BossPowerGrantServer] RPC sent from sender {sender} with {bossEnumStr} ({bossNameEnum}) and {playersToGrant}");
            string[] players = playersToGrant.Split(',');
            foreach (string player in players)
            {
                BossNameUtils.GrantBossPowerToPlayer(bossNameEnum, player, true);
                //TODO RPC para avisar al player
            }
        }
        
        public static void RPC_BossPowerRemoveGrantServer(long sender, string bossEnumStr, string playersToRemoveGrant)
        {
            //Message to the host to sync powers list
            BossNameEnum bossNameEnum = KillMeForMyPowerUtils.findBossNameByPrefabName(bossEnumStr);
            if (bossNameEnum == BossNameEnum.None) return;
            
            Logger.Log($"[RPC_BossPowerRemoveGrantServer] RPC sent from sender {sender} with {bossEnumStr} ({bossNameEnum}) and {playersToRemoveGrant}");
            string[] players = playersToRemoveGrant.Split(',');
            foreach (string player in players)
            {
                BossNameUtils.GrantBossPowerToPlayer(bossNameEnum, player, false);
                //TODO RPC para avisar al player
            }
        }
    }
}
