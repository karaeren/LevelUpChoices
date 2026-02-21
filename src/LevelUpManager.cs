using System;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Linq;

namespace LevelUpChoices
{
    public class LevelUpManager : MonoBehaviour
    {
        public static LevelUpManager Instance;

        public class PlayerState
        {
            public int SelectionTokens = 0;
            public int BanishTokens = 1;
            public int RerollTokens = 1;
            public int UsedTokens = 0;
            public List<PickupIndex> BanishedItems = new List<PickupIndex>();
            public List<PickupIndex> CurrentOptions = new List<PickupIndex>();
        }

        // Client side state
        public int AvailableTokens { get; private set; } = 0;
        public int BanishTokens { get; private set; } = 0;
        public int RerollTokens { get; private set; } = 0;

        // Local cache for UI
        private List<PickupIndex> currentOptions = new List<PickupIndex>();

        // Server side state
        private Dictionary<NetworkInstanceId, PlayerState> playerStates = new Dictionary<NetworkInstanceId, PlayerState>();

        private void Awake()
        {
            if (Instance) Destroy(Instance);
            Instance = this;

            // Global hooks
            On.RoR2.TeamManager.GiveTeamExperience += TeamManager_GiveTeamExperience;
            Run.onRunDestroyGlobal += OnRunDestroy;
            On.RoR2.ClassicStageInfo.Awake += ClassicStageInfo_Awake;
        }

        private void OnDestroy()
        {
            On.RoR2.TeamManager.GiveTeamExperience -= TeamManager_GiveTeamExperience;
            Run.onRunDestroyGlobal -= OnRunDestroy;
            On.RoR2.ClassicStageInfo.Awake -= ClassicStageInfo_Awake;
        }

        private void ClassicStageInfo_Awake(On.RoR2.ClassicStageInfo.orig_Awake orig, ClassicStageInfo self)
        {
            orig(self);
            if (self.interactableCategories)
            {
                if (NetworkServer.active)
                {
                }
            }
        }

        private void Start()
        {
            SceneDirector.onPrePopulateSceneServer += OnPrePopulateSceneServer;
        }

        private void OnPrePopulateSceneServer(SceneDirector director)
        {
            if (!ClassicStageInfo.instance || !ClassicStageInfo.instance.interactableCategories) return;

            var selection = ClassicStageInfo.instance.interactableCategories;
            if (!selection)
            {
                Log.Error("No Interactable Categories found on ClassicStageInfo!");
                return;
            }

            // Gotten from R2API.DirectorAPIhelpers
            // We will allow scavenger sacks since they are rare.
            // string ScavengersSack = "iscscavbackpack";
            // string ScavengersLunarSack = "iscscavlunarbackpack";
            string Barrel = "iscbarrel1";
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
                Barrel, AdaptiveChest, ChestDamage, ChestHealing, ChestUtility, Chest, CloakedChest, LargeChest, Printer3D, Printer3DLarge, PrinterMiliTech, PrinterOvergrown3D,
                LegendaryChest, LunarPod, Scrapper, ShrineOfBlood, ShrineOfBloodSandy, ShrineOfBloodSnowy, ShrineOfChance, ShrineOfChanceSandy, ShrineOfChanceSnowy, CleansingPool, CleansingPoolSandy, CleansingPoolSnowy,
                ShrineOfCombat, ShrineOfCombatSandy, ShrineOfCombatSnowy, ShrineOfOrder, ShrineOfOrderSandy, ShrineOfOrderSnowy, TripleShop, TripleShopLarge, LargeChestDamage, LargeChestHealing, LargeChestUtility
            ];

            for (int i = 0; i < selection.categories.Length; i++)
            {
                selection.categories[i].cards = selection.categories[i].cards.Where(c => !blacklistedSpawns.Contains(c.spawnCard.name, StringComparer.OrdinalIgnoreCase)).ToArray();
                // If no cards left, set weight to 0
                if (selection.categories[i].cards.Length == 0)
                {
                    selection.categories[i].selectionWeight = 0f;
                }
            }
        }

        private void OnRunDestroy(Run run)
        {
            AvailableTokens = 0;
            BanishTokens = 0;
            RerollTokens = 0;

            playerStates.Clear();
            currentOptions.Clear();

            if (LevelUpUI.Instance)
            {
                LevelUpUI.Instance.Hide();
            }
        }

        private void TeamManager_GiveTeamExperience(On.RoR2.TeamManager.orig_GiveTeamExperience orig, TeamManager self, TeamIndex teamIndex, ulong experience)
        {
            if (teamIndex == TeamIndex.Player)
            {
                var currentLevel = self.GetTeamLevel(teamIndex);
                orig(self, teamIndex, experience);
                var newLevel = self.GetTeamLevel(teamIndex);

                if (newLevel > currentLevel)
                {
                    OnLevelUp(newLevel);
                }
            }
            else
            {
                orig(self, teamIndex, experience);
            }
        }

