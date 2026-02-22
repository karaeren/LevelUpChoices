using System;
using RoR2;
using UnityEngine;

namespace LevelUpChoices
{
    public class ExperienceHook : MonoBehaviour
    {
        public static event Action<uint> OnLevelUp;

        private const float StartMultiplier = 1.69f;
        private const float GrowthRate = 1.169f;

        private uint cachedLevel = 0;
        private double cachedMultiplier = StartMultiplier;

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
            cachedMultiplier = StartMultiplier;
        }

        private void TeamManager_GiveTeamExperience(On.RoR2.TeamManager.orig_GiveTeamExperience orig, TeamManager self, TeamIndex teamIndex, ulong experience)
        {
            if (teamIndex != TeamIndex.Player)
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
            if (newLevel != cachedLevel)
            {
                cachedLevel = newLevel;
                cachedMultiplier = StartMultiplier * Math.Pow(GrowthRate, cachedLevel - 1);
                OnLevelUp?.Invoke(cachedLevel);
            }
        }
    }
}
