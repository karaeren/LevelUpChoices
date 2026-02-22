using System;
using RoR2;
using UnityEngine;

namespace LevelUpChoices
{
    public class ExperienceHook : MonoBehaviour
    {
        public static event Action<uint> OnLevelUp;

        private float StartMultiplier => ModConfig.ExperienceStartMultiplier.Value;
        private float GrowthRate => ModConfig.ExperienceGrowthRate.Value;

        private uint cachedLevel = 0;
        private double cachedMultiplier = 0;

        private void Awake()
        {
            On.RoR2.TeamManager.GiveTeamExperience += TeamManager_GiveTeamExperience;
            Run.onRunDestroyGlobal += OnRunDestroy;
        }

        private void OnDestroy()
        {
            On.RoR2.TeamManager.GiveTeamExperience -= TeamManager_GiveTeamExperience;
            Run.onRunDestroyGlobal -= OnRunDestroy;
        }

        private void OnRunDestroy(Run run)
        {
            cachedLevel = 0;
            cachedMultiplier = 0;
        }

        private void TeamManager_GiveTeamExperience(On.RoR2.TeamManager.orig_GiveTeamExperience orig, TeamManager self, TeamIndex teamIndex, ulong experience)
        {
            if (teamIndex != TeamIndex.Player || !ModConfig.ModEnabled.Value)
            {
                orig(self, teamIndex, experience);
                return;
            }

            // On first call (or after a reset), initialize cache from the actual current level.
            if (cachedLevel == 0)
            {
                cachedLevel = self.GetTeamLevel(teamIndex);
                cachedMultiplier = StartMultiplier * Math.Pow(GrowthRate, cachedLevel - 1);
            }

            ulong modifiedExperience = (ulong)(experience * cachedMultiplier);
            modifiedExperience = (ulong)Math.Max(modifiedExperience, 1); // Ensure at least 1 experience is given

            orig(self, teamIndex, modifiedExperience);

            // After orig(), check if the team leveled up and update cached values.
            uint newLevel = self.GetTeamLevel(teamIndex);
            if (newLevel > cachedLevel)
            {
                uint previousLevel = cachedLevel;
                cachedLevel = newLevel;
                cachedMultiplier = StartMultiplier * Math.Pow(GrowthRate, cachedLevel - 1);

                // Fire once per level gained so tokens are awarded for every level,
                // even when a large XP gain skips multiple levels at once.
                for (uint lvl = previousLevel + 1; lvl <= newLevel; lvl++)
                {
                    OnLevelUp?.Invoke(lvl);
                }
            }
        }
    }
}