        private void OnLevelUp(uint newLevel)
        {
            if (!NetworkServer.active) return;

            Log.Info($"LevelUpManager: Level Up {newLevel}! Granting tokens.");

            // Allow XP Compensation for Level 10 and above
            if (newLevel >= 10)
            {
                double nextXpReq = GetExperienceForLevel(newLevel + 1);
                double currentXp = TeamManager.instance.GetTeamExperience(TeamIndex.Player);

                double gap = nextXpReq - currentXp;
                double maxGap = 2000.0;

                if (gap > maxGap)
                {
                    double diff = gap - maxGap;
                    if (diff > 0)
                    {
                        Log.Info($"XP Compensation (Lvl {newLevel}): Gap {gap:F1}, Reducing to {maxGap}. Giving {diff:F1} XP.");
                        TeamManager.instance.GiveTeamExperience(TeamIndex.Player, (ulong)diff);
                    }
                }
            }

            foreach (var player in PlayerCharacterMasterController.instances)
            {
                if (player.networkUser)
                {
                    var netId = player.networkUser.netId;
                    if (!playerStates.ContainsKey(netId))
                    {
                        playerStates[netId] = new PlayerState();
                    }

                    var state = playerStates[netId];
                    state.SelectionTokens++;

                    if (newLevel % 10 == 0)
                    {
                        state.BanishTokens++;
                    }

                    if (state.CurrentOptions == null || state.CurrentOptions.Count == 0)
                    {
                        RollItemsForPlayer(netId);
                    }

                    SyncState(netId);
                    SyncOptions(netId);
                }
            }
        }

        // Formula: (1.55^(Level-1) - 1) / 0.0275
        private double GetExperienceForLevel(uint level)
        {
            if (level <= 1) return 0;
            return (Mathf.Pow(1.55f, level - 1) - 1f) / 0.0275f;
        }

        public void UpdatePlayerState(int sTokens, int bTokens, int rTokens)
        {
            AvailableTokens = sTokens;
            BanishTokens = bTokens;
            RerollTokens = rTokens;
            LevelUpUI.Instance.UpdateTokens();
        }

        public void UpdateAvailableItems(List<PickupIndex> options)
        {
            currentOptions = options;
            if (LevelUpUI.Instance != null && LevelUpUI.Instance.IsVisible)
            {
                LevelUpUI.Instance.UpdateOptions(currentOptions);
            }
        }

        public bool SpendTokenLocal()
        {
            if (AvailableTokens > 0)
            {
                AvailableTokens--;
                if (AvailableTokens <= 0)
                {
                    LevelUpUI.Instance.Hide();
                }
                LevelUpUI.Instance.UpdateTokens();
                return true;
            }
            return false;
        }

        private void SyncState(NetworkInstanceId netId)
        {
            if (playerStates.ContainsKey(netId))
            {
                var s = playerStates[netId];
                new Networking.SyncPlayerState(netId, s.SelectionTokens, s.BanishTokens, s.RerollTokens).Send(NetworkDestination.Clients);
            }
        }

        private void SyncOptions(NetworkInstanceId netId)
        {
            if (playerStates.ContainsKey(netId))
            {
                new Networking.SyncItems(netId, playerStates[netId].CurrentOptions).Send(NetworkDestination.Clients);
            }
        }

        public void HandlePlayerSelection(NetworkInstanceId netId, PickupIndex selection)
        {
            if (!NetworkServer.active) return;
            if (!playerStates.ContainsKey(netId)) return;

            var state = playerStates[netId];

            if (state.SelectionTokens <= 0) return;

            state.SelectionTokens--;
            state.UsedTokens++;

            state.RerollTokens = 1;

            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                if (pcmc.networkUser && pcmc.networkUser.netId == netId)
                {
                    var master = pcmc.master;
                    if (master && master.inventory)
                    {
                        master.inventory.GiveItem(PickupCatalog.GetPickupDef(selection).itemIndex);
                    }
                    break;
                }
            }

