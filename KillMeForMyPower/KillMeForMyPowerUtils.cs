using System;
using KillMeForMyPower.Managers;
using KillMeForMyPower.Restrictions.BossNameManagement;

namespace KillMeForMyPower
{
    public class KillMeForMyPowerUtils
    {
        private static BossNameEnum parseBossName(string value) {
            Logger.Log("Parsing value: " + value);
            return (BossNameEnum) Enum.Parse(typeof(BossNameEnum), value, true);
        }
        
        public static BossNameEnum findBossNameByPrefabName(string value, bool logSilent = false) {
            Logger.Log($"Parsing prefab bossName {value} into Enum");
            
            foreach (BossNameEnum bossNameEnum in Enum.GetValues(typeof(BossNameEnum)))
            {
                if (bossNameEnum.GetFightBossname() == value || bossNameEnum.ToString() == value)
                    return bossNameEnum;
            }

            if (!logSilent)
                Logger.LogWarning($"{value} not a vanilla/monstrum boss name.");
            return BossNameEnum.None;
        }

        public static string getBossNameTranslation(BossNameEnum bossName)
        {
            return bossName.GetTranslationKey();
        }

        public static bool HasDefeatedBossNameStr(string bossName)
        {
            BossNameEnum parsedEnum;
            try
            {
                parsedEnum = parseBossName(bossName);
            }
            catch (Exception e)
            {
                Logger.LogError("Error in HasDefeatedBossName with " + bossName + " in parseBossName.\n" + e);
                return false;
            }

            return HasDefeatedBossName(parsedEnum);
        }

        public static bool HasDefeatedBossNameStr(string bossName, Player player)
        {
            BossNameEnum parsedEnum;
            try
            {
                parsedEnum = parseBossName(bossName);
            }
            catch (Exception e)
            {
                Logger.LogError("Error in HasDefeatedBossName with " + bossName + " in parseBossName.\n" + e);
                return false;
            }

            return HasDefeatedBossName(parsedEnum, player);
        }

        public static bool HasDefeatedBossName(BossNameEnum bossNameEnum, Player player)
        {
            if (player == null)
                return false;

            bool hasDefeated = BossNameUtils.IsBossPowerGrantedForPlayer(bossNameEnum, player);
            Logger.Log($"hasDefeated(1) for player {player.GetPlayerName()}: {hasDefeated}");

            if (!hasDefeated)
            {
                hasDefeated = ConfigurationFile.activateMidPlayDetection.Value &&
                              player.HaveUniqueKey(bossNameEnum.GetPowerKey());

                Logger.Log($"hasDefeated(2) for player {player.GetPlayerName()}: {hasDefeated}");

                if (hasDefeated)
                {
                    ZRoutedRpc.instance.InvokeRoutedRPC(
                        0L,
                        "RPC_BossPowerGrantServer",
                        bossNameEnum.ToString(),
                        player.GetPlayerName()
                    );
                }
            }

            return hasDefeated;
        }

        public static bool HasDefeatedBossName(BossNameEnum bossNameEnum)
        {
            return HasDefeatedBossName(bossNameEnum, Player.m_localPlayer);
        }

        public static int GetCurrentDay()
        {
            return (int) GameManager.GetPrivateMethod(EnvMan.instance, "GetCurrentDay");
        }

        public static int GetBossMinimumDayForPower(string bossName)
        {
            var parsedEnum = parseBossName(bossName);
            if (parsedEnum == BossNameEnum.Eikthyr)
                return ConfigurationFile.daysBossEikthyr.Value;
            if (parsedEnum == BossNameEnum.TheElder)
                return ConfigurationFile.daysBossElder.Value;
            if (parsedEnum == BossNameEnum.Bonemass)
                return ConfigurationFile.daysBossBonemass.Value;
            if (parsedEnum == BossNameEnum.Moder)
                return ConfigurationFile.daysBossModer.Value;
            if (parsedEnum == BossNameEnum.Yagluth)
                return ConfigurationFile.daysBossYagluth.Value;
            if (parsedEnum == BossNameEnum.Queen)
                return ConfigurationFile.daysBossQueen.Value;
            if (parsedEnum == BossNameEnum.Fader)
                return ConfigurationFile.daysBossFader.Value;
            return ConfigurationFile.daysBossModded.Value;
        }

        public static bool bossIsKilled(string bossToCheck)
        {
            return bossIsKilled(parseBossName(bossToCheck));
        }

        public static bool bossIsKilled(BossNameEnum bossToCheck)
        {
            return HasDefeatedBossName(bossToCheck);
        }
    }
}
