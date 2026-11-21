using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace LevelUpChoices
{
    public class InteractableSpawnHook : MonoBehaviour
    {
        private void Start()
        {
            On.RoR2.SceneDirector.PopulateScene += OnPopulateScene;
            On.RoR2.DirectorCore.TrySpawnObject += OnTrySpawnObject;
        }

        private void OnDestroy()
        {
            On.RoR2.SceneDirector.PopulateScene -= OnPopulateScene;
            On.RoR2.DirectorCore.TrySpawnObject -= OnTrySpawnObject;
        }

        private static readonly HashSet<string> BlacklistedSpawns = new(StringComparer.OrdinalIgnoreCase)
        {
            // https://github.com/risk-of-thunder/R2API/blob/master/R2API.Director/DirectorAPIhelpers.cs

            // "iscscavbackpack", // rare Scavenger reward
            // "iscscavlunarbackpack", // rare Twisted Scavenger reward
            // "iscbarrel1", // gold and XP, no longer needed to dilute equipment barrel spawns
            // "iscshrinecombat", // optional fights for gold and XP, all three variants
            // "iscshrinecombatsandy",
            // "iscshrinecombatsnowy",

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
            "iscshrineblood", // trades health for gold
            "iscshrinebloodsandy",
            "iscshrinebloodsnowy",
            "iscshrinechance", // we don't want item spawns
            "iscshrinechancesandy",
            "iscshrinechancesnowy",
            "iscshrinecleanse", // trades lunar items or equipment for pearls
            "iscshrinecleansesandy",
            "iscshrinecleansesnowy",
            "iscshrinerestack",
            "iscshrinerestacksandy",
            "iscshrinerestacksnowy",
            "isctripleshop",
            "isctripleshoplarge",
            "isccategorychest2damage",
            "isccategorychest2healing",
            "isccategorychest2utility",
            "isctemporaryitemsshop", // Temporary Item Distributor
            "iscMimic", // Starstorm 2's Security Chest
            "iscdronescrapper", // turns drones into scrap
            "isclockbox", // Rusted Key lockbox
            "isclockboxvoid", // Encrusted Key lockbox
            "iscfreechest", // Shipping Request Form delivery

            // Item Qualities' item sources, including sources spawned outside stage pools.
            "iscQualityChest1",
            "iscQualityChest2",
            "iscQualityDuplicator",
            "iscQualityDuplicatorLarge",
            "iscQualityDuplicatorMilitary",
            "iscQualityDuplicatorWild",
            "iscQualityScrapper",
            "iscSpeedOnPickupBarrel",
            "iscChest2Stealthed"
        };

        private static readonly HashSet<string> EquipmentSpawns = new(StringComparer.OrdinalIgnoreCase)
        {
            // Replaced by equipment rounds when equipment choices are enabled; otherwise keep.
            "iscequipmentbarrel",
            "isctripleshopequipment",
            "iscQualityEquipmentBarrel"
        };

        private static bool ServerModEnabled => NetworkServer.active && ModConfig.IsModEnabled;
        private static bool RemovalEnabled => ServerModEnabled && ModConfig.EnableInteractableRemoval.Value;

        private static bool IsBlockedSpawn(string spawnName)
        {
            return BlacklistedSpawns.Contains(spawnName)
                || (ModConfig.EnableEquipmentChoices.Value && EquipmentSpawns.Contains(spawnName));
        }

        private static GameObject OnTrySpawnObject(On.RoR2.DirectorCore.orig_TrySpawnObject orig, DirectorCore self, DirectorSpawnRequest request)
        {
            if (RemovalEnabled && request?.spawnCard && IsBlockedSpawn(request.spawnCard.name))
                return null;

            return orig(self, request);
        }

        private static void OnPopulateScene(On.RoR2.SceneDirector.orig_PopulateScene orig, SceneDirector self)
        {
            if (ServerModEnabled)
            {
                float multiplier = ModConfig.InteractableBudgetMultiplier.Value;
                if (multiplier != 1f)
                    self.interactableCredit = Mathf.FloorToInt(self.interactableCredit * multiplier);
            }

            if (RemovalEnabled && ClassicStageInfo.instance && ClassicStageInfo.instance.interactableCategories)
            {
                var selection = ClassicStageInfo.instance.interactableCategories;
                for (int i = 0; i < selection.categories.Length; i++)
                {
                    var category = selection.categories[i];
                    var cards = Array.FindAll(category.cards, card => !card?.spawnCard || !IsBlockedSpawn(card.spawnCard.name));
                    if (cards.Length == category.cards.Length)
                        continue;

                    category.cards = cards;
                    if (cards.Length == 0)
                        category.selectionWeight = 0f;

                    selection.categories[i] = category;
                }
            }

            orig(self);
        }
    }
}
