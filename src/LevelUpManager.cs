using System.Collections.Generic;
using System.Linq;
using LevelUpChoices.UI;
using R2API.Networking;
using R2API.Networking.Interfaces;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace LevelUpChoices
{
    public class LevelUpManager : MonoBehaviour
    {
        public static LevelUpManager Instance { get; private set; }

        public class PlayerState
        {
            public int SelectionTokens = 0;
            public int BanishTokens = ModConfig.StartingBanishTokens.Value;
            public int RerollTokens = ModConfig.StartingRerollTokens.Value;
            public int UsedTokens = 0;
            public List<ItemIndex> CurrentOptions = [];
            public List<ItemIndex> CurrentSynergies = [];
            public PlayerDropTable DropTable = new();
        }

        // Client side state
        public int AvailableTokens { get; private set; } = 0;
        public int BanishTokens { get; private set; } = 0;
        public int RerollTokens { get; private set; } = 0;

        // Local cache for UI
        private List<PickupIndex> currentOptions = [];
        private List<ItemIndex> currentSynergies = [];

        // Server side state
        private readonly Dictionary<NetworkInstanceId, PlayerState> playerStates = [];

        private void Awake()
        {
            if (Instance)
                Destroy(Instance);
            Instance = this;

            // Global hooks
            ExperienceHook.OnLevelUp += OnLevelUp;
            Run.onRunDestroyGlobal += OnRunDestroy;
        }

        private void OnDestroy()
        {
            ExperienceHook.OnLevelUp -= OnLevelUp;
            Run.onRunDestroyGlobal -= OnRunDestroy;
        }

        private void OnLevelUp(uint newLevel)
        {
            if (!NetworkServer.active)
                return;
            if (!ModConfig.IsModEnabled)
                return;

            foreach (var player in PlayerCharacterMasterController.instances)
            {
                if (player.networkUser)
                {
                    var netId = player.networkUser.netId;
                    if (!playerStates.TryGetValue(netId, out var state))
                    {
                        state = new PlayerState();
                        playerStates[netId] = state;
                        bool isDrifter = false;
                        if (DLC3Content.BodyPrefabs.DrifterBody != null)
                        {
                            if (player.master && player.master.bodyPrefab)
                            {
                                var bodyComp = player.master.bodyPrefab.GetComponent<CharacterBody>();
                                if (bodyComp)
                                {
                                    isDrifter = bodyComp.bodyIndex == DLC3Content.BodyPrefabs.DrifterBody.bodyIndex;
                                }
                            }
                            else if (player.body)
                            {
                                isDrifter = player.body.bodyIndex == DLC3Content.BodyPrefabs.DrifterBody.bodyIndex;
                            }
                        }
                        state.DropTable.Initialize(isDrifter);
                    }

                    state.SelectionTokens++;

                    int levelsPerBanish = ModConfig.LevelsPerBanishToken.Value;
                    if (levelsPerBanish > 0 && newLevel % (uint)levelsPerBanish == 0)
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

        private void OnRunDestroy(Run run)
        {
            AvailableTokens = 0;
            BanishTokens = 0;
            RerollTokens = 0;

            playerStates.Clear();
            currentOptions.Clear();
            currentSynergies.Clear();

            // Safety net: ensure we don't leave the game paused
            GamePauseManager.ForceReset();

            if (ItemSelectUI.Instance)
            {
                ItemSelectUI.Instance.Hide();
            }
        }

        public void UpdatePlayerState(int sTokens, int bTokens, int rTokens)
        {
            AvailableTokens = sTokens;
            BanishTokens = bTokens;
            RerollTokens = rTokens;
            ItemSelectUI.Instance?.UpdateTokens();
        }

        public void UpdateAvailableItems(List<PickupIndex> options, List<ItemIndex> synergies = null)
        {
            currentOptions = options;
            currentSynergies = synergies ?? [.. new ItemIndex[options.Count]];
            if (ItemSelectUI.Instance != null && ItemSelectUI.Instance.IsVisible)
            {
                ItemSelectUI.Instance.UpdateOptions(currentOptions, currentSynergies);
            }
        }

        public bool SpendTokenLocal()
        {
            if (AvailableTokens > 0)
            {
                AvailableTokens--;
                if (AvailableTokens <= 0)
                {
                    ItemSelectUI.Instance.Hide();
                }
                ItemSelectUI.Instance.UpdateTokens();
                return true;
            }
            return false;
        }

        private void SyncState(NetworkInstanceId netId)
        {
            if (playerStates.TryGetValue(netId, out var s))
            {
                new Networking.SyncPlayerState(netId, s.SelectionTokens, s.BanishTokens, s.RerollTokens).Send(NetworkDestination.Clients);
            }
        }

        private void SyncOptions(NetworkInstanceId netId)
        {
            if (playerStates.TryGetValue(netId, out var state))
            {
                var pickups = state.CurrentOptions
                    .Select(i => PickupCatalog.FindPickupIndex(i))
                    .ToList();
                new Networking.SyncItems(netId, pickups, state.CurrentSynergies).Send(NetworkDestination.Clients);
            }
        }

        public void HandlePlayerSelection(NetworkInstanceId netId, PickupIndex selection)
        {
            if (!NetworkServer.active)
                return;
            if (!playerStates.TryGetValue(netId, out var state))
                return;

            if (state.SelectionTokens <= 0)
                return;

            state.SelectionTokens--;
            state.UsedTokens++;
            state.DropTable.RecalculateWeights(state.UsedTokens);

            if (ModConfig.RerollTokenRefreshOnPick.Value)
                state.RerollTokens = 1;

            var pickupDef = PickupCatalog.GetPickupDef(selection);
            if (pickupDef == null || pickupDef.itemIndex == ItemIndex.None)
            {
                Log.Warning($"Invalid pickup {selection}. Refunding token.");
                state.SelectionTokens++;
                state.UsedTokens--;
                SyncState(netId);
                return;
            }

            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                if (pcmc.networkUser && pcmc.networkUser.netId == netId)
                {
                    var master = pcmc.master;
                    if (master && master.inventory)
                    {
                        master.inventory.GiveItemPermanent(pickupDef.itemIndex);
                    }
                    break;
                }
            }

            RollItemsForPlayer(netId);
            SyncState(netId);
        }

        private static float GetPlayerLuck(NetworkInstanceId netId)
        {
            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                if (pcmc.networkUser && pcmc.networkUser.netId == netId)
                {
                    if (pcmc.master)
                    {
                        pcmc.master.GetBody()?.RecalculateStats();
                        return pcmc.master.luck;
                    }
                }
            }

            return 0f;
        }

        public void HandlePlayerBanish(NetworkInstanceId netId, int slotIndex)
        {
            if (!NetworkServer.active)
                return;
            if (!playerStates.TryGetValue(netId, out var state))
                return;

            if (state.BanishTokens <= 0)
                return;
            if (slotIndex < 0 || slotIndex >= state.CurrentOptions.Count)
                return;

            ItemIndex itemToBanish = state.CurrentOptions[slotIndex];
            state.BanishTokens--;
            state.DropTable.Remove(itemToBanish);

            var banishExclude = new List<ItemIndex>(state.CurrentOptions);
            banishExclude.RemoveAt(slotIndex);

            var (RolledItem, SynergizedWith) = RollSingleSlot(netId, state, banishExclude);
            state.CurrentOptions[slotIndex] = RolledItem;
            if (slotIndex < state.CurrentSynergies.Count)
            {
                state.CurrentSynergies[slotIndex] = SynergizedWith;
            }

            SyncState(netId);
            SyncOptions(netId);
        }

        public void HandlePlayerReroll(NetworkInstanceId netId, int slotIndex)
        {
            if (!NetworkServer.active)
                return;
            if (!playerStates.TryGetValue(netId, out var state))
                return;

            if (state.RerollTokens <= 0)
                return;
            if (slotIndex < 0 || slotIndex >= state.CurrentOptions.Count)
                return;

            state.RerollTokens--;

            var rerollExclude = new List<ItemIndex>(state.CurrentOptions);
            rerollExclude.RemoveAt(slotIndex);

            var (RolledItem, SynergizedWith) = RollSingleSlot(netId, state, rerollExclude);
            state.CurrentOptions[slotIndex] = RolledItem;
            if (slotIndex < state.CurrentSynergies.Count)
            {
                state.CurrentSynergies[slotIndex] = SynergizedWith;
            }

            SyncState(netId);
            SyncOptions(netId);
        }

        private static (ItemIndex RolledItem, ItemIndex SynergizedWith) RollSingleSlot(NetworkInstanceId netId, PlayerState state, List<ItemIndex> exclude)
        {
            float luck = GetPlayerLuck(netId);

            ItemIndex normallyRolled = state.DropTable.Roll(luck, exclude);
            if (normallyRolled == ItemIndex.None)
            {
                Log.Warning($"Rolled invalid normallyRolled item, skipping slot.");
                return (ItemIndex.None, ItemIndex.None);
            }

            ItemIndex rolled = normallyRolled;
            ItemIndex synergizedWith = ItemIndex.None;
            var normalDef = ItemCatalog.GetItemDef(normallyRolled);
            if (normalDef != null)
            {
                var (newRolled, newSynergy) = TryRollSimilarItem(netId, exclude, state.DropTable, normalDef.tier);
                if (newRolled != ItemIndex.None)
                {
                    rolled = newRolled;
                    synergizedWith = newSynergy;
                }
            }

            // Guard: skip items that have no valid pickup (e.g. modded items
            // registered in ItemCatalog but without a pickup definition).
            if (rolled == ItemIndex.None || PickupCatalog.FindPickupIndex(rolled) == PickupIndex.none)
            {
                Log.Warning($"Rolled invalid item {rolled}, skipping slot.");
                return (ItemIndex.None, ItemIndex.None);
            }

            return (rolled, synergizedWith);
        }

        private static (ItemIndex RolledItem, ItemIndex SynergizedWith) TryRollSimilarItem(NetworkInstanceId netId, List<ItemIndex> exclude, PlayerDropTable dropTable, ItemTier targetTier)
        {
            if (Random.Range(0f, 100f) >= ModConfig.SimilarItemChance.Value)
            {
                return (ItemIndex.None, ItemIndex.None);
            }

            Inventory inventory = null;
            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                if (pcmc.networkUser && pcmc.networkUser.netId == netId)
                {
                    if (pcmc.master)
                        inventory = pcmc.master.inventory;
                    break;
                }
            }

            if (!inventory)
                return (ItemIndex.None, ItemIndex.None);

            var ownedItems = new List<(ItemIndex Item, int Count)>();
            foreach (var itemIndex in inventory.itemAcquisitionOrder)
            {
                int count = inventory.GetItemCountEffective(itemIndex);
                if (count > 0)
                {
                    ownedItems.Add((itemIndex, count));
                }
            }

            if (ownedItems.Count == 0)
                return (ItemIndex.None, ItemIndex.None);

            var topItems = ownedItems.OrderByDescending(x => x.Count).Take(5).ToList();
            var chosenBaseItem = topItems[Random.Range(0, topItems.Count)].Item;

            if (ItemSimilarityManager.SimilarItemsMap.TryGetValue(chosenBaseItem, out var similarItems))
            {
                foreach (var similarItem in similarItems)
                {
                    if (!exclude.Contains(similarItem) && dropTable.CanDrop(similarItem))
                    {
                        var def = ItemCatalog.GetItemDef(similarItem);
                        if (def != null && def.tier == targetTier)
                        {
                            return (similarItem, chosenBaseItem);
                        }
                    }
                }
            }

            return (ItemIndex.None, ItemIndex.None);
        }

        private void RollItemsForPlayer(NetworkInstanceId netId)
        {
            if (!playerStates.TryGetValue(netId, out var state))
                return;

            state.CurrentOptions.Clear();
            state.CurrentSynergies.Clear();
            int choiceCount = Mathf.Max(1, ModConfig.ItemChoiceCount.Value);
            for (int i = 0; i < choiceCount; i++)
            {
                var (RolledItem, SynergizedWith) = RollSingleSlot(netId, state, state.CurrentOptions);
                if (RolledItem == ItemIndex.None)
                    continue;

                state.CurrentOptions.Add(RolledItem);
                state.CurrentSynergies.Add(SynergizedWith);
            }

            SyncOptions(netId);

            var localUsers = NetworkUser.readOnlyLocalPlayersList;
            if (localUsers.Count > 0 && localUsers[0].netId == netId)
            {
                UpdateAvailableItems([.. state.CurrentOptions.Select(i => PickupCatalog.FindPickupIndex(i))], state.CurrentSynergies);
            }
        }

        private void Update()
        {
            if (ModConfig.ToggleMenuKey.Value.IsDown())
            {
                if (ItemSelectUI.Instance)
                {
                    if (ItemSelectUI.Instance.IsVisible)
                    {
                        ItemSelectUI.Instance.Hide();
                    }
                    else if (Run.instance != null && AvailableTokens > 0)
                    {
                        ItemSelectUI.Instance.ShowChoices(currentOptions, currentSynergies);
                    }
                }
            }
        }
    }
}
