using System;
using System.Collections.ObjectModel;
using System.Reflection;
using BepInEx;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace LevelUpChoices
{
    public class ExperienceHook : MonoBehaviour
    {
        public static event Action<uint> OnLevelUp;

        private int MaxLevel => ModConfig.MaxLevel.Value;

        private ulong[] customExperienceTable;
        private uint customNaturalLevelCap;
        private ulong customHardExpCap;

        private int lastMaxLevel = -1;
        private bool lastEnableCustomLevelSystem = false;
        private bool lastEnableMonsterLevelScaling = false;

        private const double XP_BASE = 20.0;
        private const double XP_BUFFER = 100000.0;
        private const double SEARCH_LOW = 1.000001;
        private const double SEARCH_HIGH = 100.0;

        private static FieldInfo _teamExperienceField;
        private static FieldInfo _teamLevelsField;
        private static FieldInfo _teamCurrentExpField;
        private static FieldInfo _teamNextExpField;

        private void Awake()
        {
            CacheReflectionFields();

            ModConfig.EnableCustomLevelSystem.SettingChanged += OnSettingsChanged;
            ModConfig.EnableMonsterLevelScaling.SettingChanged += OnSettingsChanged;
            ModConfig.MaxLevel.SettingChanged += OnSettingsChanged;

            OnSettingsChanged(null, null);
        }

        private void CacheReflectionFields()
        {
            var tm = typeof(TeamManager);
            _teamExperienceField = tm.GetField("teamExperience", BindingFlags.NonPublic | BindingFlags.Instance);
            _teamLevelsField = tm.GetField("teamLevels", BindingFlags.NonPublic | BindingFlags.Instance);
            _teamCurrentExpField = tm.GetField("teamCurrentLevelExperience", BindingFlags.NonPublic | BindingFlags.Instance);
            _teamNextExpField = tm.GetField("teamNextLevelExperience", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private void OnDestroy()
        {
            On.RoR2.TeamManager.FindLevelForExperience -= TeamManager_FindLevelForExperience;
            On.RoR2.TeamManager.GetExperienceForLevel -= TeamManager_GetExperienceForLevel;
            On.RoR2.TeamManager.GiveTeamExperience -= TeamManager_GiveTeamExperience;
            On.RoR2.TeamManager.SetTeamExperience -= TeamManager_SetTeamExperience;
            On.RoR2.TeamManager.SetTeamLevel -= TeamManager_SetTeamLevel;

            On.RoR2.CharacterBody.RecalculateStats -= CharacterBody_RecalculateStats;
        }

        private void OnSettingsChanged(object sender, object args)
        {
            bool enableCustomLevelSystem = ModConfig.EnableCustomLevelSystem.Value;
            bool enableMonsterLevelScaling = ModConfig.EnableMonsterLevelScaling.Value;

            if (enableCustomLevelSystem)
            {
                if (!lastEnableCustomLevelSystem)
                {
                    On.RoR2.TeamManager.FindLevelForExperience += TeamManager_FindLevelForExperience;
                    On.RoR2.TeamManager.GetExperienceForLevel += TeamManager_GetExperienceForLevel;
                    On.RoR2.TeamManager.GiveTeamExperience += TeamManager_GiveTeamExperience;
                    On.RoR2.TeamManager.SetTeamExperience += TeamManager_SetTeamExperience;
                    On.RoR2.TeamManager.SetTeamLevel += TeamManager_SetTeamLevel;
                    Log.Info("Custom XP enabled.");
                }

                RebuildCustomTable();

                if (enableMonsterLevelScaling && !lastEnableMonsterLevelScaling)
                {
                    On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
                    Log.Info("Monster level scaling enabled.");
                }
                else if (!enableMonsterLevelScaling && lastEnableMonsterLevelScaling)
                {
                    On.RoR2.CharacterBody.RecalculateStats -= CharacterBody_RecalculateStats;
                    Log.Info("Monster level scaling disabled.");
                }
            }
            else
            {
                if (lastEnableCustomLevelSystem)
                {
                    On.RoR2.TeamManager.FindLevelForExperience -= TeamManager_FindLevelForExperience;
                    On.RoR2.TeamManager.GetExperienceForLevel -= TeamManager_GetExperienceForLevel;
                    On.RoR2.TeamManager.GiveTeamExperience -= TeamManager_GiveTeamExperience;
                    On.RoR2.TeamManager.SetTeamExperience -= TeamManager_SetTeamExperience;
                    On.RoR2.TeamManager.SetTeamLevel -= TeamManager_SetTeamLevel;

                    if (lastEnableMonsterLevelScaling)
                    {
                        On.RoR2.CharacterBody.RecalculateStats -= CharacterBody_RecalculateStats;
                    }

                    customExperienceTable = null;
                    Log.Info("Custom XP disabled.");
                }
            }

            lastEnableCustomLevelSystem = enableCustomLevelSystem;
            lastEnableMonsterLevelScaling = enableMonsterLevelScaling;
            lastMaxLevel = MaxLevel;
        }

        private void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);

            if (!Run.instance || !ModConfig.EnableMonsterLevelScaling.Value || !ModConfig.IsModEnabled)
                return;

            if (self.teamComponent == null)
                return;

            TeamIndex team = self.teamComponent.teamIndex;
            if (team == TeamIndex.Player)
                return;

            bool usesAmbient = (self.bodyFlags & CharacterBody.BodyFlags.UsesAmbientLevel) != 0;
            if (!usesAmbient)
                return;

            float scaleFactor = (float)MaxLevel / 94; // Vanilla max level is 94
            int ambientFloor = Run.instance.ambientLevelFloor;
            int scaledAmbient = Mathf.FloorToInt(ambientFloor * scaleFactor);
            scaledAmbient = Mathf.Min(scaledAmbient, MaxLevel);

            self.level = Mathf.Max(self.level, scaledAmbient);
        }

        private double CalculateOptimalMultiplier(uint maxLevel)
        {
            if (maxLevel < 2)
                return 1.0;

            double target = (double)ulong.MaxValue - XP_BUFFER;
            double low = SEARCH_LOW;
            double high = SEARCH_HIGH;
            double mid = 0;

            for (int i = 0; i < 64; i++)
            {
                mid = (low + high) / 2.0;
                double sum = XP_BASE * (Math.Pow(mid, maxLevel - 1) - 1.0) / (mid - 1.0);
                if (sum < target)
                    low = mid;
                else
                    high = mid;
            }

            return mid;
        }

        private void RebuildCustomTable()
        {
            int targetMaxLevel = MaxLevel;

            if (targetMaxLevel < 2)
            {
                Log.Error("MaxLevel must be at least 2. Defaulting to 256.");
                targetMaxLevel = 256;
            }

            double multiplier = CalculateOptimalMultiplier((uint)targetMaxLevel);
            double cumulative = 0.0;
            double stepMultiplier = 1.0;

            var list = new System.Collections.Generic.List<ulong> { 0uL, 0uL };
            bool maxed = false;

            for (uint lvl = 2; lvl <= targetMaxLevel; lvl++)
            {
                ulong xp;

                if (maxed)
                {
                    xp = ulong.MaxValue;
                }
                else
                {
                    double step = XP_BASE * stepMultiplier;
                    stepMultiplier *= multiplier;
                    cumulative += step;

                    if (cumulative >= (double)ulong.MaxValue)
                    {
                        cumulative = (double)ulong.MaxValue;
                        xp = ulong.MaxValue;
                        maxed = true;
                    }
                    else if (cumulative < 0.0)
                    {
                        xp = 0;
                    }
                    else
                    {
                        xp = (ulong)cumulative;
                    }

                    if (list.Count > 1 && xp <= list[^1] && xp != ulong.MaxValue)
                    {
                        xp = ulong.MaxValue;
                        maxed = true;
                    }
                }

                list.Add(xp);
            }

            customExperienceTable = [.. list];
            customNaturalLevelCap = (uint)(customExperienceTable.Length - 1);
            customHardExpCap = customExperienceTable[^1];

            Log.Info($"XP table rebuilt. Max: {customNaturalLevelCap}, Cap: {customHardExpCap}, R: {multiplier:F4}");
        }

        private uint TeamManager_FindLevelForExperience(On.RoR2.TeamManager.orig_FindLevelForExperience orig, ulong exp)
        {
            if (!ModConfig.IsModEnabled)
                return orig(exp);
            if (customExperienceTable == null || customExperienceTable.Length == 0)
                return orig(exp);

            for (uint i = 1; i < customExperienceTable.Length; i++)
            {
                if (customExperienceTable[i] > exp)
                    return i - 1;
            }
            return customNaturalLevelCap;
        }

        private ulong TeamManager_GetExperienceForLevel(On.RoR2.TeamManager.orig_GetExperienceForLevel orig, uint level)
        {
            if (!ModConfig.IsModEnabled)
                return orig(level);
            if (customExperienceTable == null || customExperienceTable.Length == 0)
                return orig(level);

            if (level >= customExperienceTable.Length)
                level = customNaturalLevelCap;

            return customExperienceTable[level];
        }

        private void TeamManager_GiveTeamExperience(On.RoR2.TeamManager.orig_GiveTeamExperience orig, TeamManager self, TeamIndex teamIndex, ulong exp)
        {
            if (!NetworkServer.active)
                return;

            if (!ModConfig.IsModEnabled)
            {
                orig(self, teamIndex, exp);
                return;
            }

            ulong current = self.GetTeamExperience(teamIndex);
            ulong total = current + exp;
            if (total < current)
                total = ulong.MaxValue;

            self.SetTeamExperience(teamIndex, total);

            if (teamIndex != TeamIndex.Player)
                return;

            var members = TeamComponent.GetTeamMembers(teamIndex);
            for (int i = 0; i < members.Count; i++)
            {
                var body = members[i].GetComponent<CharacterBody>();
                if (body?.master)
                    body.master.TrackBeadExperience(exp);
            }
        }

        private void TeamManager_SetTeamLevel(On.RoR2.TeamManager.orig_SetTeamLevel orig, TeamManager self, TeamIndex teamIndex, uint newLevel)
        {
            if (!ModConfig.IsModEnabled)
            {
                orig(self, teamIndex, newLevel);
                return;
            }

            if (teamIndex >= TeamIndex.Neutral && teamIndex < TeamIndex.Count && self.GetTeamLevel(teamIndex) != newLevel)
            {
                self.SetTeamExperience(teamIndex, TeamManager_GetExperienceForLevel(null, newLevel));
            }
        }

        private void TeamManager_SetTeamExperience(On.RoR2.TeamManager.orig_SetTeamExperience orig, TeamManager self, TeamIndex teamIndex, ulong exp)
        {
            if (!ModConfig.IsModEnabled)
            {
                orig(self, teamIndex, exp);
                return;
            }

            if (exp > customHardExpCap)
                exp = customHardExpCap;

            var expArr = (ulong[])_teamExperienceField?.GetValue(self);
            if (expArr != null)
                expArr[(int)teamIndex] = exp;

            uint oldLvl = self.GetTeamLevel(teamIndex);
            uint newLvl = TeamManager_FindLevelForExperience(null, exp);

            if (oldLvl != newLvl)
            {
                var members = TeamComponent.GetTeamMembers(teamIndex);
                for (int i = 0; i < members.Count; i++)
                {
                    members[i].GetComponent<CharacterBody>()?.OnTeamLevelChanged();
                }

                var levelsArr = (uint[])_teamLevelsField?.GetValue(self);
                if (levelsArr != null)
                    levelsArr[(int)teamIndex] = newLvl;

                var currExpArr = (ulong[])_teamCurrentExpField?.GetValue(self);
                if (currExpArr != null)
                    currExpArr[(int)teamIndex] = TeamManager_GetExperienceForLevel(null, newLvl);

                var nextExpArr = (ulong[])_teamNextExpField?.GetValue(self);
                if (nextExpArr != null)
                    nextExpArr[(int)teamIndex] = TeamManager_GetExperienceForLevel(null, newLvl + 1);

                if (oldLvl < newLvl)
                {
                    GlobalEventManager.OnTeamLevelUp(teamIndex);

                    if (teamIndex == TeamIndex.Player)
                    {
                        for (uint l = oldLvl + 1; l <= newLvl; l++)
                        {
                            OnLevelUp?.Invoke(l);
                        }
                    }
                }
            }

            if (NetworkServer.active)
            {
                self.SetDirtyBit((uint)(1 << (int)teamIndex));
            }
        }
    }
}
