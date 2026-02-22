using RoR2;
using UnityEngine;
using System;
using System.Linq;

namespace LevelUpChoices
{
    public class InteractableSpawnHook : MonoBehaviour
    {
        private void Start()
        {
            SceneDirector.onPrePopulateSceneServer += OnPrePopulateSceneServer;
        }

        private void OnPrePopulateSceneServer(SceneDirector director)
        {
            if (!ModConfig.ModEnabled.Value || !ModConfig.EnableInteractableRemoval.Value) return;
            if (!ClassicStageInfo.instance || !ClassicStageInfo.instance.interactableCategories) return;

            var selection = ClassicStageInfo.instance.interactableCategories;
            if (!selection)
            {
                Log.Error("No Interactable Categories found on ClassicStageInfo!");
                return;
            }

            // https://github.com/risk-of-thunder/R2API/blob/master/R2API.Director/DirectorAPIhelpers.cs
            // We will allow scavenger sacks since they are rare.
            // string ScavengersSack = "iscscavbackpack";
            // string ScavengersLunarSack = "iscscavlunarbackpack";

            // We leave barrels on even though gold is mostly useless because
            // on base game with no interactable mods, there isn't much to spawn so we see
            // ton of equipment barrels which isn't balanced.
            // string Barrel = "iscbarrel1";

            string AdaptiveChest = "isccasinochest";
            string ChestDamage = "isccategorychestdamage";
            string ChestHealing = "isccategorychesthealing";
            string ChestUtility = "isccategorychestutility";
            string Chest = "iscchest1";
            string CloakedChest = "iscchest1stealthed";
            string LargeChest = "iscchest2";
            string Printer3D = "iscduplicator";
            string Printer3DLarge = "iscduplicatorlarge";
            string PrinterMiliTech = "iscduplicatormilitary";
            string PrinterOvergrown3D = "iscduplicatorwild";
            // We will allow Equipment Barrels since they are the only way to get items from the mod currently
            // but we will disable them if that ever changes and we have a way to differentiate them from regular barrels.
            // string EquipmentBarrel = "iscequipmentbarrel";
            string LegendaryChest = "iscgoldchest";
            string LunarPod = "isclunarchest";
            string Scrapper = "iscscrapper";
            string ShrineOfBlood = "iscshrineblood"; // gold is useless
            string ShrineOfBloodSandy = "iscshrinebloodsandy";
            string ShrineOfBloodSnowy = "iscshrinebloodsnowy";
            string ShrineOfChance = "iscshrinechance"; // we don't want item spawns
            string ShrineOfChanceSandy = "iscshrinechancesandy";
            string ShrineOfChanceSnowy = "iscshrinechancesnowy";
            string CleansingPool = "iscshrinecleanse"; // 3d printer for lunar items
            string CleansingPoolSandy = "iscshrinecleansesandy";
            string CleansingPoolSnowy = "iscshrinecleansesnowy";
            string ShrineOfCombat = "iscshrinecombat"; // just spawns mobs, no items
            string ShrineOfCombatSandy = "iscshrinecombatsandy";
            string ShrineOfCombatSnowy = "iscshrinecombatsnowy";
            string ShrineOfOrder = "iscshrinerestack";
            string ShrineOfOrderSandy = "iscshrinerestacksandy";
            string ShrineOfOrderSnowy = "iscshrinerestacksnowy";
            string TripleShop = "isctripleshop";
            // string TripleShopEquipment = "isctripleshopequipment"; // allowed for equipments
            string TripleShopLarge = "isctripleshoplarge";
            string LargeChestDamage = "isccategorychest2damage";
            string LargeChestHealing = "isccategorychest2healing";
            string LargeChestUtility = "isccategorychest2utility";

            string[] blacklistedSpawns = [
                AdaptiveChest, ChestDamage, ChestHealing, ChestUtility, Chest, CloakedChest, LargeChest, Printer3D, Printer3DLarge, PrinterMiliTech, PrinterOvergrown3D,
                LegendaryChest, LunarPod, Scrapper, ShrineOfBlood, ShrineOfBloodSandy, ShrineOfBloodSnowy, ShrineOfChance, ShrineOfChanceSandy, ShrineOfChanceSnowy, CleansingPool, CleansingPoolSandy, CleansingPoolSnowy,
                ShrineOfCombat, ShrineOfCombatSandy, ShrineOfCombatSnowy, ShrineOfOrder, ShrineOfOrderSandy, ShrineOfOrderSnowy, TripleShop, TripleShopLarge, LargeChestDamage, LargeChestHealing, LargeChestUtility
            ];

            float removedWeight = 0f;

            for (int i = 0; i < selection.categories.Length; i++)
            {
                selection.categories[i].cards = selection.categories[i].cards.Where(c => !blacklistedSpawns.Contains(c.spawnCard.name, StringComparer.OrdinalIgnoreCase)).ToArray();
                // If no cards left, set weight to 0
                if (selection.categories[i].cards.Length == 0)
                {
                    removedWeight += selection.categories[i].selectionWeight;
                    selection.categories[i].selectionWeight = 0f;
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
