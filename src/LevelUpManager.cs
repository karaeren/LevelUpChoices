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
            public List<ItemIndex> CurrentOptions = new List<ItemIndex>();
            public PlayerDropTable DropTable = new PlayerDropTable();
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
            if (!NetworkServer.active) return;

            foreach (var player in PlayerCharacterMasterController.instances)
            {
                if (player.networkUser)
                {
                    var netId = player.networkUser.netId;
                    if (!playerStates.ContainsKey(netId))
                    {
                        playerStates[netId] = new PlayerState();
                        playerStates[netId].DropTable.Initialize();
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
                var pickups = playerStates[netId].CurrentOptions
                    .Select(i => PickupCatalog.FindPickupIndex(i))
                    .ToList();
                new Networking.SyncItems(netId, pickups).Send(NetworkDestination.Clients);
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
            state.DropTable.RecalculateWeights(state.UsedTokens);

            state.RerollTokens = 1;

            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                if (pcmc.networkUser && pcmc.networkUser.netId == netId)
                {
                    var master = pcmc.master;
                    if (master && master.inventory)
                    {
                        master.inventory.GiveItemPermanent(PickupCatalog.GetPickupDef(selection).itemIndex);
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

            ItemIndex itemToBanish = state.CurrentOptions[slotIndex];
            state.BanishTokens--;
            state.DropTable.Remove(itemToBanish);

            var banishExclude = new List<ItemIndex>(state.CurrentOptions);
            banishExclude.RemoveAt(slotIndex);
            state.CurrentOptions[slotIndex] = state.DropTable.Roll(banishExclude);

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

            var rerollExclude = new List<ItemIndex>(state.CurrentOptions);
            rerollExclude.RemoveAt(slotIndex);
            state.CurrentOptions[slotIndex] = state.DropTable.Roll(rerollExclude);

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
                state.CurrentOptions.Add(state.DropTable.Roll(state.CurrentOptions));
            }

            SyncOptions(netId);

            if (NetworkUser.readOnlyLocalPlayersList.Count > 0 && NetworkUser.readOnlyLocalPlayersList[0].netId == netId)
            {
                UpdateAvailableItems(state.CurrentOptions.Select(i => PickupCatalog.FindPickupIndex(i)).ToList());
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