            RollItemsForPlayer(netId);
            SyncState(netId);
        }

        public void HandlePlayerBanish(NetworkInstanceId netId, int slotIndex)
        {
            if (!NetworkServer.active) return;
            if (!playerStates.ContainsKey(netId)) return;
            var state = playerStates[netId];

            if (state.BanishTokens <= 0) return;
            if (slotIndex < 0 || slotIndex >= state.CurrentOptions.Count) return;

            PickupIndex itemToBanish = state.CurrentOptions[slotIndex];
            state.BanishTokens--;
            state.BanishedItems.Add(itemToBanish);

            state.CurrentOptions[slotIndex] = GenerateSingleItem(state);

            SyncState(netId);
            SyncOptions(netId);
        }

        public void HandlePlayerReroll(NetworkInstanceId netId, int slotIndex)
        {
            if (!NetworkServer.active) return;
            if (!playerStates.ContainsKey(netId)) return;
            var state = playerStates[netId];

            if (state.RerollTokens <= 0) return;
            if (slotIndex < 0 || slotIndex >= state.CurrentOptions.Count) return;

            state.RerollTokens--;

            state.CurrentOptions[slotIndex] = GenerateSingleItem(state);

            SyncState(netId);
            SyncOptions(netId);
        }

        private void RollItemsForPlayer(NetworkInstanceId netId)
        {
            if (!playerStates.ContainsKey(netId)) return;
            var state = playerStates[netId];

            state.CurrentOptions.Clear();
            for (int i = 0; i < 3; i++)
            {
                state.CurrentOptions.Add(GenerateSingleItem(state));
            }

            SyncOptions(netId);

            if (NetworkUser.readOnlyLocalPlayersList.Count > 0 && NetworkUser.readOnlyLocalPlayersList[0].netId == netId)
            {
                UpdateAvailableItems(state.CurrentOptions);
            }
        }

        private PickupIndex GenerateSingleItem(PlayerState state)
        {
            var run = Run.instance;
            if (!run) return PickupIndex.none;

            WeightedSelection<PickupIndex> selection = new WeightedSelection<PickupIndex>();

            float used = (float)state.UsedTokens;

            // todo: make this based on itemdef ItemTierCatalog.GetItemTierDef(itemDef.tier);
            // check wiki for normal game rates
            
            float w1 = Mathf.Max(0, 75f - (used * 1.5f));
            float remainder = 75f - w1;

            float w2 = 16f + (remainder * 0.5f);
            float w3 = 6f + (remainder * 0.3f);
            // float wEq = 3f;
            float wLunar = 2f + (remainder * 0.1f);
            float wBoss = 1f + (remainder * 0.1f);

            AddDrops(selection, run.availableTier1DropList, w1, state);
            AddDrops(selection, run.availableTier2DropList, w2, state);
            AddDrops(selection, run.availableTier3DropList, w3, state);
            AddDrops(selection, run.availableBossDropList, wBoss, state);
            AddDrops(selection, run.availableLunarItemDropList, wLunar, state);

            // List<PickupIndex> equipments = run.availableEquipmentDropList.Concat(run.availableLunarEquipmentDropList).Where(p => p != PickupIndex.none).ToList();
            // AddDrops(selection, equipments, wEq, state);

            if (selection.Count == 0)
            {
                Log.Warning("No available items to select from!");
                return PickupIndex.none;
            }

            int attempts = 0;
            PickupIndex result = PickupIndex.none;
            while (attempts < 10)
            {
                result = selection.Evaluate(UnityEngine.Random.value);
                if (!state.BanishedItems.Contains(result) && !state.CurrentOptions.Contains(result))
                {
                    return result;
                }
                attempts++;
            }
            return result;
        }

        private void AddDrops(WeightedSelection<PickupIndex> selection, List<PickupIndex> drops, float categoryWeight, PlayerState state)
        {
            if (drops == null || drops.Count == 0) return;
            float individualWeight = categoryWeight / drops.Count;

            foreach (var drop in drops)
            {
                if (state.BanishedItems.Contains(drop)) continue;

                var pickupDef = PickupCatalog.GetPickupDef(drop);
                if (pickupDef != null)
                {
                    var itemDef = ItemCatalog.GetItemDef(pickupDef.itemIndex);
                    // var equipmentDef = EquipmentCatalog.GetEquipmentDef(pickupDef.equipmentIndex);

                    if (itemDef == null /*&& equipmentDef == null*/)
                    {
                        state.BanishedItems.Add(drop);
                        Log.Warning($"{pickupDef.nameToken}/{Language.GetString(pickupDef.nameToken)} has no associated itemDef, banishing.");
                        continue;
                    }
                    else
                    {
                        if (itemDef.name == "TreasureCache")
                        {
                            state.BanishedItems.Add(drop);
                            continue;
                        }
                    }
                }
                else
                {
                    Log.Error($"Pickup {drop} has no PickupDef, skipping.");
                    continue;
                }

                selection.AddChoice(drop, individualWeight);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F3))
            {
                if (LevelUpUI.Instance)
                {
                    if (LevelUpUI.Instance.IsVisible)
                    {
                        LevelUpUI.Instance.Hide();
                    }
                    else if (Run.instance != null && AvailableTokens > 0)
                    {
                        LevelUpUI.Instance.ShowChoices(currentOptions);
                    }
                }
            }
        }
    }
}
