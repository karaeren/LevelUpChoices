using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace LevelUpChoices
{
    public class InteractableSpawnHook : MonoBehaviour
    {
        private void Start()
        {
            SceneDirector.onPrePopulateSceneServer += OnPrePopulateSceneServer;
        }

        private static readonly HashSet<string> BlacklistedSpawns = new(StringComparer.OrdinalIgnoreCase)
        {
            // https://github.com/risk-of-thunder/R2API/blob/master/R2API.Director/DirectorAPIhelpers.cs

            // "iscscavbackpack", // rare
            // "iscscavlunarbackpack", // rare

            // "iscbarrel1", // useless but needed to reduce the amount of equipment barrels

            // "iscequipmentbarrel", // only way to get equipment
            // "isctripleshopequipment", // only way to get equipment
            "isccasinochest",
            "isccategorychestdamage",
            "isccategorychesthealing",
            "isccategorychestutility",
            "iscchest1",
            "iscchest1stealthed",
            "iscchest2",
            "iscduplicator",
            "iscduplicatorlarge",
            "iscduplicatormilitary",
            "iscduplicatorwild",
            "iscgoldchest",
            "isclunarchest",
            "iscscrapper",
            "iscshrineblood", // gold is useless
            "iscshrinebloodsandy",
            "iscshrinebloodsnowy",
            "iscshrinechance", // we don't want item spawns
            "iscshrinechancesandy",
            "iscshrinechancesnowy",
            "iscshrinecleanse", // 3d printer for lunar items
            "iscshrinecleansesandy",
            "iscshrinecleansesnowy",
            "iscshrinecombat", // just spawns mobs, no items
            "iscshrinecombatsandy",
            "iscshrinecombatsnowy",
            "iscshrinerestack",
            "iscshrinerestacksandy",
            "iscshrinerestacksnowy",
            "isctripleshop",
            // "isctripleshopequipment", // allowed for equipments
            "isctripleshoplarge",
            "isccategorychest2damage",
            "isccategorychest2healing",
            "isccategorychest2utility"
        };

        private void OnPrePopulateSceneServer(SceneDirector director)
        {
            if (!ModConfig.IsModEnabled || !ModConfig.EnableInteractableRemoval.Value)
                return;
            if (!ClassicStageInfo.instance || !ClassicStageInfo.instance.interactableCategories)
                return;

            var selection = ClassicStageInfo.instance.interactableCategories;
            if (!selection)
            {
                Log.Error("No interactable categories found on ClassicStageInfo!");
                return;
            }

            float removedWeight = 0f;

            for (int i = 0; i < selection.categories.Length; i++)
            {
                var category = selection.categories[i];
                var originalCards = category.cards;
                var filteredCards = new DirectorCard[originalCards.Length];
                int filteredCount = 0;

                for (int j = 0; j < originalCards.Length; j++)
                {
                    if (!BlacklistedSpawns.Contains(originalCards[j].spawnCard.name))
                    {
                        filteredCards[filteredCount++] = originalCards[j];
                    }
                }

                if (filteredCount < originalCards.Length)
                {
                    Array.Resize(ref filteredCards, filteredCount);
                    category.cards = filteredCards;

                    if (filteredCount == 0)
                    {
                        removedWeight += category.selectionWeight;
                        category.selectionWeight = 0f;
                    }
                }
            }

            // Give removed weight to barrels since they are useless
            for (int i = 0; i < selection.categories.Length; i++)
            {
                if (selection.categories[i].name.Equals("Barrels", StringComparison.OrdinalIgnoreCase))
                {
                    selection.categories[i].selectionWeight += removedWeight;
                    break;
                }
            }
        }
    }
}
