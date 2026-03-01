using System;
using System.Collections.ObjectModel;
using System.Reflection;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace LevelUpChoices.Hooks {
    public class ExperienceHook : MonoBehaviour {
        public static event Action<uint> OnLevelUp;

        private static int MaxLevel => ModConfig.MaxLevelValue;

        private ulong[] _customExperienceTable;
        private uint _customNaturalLevelCap;
        private ulong _customHardExpCap;

        private int _lastMaxLevel = -1;
        private bool _lastEnableCustomLevelSystem = false;
        private bool _lastEnableMonsterLevelScaling = false;

        private const double XP_BASE = 20.0;
        private const double XP_BUFFER = 100000.0;
        private const double SEARCH_LOW = 1.000001;
        private const double SEARCH_HIGH = 100.0;

        private static FieldInfo s_teamExperienceField;
        private static FieldInfo s_teamLevelsField;
        private static FieldInfo s_teamCurrentExpField;
        private static FieldInfo s_teamNextExpField;

        private void Awake() {
            CacheReflectionFields();

            ModConfig.EnableCustomLevelSystem.SettingChanged += OnSettingsChanged;
            ModConfig.EnableMonsterLevelScaling.SettingChanged += OnSettingsChanged;
            ModConfig.MaxLevel.SettingChanged += OnSettingsChanged;
            ModConfig.OnServerConfigSynced += OnServerConfigSynced;

            Run.onRunStartGlobal += OnRunStartGlobal;
            Run.onRunDestroyGlobal += OnRunDestroyGlobal;

            OnSettingsChanged(null, null);
        }

        private void OnServerConfigSynced() {
            OnSettingsChanged(null, null);
        }

        private static void SyncConfigAsServer() {
            if (NetworkServer.active) {
                new Networking.SyncConfig(ModConfig.MaxLevel.Value, ModConfig.EnableMonsterLevelScaling.Value, ModConfig.EnableCustomLevelSystem.Value).Send(R2API.Networking.NetworkDestination.Clients);
            }
        }
        private void OnRunStartGlobal(Run run) {
            SyncConfigAsServer();
        }

        private void OnRunDestroyGlobal(Run run) {
            ModConfig.ResetServerConfig();
            OnSettingsChanged(null, null);
        }

        private static void CacheReflectionFields() {
            Type tm = typeof(TeamManager);
            s_teamExperienceField = tm.GetField("teamExperience", BindingFlags.NonPublic | BindingFlags.Instance);
            s_teamLevelsField = tm.GetField("teamLevels", BindingFlags.NonPublic | BindingFlags.Instance);
            s_teamCurrentExpField = tm.GetField("teamCurrentLevelExperience", BindingFlags.NonPublic | BindingFlags.Instance);
            s_teamNextExpField = tm.GetField("teamNextLevelExperience", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private void OnDestroy() {
            On.RoR2.TeamManager.FindLevelForExperience -= TeamManager_FindLevelForExperience;
            On.RoR2.TeamManager.GetExperienceForLevel -= TeamManager_GetExperienceForLevel;
            On.RoR2.TeamManager.GiveTeamExperience -= TeamManager_GiveTeamExperience;
            On.RoR2.TeamManager.SetTeamExperience -= TeamManager_SetTeamExperience;
            On.RoR2.TeamManager.SetTeamLevel -= TeamManager_SetTeamLevel;

            On.RoR2.CharacterBody.RecalculateStats -= CharacterBody_RecalculateStats;
            On.RoR2.UI.AmbientLevelDisplay.Update -= AmbientLevelDisplay_Update;

            ModConfig.EnableCustomLevelSystem.SettingChanged -= OnSettingsChanged;
            ModConfig.EnableMonsterLevelScaling.SettingChanged -= OnSettingsChanged;
            ModConfig.MaxLevel.SettingChanged -= OnSettingsChanged;
            ModConfig.OnServerConfigSynced -= OnServerConfigSynced;

            Run.onRunStartGlobal -= OnRunStartGlobal;
            Run.onRunDestroyGlobal -= OnRunDestroyGlobal;
        }

        private void OnSettingsChanged(object sender, object args) {
            bool enableCustomLevelSystem = ModConfig.EnableCustomLevelSystemValue;
            bool enableMonsterLevelScaling = ModConfig.EnableMonsterLevelScalingValue;

            if (enableCustomLevelSystem) {
                if (!_lastEnableCustomLevelSystem) {
                    On.RoR2.TeamManager.FindLevelForExperience += TeamManager_FindLevelForExperience;
                    On.RoR2.TeamManager.GetExperienceForLevel += TeamManager_GetExperienceForLevel;
                    On.RoR2.TeamManager.GiveTeamExperience += TeamManager_GiveTeamExperience;
                    On.RoR2.TeamManager.SetTeamExperience += TeamManager_SetTeamExperience;
                    On.RoR2.TeamManager.SetTeamLevel += TeamManager_SetTeamLevel;
                    Log.Info("Custom XP enabled.");
                }

                RebuildCustomTable();

                if (enableMonsterLevelScaling && !_lastEnableMonsterLevelScaling) {
                    On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
                    On.RoR2.UI.AmbientLevelDisplay.Update += AmbientLevelDisplay_Update;
                    Log.Info("Monster level scaling enabled.");
                }
                else if (!enableMonsterLevelScaling && _lastEnableMonsterLevelScaling) {
                    On.RoR2.CharacterBody.RecalculateStats -= CharacterBody_RecalculateStats;
                    On.RoR2.UI.AmbientLevelDisplay.Update -= AmbientLevelDisplay_Update;
                    Log.Info("Monster level scaling disabled.");
                }
            }
            else {
                if (_lastEnableCustomLevelSystem) {
                    On.RoR2.TeamManager.FindLevelForExperience -= TeamManager_FindLevelForExperience;
                    On.RoR2.TeamManager.GetExperienceForLevel -= TeamManager_GetExperienceForLevel;
                    On.RoR2.TeamManager.GiveTeamExperience -= TeamManager_GiveTeamExperience;
                    On.RoR2.TeamManager.SetTeamExperience -= TeamManager_SetTeamExperience;
                    On.RoR2.TeamManager.SetTeamLevel -= TeamManager_SetTeamLevel;

                    if (_lastEnableMonsterLevelScaling) {
                        On.RoR2.CharacterBody.RecalculateStats -= CharacterBody_RecalculateStats;
                        On.RoR2.UI.AmbientLevelDisplay.Update -= AmbientLevelDisplay_Update;
                    }

                    _customExperienceTable = null;
                    Log.Info("Custom XP disabled.");
                }
            }

            _lastEnableCustomLevelSystem = enableCustomLevelSystem;
            _lastEnableMonsterLevelScaling = enableMonsterLevelScaling;
            _lastMaxLevel = MaxLevel;

            SyncConfigAsServer();
        }

        public static int GetCurrentMonsterLevel() {
            if (!Run.instance)
                return 1;

            if (ModConfig.EnableMonsterLevelScalingValue && ModConfig.IsModEnabled) {
                float scaleFactor = (float)ModConfig.MaxLevelValue / 94f; // Vanilla max level is 94
                int ambientFloor = Run.instance.ambientLevelFloor;
                int scaledAmbient = Mathf.FloorToInt(ambientFloor * scaleFactor);
                return Mathf.Min(scaledAmbient, ModConfig.MaxLevelValue);
            }
            return Run.instance.ambientLevelFloor;
        }

        private void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self) {
            orig(self);

            if (!Run.instance || !ModConfig.EnableMonsterLevelScalingValue || !ModConfig.IsModEnabled)
                return;

            if (self.teamComponent == null)
                return;

            TeamIndex team = self.teamComponent.teamIndex;
            if (team == TeamIndex.Player)
                return;

            bool usesAmbient = (self.bodyFlags & CharacterBody.BodyFlags.UsesAmbientLevel) != 0;
            if (!usesAmbient)
                return;

            self.level = Mathf.Max(self.level, GetCurrentMonsterLevel());
        }

        private static void AmbientLevelDisplay_Update(On.RoR2.UI.AmbientLevelDisplay.orig_Update orig, RoR2.UI.AmbientLevelDisplay self) {
            orig(self);
            if (ModConfig.EnableMonsterLevelScalingValue && ModConfig.IsModEnabled && Run.instance) {
                int scaledLevel = GetCurrentMonsterLevel();
                self.text.text = Language.GetStringFormatted("AMBIENT_LEVEL_DISPLAY_FORMAT", scaledLevel.ToString());
            }
        }

        private static double CalculateOptimalMultiplier(uint maxLevel) {
            if (maxLevel < 2)
                return 1.0;

            double target = (double)ulong.MaxValue - XP_BUFFER;
            double low = SEARCH_LOW;
            double high = SEARCH_HIGH;
            double mid = 0;

            for (int i = 0; i < 64; i++) {
                mid = (low + high) / 2.0;
                double sum = XP_BASE * (Math.Pow(mid, maxLevel - 1) - 1.0) / (mid - 1.0);
                if (sum < target)
                    low = mid;
                else
                    high = mid;
            }

            return mid;
        }

        private void RebuildCustomTable() {
            int targetMaxLevel = MaxLevel;

            if (targetMaxLevel < 2) {
                Log.Error("MaxLevel must be at least 2. Defaulting to 256.");
                targetMaxLevel = 256;
            }

            double multiplier = CalculateOptimalMultiplier((uint)targetMaxLevel);
            double cumulative = 0.0;
            double stepMultiplier = 1.0;

            var list = new System.Collections.Generic.List<ulong> { 0uL, 0uL };
            bool maxed = false;

            for (uint lvl = 2; lvl <= targetMaxLevel; lvl++) {
                ulong xp;

                if (maxed) {
                    xp = ulong.MaxValue;
                }
                else {
                    double step = XP_BASE * stepMultiplier;
                    stepMultiplier *= multiplier;
                    cumulative += step;

                    if (cumulative >= (double)ulong.MaxValue) {
                        cumulative = (double)ulong.MaxValue;
                        xp = ulong.MaxValue;
                        maxed = true;
                    }
                    else if (cumulative < 0.0) {
                        xp = 0;
                    }
                    else {
                        xp = (ulong)cumulative;
                    }

                    if (list.Count > 1 && xp <= list[^1] && xp != ulong.MaxValue) {
                        xp = ulong.MaxValue;
                        maxed = true;
                    }
                }

                list.Add(xp);
            }

            _customExperienceTable = [.. list];
            _customNaturalLevelCap = (uint)(_customExperienceTable.Length - 1);
            _customHardExpCap = _customExperienceTable[^1];

            Log.Info($"XP table rebuilt. Max: {_customNaturalLevelCap}, Cap: {_customHardExpCap}, R: {multiplier:F4}");
        }

        private uint TeamManager_FindLevelForExperience(On.RoR2.TeamManager.orig_FindLevelForExperience orig, ulong exp) {
            if (!ModConfig.IsModEnabled)
                return orig(exp);
            if (_customExperienceTable == null || _customExperienceTable.Length == 0)
                return orig(exp);

            for (uint i = 1; i < _customExperienceTable.Length; i++) {
                if (_customExperienceTable[i] > exp)
                    return i - 1;
            }
            return _customNaturalLevelCap;
        }

        private ulong TeamManager_GetExperienceForLevel(On.RoR2.TeamManager.orig_GetExperienceForLevel orig, uint level) {
            if (!ModConfig.IsModEnabled)
                return orig(level);
            if (_customExperienceTable == null || _customExperienceTable.Length == 0)
                return orig(level);

            if (level >= _customExperienceTable.Length)
                level = _customNaturalLevelCap;

            return _customExperienceTable[level];
        }

        private void TeamManager_GiveTeamExperience(On.RoR2.TeamManager.orig_GiveTeamExperience orig, TeamManager self, TeamIndex teamIndex, ulong exp) {
            if (!NetworkServer.active)
                return;

            if (!ModConfig.IsModEnabled) {
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

            ReadOnlyCollection<TeamComponent> members = TeamComponent.GetTeamMembers(teamIndex);
            for (int i = 0; i < members.Count; i++) {
                CharacterBody body = members[i].GetComponent<CharacterBody>();
                if (body?.master)
                    body.master.TrackBeadExperience(exp);
            }
        }

        private void TeamManager_SetTeamLevel(On.RoR2.TeamManager.orig_SetTeamLevel orig, TeamManager self, TeamIndex teamIndex, uint newLevel) {
            if (!ModConfig.IsModEnabled) {
                orig(self, teamIndex, newLevel);
                return;
            }

            if (teamIndex >= TeamIndex.Neutral && teamIndex < TeamIndex.Count && self.GetTeamLevel(teamIndex) != newLevel) {
                self.SetTeamExperience(teamIndex, TeamManager_GetExperienceForLevel(null, newLevel));
            }
        }

        private void TeamManager_SetTeamExperience(On.RoR2.TeamManager.orig_SetTeamExperience orig, TeamManager self, TeamIndex teamIndex, ulong exp) {
            if (!ModConfig.IsModEnabled) {
                orig(self, teamIndex, exp);
                return;
            }

            if (exp > _customHardExpCap)
                exp = _customHardExpCap;

            ulong[] expArr = (ulong[])s_teamExperienceField?.GetValue(self);
            expArr?[(int)teamIndex] = exp;

            uint oldLvl = self.GetTeamLevel(teamIndex);
            uint newLvl = TeamManager_FindLevelForExperience(null, exp);

            if (oldLvl != newLvl) {
                ReadOnlyCollection<TeamComponent> members = TeamComponent.GetTeamMembers(teamIndex);
                for (int i = 0; i < members.Count; i++) {
                    members[i].GetComponent<CharacterBody>()?.OnTeamLevelChanged();
                }

                uint[] levelsArr = (uint[])s_teamLevelsField?.GetValue(self);
                levelsArr?[(int)teamIndex] = newLvl;

                ulong[] currExpArr = (ulong[])s_teamCurrentExpField?.GetValue(self);
                currExpArr?[(int)teamIndex] = TeamManager_GetExperienceForLevel(null, newLvl);

                ulong[] nextExpArr = (ulong[])s_teamNextExpField?.GetValue(self);
                nextExpArr?[(int)teamIndex] = TeamManager_GetExperienceForLevel(null, newLvl + 1);

                if (oldLvl < newLvl) {
                    GlobalEventManager.OnTeamLevelUp(teamIndex);

                    if (teamIndex == TeamIndex.Player) {
                        for (uint l = oldLvl + 1; l <= newLvl; l++) {
                            OnLevelUp?.Invoke(l);
                        }
                    }
                }
            }

            if (NetworkServer.active) {
                self.SetDirtyBit((uint)(1 << (int)teamIndex));
            }
        }
    }
}
