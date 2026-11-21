using System.Collections.Generic;
using System.Linq;
using LevelUpChoices.UI;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace LevelUpChoices
{
    public class LevelUpManager : MonoBehaviour
    {
        private const int FirstEquipmentRound = 10;

        public static LevelUpManager Instance { get; private set; }

        public class PlayerState
        {
            public int SelectionTokens = 0;
            public int BanishTokens = ModConfig.StartingBanishTokens.Value;
            public int RerollTokens = ModConfig.StartingRerollTokens.Value;
            public int UsedTokens = 0;
            public List<PickupIndex> CurrentOptions = [];
            public List<ItemIndex> CurrentSynergies = [];
            public PlayerDropTable DropTable = new();
            public HashSet<EquipmentIndex> BanishedEquipment = [];
            public bool IsEquipmentRound;
            public bool OfferedEquipmentThisRound;
            public bool PreviousRoundWasEquipment;
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
        private Xoroshiro128Plus itemRng;

        private void Awake()
        {
            if (Instance)
                Destroy(Instance);
            Instance = this;

            // Global hooks
            ExperienceHook.OnLevelUp += OnLevelUp;
            Run.onRunStartGlobal += OnRunStart;
            Run.onRunDestroyGlobal += OnRunDestroy;
        }

        private void OnDestroy()
        {
            ExperienceHook.OnLevelUp -= OnLevelUp;
            Run.onRunStartGlobal -= OnRunStart;
            Run.onRunDestroyGlobal -= OnRunDestroy;
        }

        private void OnRunStart(Run run)
        {
            if (NetworkServer.active)
            {
                // fixed salt seed is "Eren"
                itemRng = new Xoroshiro128Plus(run.seed ^ 0x4572656EUL);
            }
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

                    bool needsOptions = state.CurrentOptions.Count == 0;
                    if (needsOptions)
                    {
                        RollItemsForPlayer(netId);
                    }

                    SyncState(netId);
                    if (!needsOptions)
                        SyncOptions(netId);
                }
            }
        }

        private void OnRunDestroy(Run run)
        {
            itemRng = null;
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
                var pickups = state.CurrentOptions.ToList();
                new Networking.SyncItems(netId, pickups, state.CurrentSynergies).Send(NetworkDestination.Clients);
            }
        }

        public void HandlePlayerSelection(NetworkInstanceId netId, PickupIndex selection)
        {
            if (!NetworkServer.active)
                return;
            if (!playerStates.TryGetValue(netId, out var state))
                return;

            if (state.SelectionTokens <= 0 || !state.CurrentOptions.Contains(selection))
            {
                SyncState(netId);
                return;
            }

            var pickupDef = PickupCatalog.GetPickupDef(selection);
            if (pickupDef == null ||
                (state.IsEquipmentRound ? pickupDef.equipmentIndex == EquipmentIndex.None : pickupDef.itemIndex == ItemIndex.None))
            {
                SyncState(netId);
                return;
            }

            var master = FindPlayerMaster(netId);
            if (!master || !master.inventory || !Run.instance)
            {
                SyncState(netId);
                return;
            }

            if (state.IsEquipmentRound)
            {
                if (Run.instance.IsEquipmentExpansionLocked(pickupDef.equipmentIndex))
                {
                    SyncState(netId);
                    return;
                }

                EquipmentLocation location = master.inventory.FindBestEquipmentLocationOrCurrent(Inventory.EquipmentLocationQueryArgs.defaultPickup);
                EquipmentIndex replacedEquipment = master.inventory.GetEquipmentIndex(location);
                master.inventory.SetEquipmentIndex(pickupDef.equipmentIndex, location);
                if (replacedEquipment != EquipmentIndex.None && replacedEquipment != pickupDef.equipmentIndex)
                {
                    PickupIndex replacedPickup = PickupCatalog.FindPickupIndex(replacedEquipment);
                    if (replacedPickup != PickupIndex.none)
                    {
                        var body = master.GetBody();
                        Vector3 position = body ? body.corePosition : master.transform.position;
                        PickupDropletController.CreatePickupDroplet(new UniquePickup(replacedPickup), position, Vector3.up * 20f, false);
                    }
                }
            }
            else
            {
                master.inventory.GiveItemPermanent(pickupDef.itemIndex);
            }

            state.SelectionTokens--;
            state.UsedTokens++;
            state.PreviousRoundWasEquipment = state.OfferedEquipmentThisRound;
            state.DropTable.RecalculateWeights(state.UsedTokens);
            if (ModConfig.RerollTokenRefreshOnPick.Value)
                state.RerollTokens = 1;

            RollItemsForPlayer(netId);
            SyncState(netId);
        }

        public void HandleChooseItemInstead(NetworkInstanceId netId)
        {
            if (!NetworkServer.active || !Run.instance || !playerStates.TryGetValue(netId, out var state))
                return;
            if (state.SelectionTokens <= 0 || !state.IsEquipmentRound || !state.DropTable.HasEligibleItems())
                return;

            var items = new List<PickupIndex>();
            var synergies = new List<ItemIndex>();
            RollItemOptions(netId, state, items, synergies);
            if (items.Count == 0)
                return;

            state.IsEquipmentRound = false;
            state.CurrentOptions = items;
            state.CurrentSynergies = synergies;
            PublishChoices(netId, state);
        }

        private static CharacterMaster FindPlayerMaster(NetworkInstanceId netId)
        {
            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                if (pcmc.networkUser && pcmc.networkUser.netId == netId)
                    return pcmc.master;
            }

            return null;
        }

        private static float GetPlayerLuck(NetworkInstanceId netId)
        {
            var master = FindPlayerMaster(netId);
            if (!master)
                return 0f;

            master.GetBody()?.RecalculateStats();
            return master.luck;
        }

        public void HandlePlayerBanish(NetworkInstanceId netId, int slotIndex)
        {
            if (!NetworkServer.active)
                return;
            if (!playerStates.TryGetValue(netId, out var state))
                return;

            if (state.BanishTokens <= 0 || state.SelectionTokens <= 0)
                return;
            if (slotIndex < 0 || slotIndex >= state.CurrentOptions.Count)
                return;

            if (!TryReplaceOption(netId, state, slotIndex, true))
                return;

            state.BanishTokens--;
            SyncState(netId);
            SyncOptions(netId);
        }

        public void HandlePlayerReroll(NetworkInstanceId netId, int slotIndex)
        {
            if (!NetworkServer.active)
                return;
            if (!playerStates.TryGetValue(netId, out var state))
                return;

            if (state.RerollTokens <= 0 || state.SelectionTokens <= 0)
                return;
            if (slotIndex < 0 || slotIndex >= state.CurrentOptions.Count)
                return;

            if (!TryReplaceOption(netId, state, slotIndex, false))
                return;

            state.RerollTokens--;
            SyncState(netId);
            SyncOptions(netId);
        }

        private bool TryReplaceOption(NetworkInstanceId netId, PlayerState state, int slotIndex, bool banish)
        {
            if (!Run.instance)
                return false;

            var previous = PickupCatalog.GetPickupDef(state.CurrentOptions[slotIndex]);
            if (previous == null)
                return false;

            PickupIndex replacement;
            ItemIndex synergy = ItemIndex.None;
            if (state.IsEquipmentRound)
            {
                var pool = GetEquipmentPool(state, state.CurrentOptions);
                if (pool.Count == 0)
                    return false;

                replacement = pool[itemRng.RangeInt(0, pool.Count)];
                if (banish)
                    state.BanishedEquipment.Add(previous.equipmentIndex);
            }
            else
            {
                var excludedItems = state.CurrentOptions
                    .Select(pickup => PickupCatalog.GetPickupDef(pickup)?.itemIndex ?? ItemIndex.None)
                    .ToList();
                if (!state.DropTable.HasEligibleItems(excludedItems))
                    return false;

                if (banish)
                    state.DropTable.Remove(previous.itemIndex);

                float luck = GetPlayerLuck(netId);
                var rolled = RollSingleSlot(itemRng, netId, state, excludedItems, luck);
                if (rolled.RolledItem == ItemIndex.None)
                    return false;

                replacement = PickupCatalog.FindPickupIndex(rolled.RolledItem);
                synergy = rolled.SynergizedWith;
            }

            state.CurrentOptions[slotIndex] = replacement;
            state.CurrentSynergies[slotIndex] = synergy;
            return true;
        }

        private static (ItemIndex RolledItem, ItemIndex SynergizedWith) RollSingleSlot(Xoroshiro128Plus rng, NetworkInstanceId netId, PlayerState state, List<ItemIndex> exclude, float luck)
        {
            ItemIndex normallyRolled = state.DropTable.Roll(rng, luck, exclude);
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
                var (newRolled, newSynergy) = TryRollSimilarItem(rng, netId, exclude, state.DropTable, normalDef.tier);
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

        private static (ItemIndex RolledItem, ItemIndex SynergizedWith) TryRollSimilarItem(Xoroshiro128Plus rng, NetworkInstanceId netId, List<ItemIndex> exclude, PlayerDropTable dropTable, ItemTier targetTier)
        {
            if (rng.RangeFloat(0f, 100f) >= ModConfig.SimilarItemChance.Value)
            {
                return (ItemIndex.None, ItemIndex.None);
            }

            var master = FindPlayerMaster(netId);
            var inventory = master ? master.inventory : null;
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
            var chosenBaseItem = topItems[rng.RangeInt(0, topItems.Count)].Item;

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

        private static bool ShouldOfferEquipment(int completedRounds, bool previousWasEquipment, float chance, Xoroshiro128Plus rng)
        {
            int nextRound = completedRounds + 1;
            if (nextRound < FirstEquipmentRound)
                return false;
            if (nextRound == FirstEquipmentRound)
                return true;
            if (previousWasEquipment || chance <= 0f)
                return false;
            return chance >= 100f || rng.RangeFloat(0f, 100f) < chance;
        }

        private static List<PickupIndex> GetEquipmentPool(PlayerState state, ICollection<PickupIndex> exclude = null)
        {
            return Run.instance.availableEquipmentDropList
                .Where(pickup =>
                {
                    var def = PickupCatalog.GetPickupDef(pickup);
                    return def != null && def.equipmentIndex != EquipmentIndex.None
                        && !state.BanishedEquipment.Contains(def.equipmentIndex)
                        && (exclude == null || !exclude.Contains(pickup));
                })
                .Distinct()
                .ToList();
        }

        private void RollItemsForPlayer(NetworkInstanceId netId)
        {
            if (!playerStates.TryGetValue(netId, out var state))
                return;

            state.CurrentOptions.Clear();
            state.CurrentSynergies.Clear();
            bool offerEquipment = ModConfig.EnableEquipmentChoices.Value
                && ShouldOfferEquipment(state.UsedTokens, state.PreviousRoundWasEquipment, ModConfig.EquipmentRoundChance.Value, itemRng);
            var equipmentPool = offerEquipment ? GetEquipmentPool(state) : null;
            state.IsEquipmentRound = equipmentPool?.Count > 0;
            state.OfferedEquipmentThisRound = state.IsEquipmentRound;

            if (state.IsEquipmentRound)
            {
                int choiceCount = Mathf.Max(1, ModConfig.ItemChoiceCount.Value);
                while (state.CurrentOptions.Count < choiceCount && equipmentPool.Count > 0)
                {
                    int index = itemRng.RangeInt(0, equipmentPool.Count);
                    state.CurrentOptions.Add(equipmentPool[index]);
                    state.CurrentSynergies.Add(ItemIndex.None);
                    equipmentPool.RemoveAt(index);
                }
            }
            else
            {
                RollItemOptions(netId, state, state.CurrentOptions, state.CurrentSynergies);
            }

            PublishChoices(netId, state);
        }

        private void RollItemOptions(NetworkInstanceId netId, PlayerState state, List<PickupIndex> options, List<ItemIndex> synergies)
        {
            int choiceCount = Mathf.Max(1, ModConfig.ItemChoiceCount.Value);
            float luck = GetPlayerLuck(netId);
            var excludedItems = new List<ItemIndex>();
            for (int i = 0; i < choiceCount; i++)
            {
                var (rolledItem, synergizedWith) = RollSingleSlot(itemRng, netId, state, excludedItems, luck);
                if (rolledItem == ItemIndex.None)
                    continue;

                excludedItems.Add(rolledItem);
                options.Add(PickupCatalog.FindPickupIndex(rolledItem));
                synergies.Add(synergizedWith);
            }
        }

        private void PublishChoices(NetworkInstanceId netId, PlayerState state)
        {
            SyncOptions(netId);

            // R2API delivers the message locally when the local client is active.
            if (NetworkClient.active)
                return;

            var localUsers = NetworkUser.readOnlyLocalPlayersList;
            if (localUsers.Count > 0 && localUsers[0].netId == netId)
            {
                UpdateAvailableItems(state.CurrentOptions.ToList(), state.CurrentSynergies);
            }
        }

        private void Update()
        {
            // Honor the configured shortcut even when a movement key is held.
            var shortcut = ModConfig.ToggleMenuKey.Value;
            if (Input.GetKeyDown(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey))
            {
                if (GetComponent<UI.Integration.UIPauseIntegration>().IsPaused)
                    return;

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
