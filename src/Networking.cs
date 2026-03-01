using System.Collections.Generic;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine.Networking;

namespace LevelUpChoices {
    public class Networking {
        public class SyncPlayerState : INetMessage {
            private NetworkInstanceId _targetNetId;
            private int _selectionTokens;
            private int _banishTokens;
            private int _rerollTokens;

            public SyncPlayerState() { }
            public SyncPlayerState(NetworkInstanceId targetNetId, int selectionTokens, int banishTokens, int rerollTokens) {
                _targetNetId = targetNetId;
                _selectionTokens = selectionTokens;
                _banishTokens = banishTokens;
                _rerollTokens = rerollTokens;
            }

            public void Serialize(NetworkWriter writer) {
                writer.Write(_targetNetId);
                writer.Write(_selectionTokens);
                writer.Write(_banishTokens);
                writer.Write(_rerollTokens);
            }

            public void Deserialize(NetworkReader reader) {
                _targetNetId = reader.ReadNetworkId();
                _selectionTokens = reader.ReadInt32();
                _banishTokens = reader.ReadInt32();
                _rerollTokens = reader.ReadInt32();
            }

            public void OnReceived() {
                if (RoR2.NetworkUser.readOnlyLocalPlayersList.Count > 0 && RoR2.NetworkUser.readOnlyLocalPlayersList[0].netId == _targetNetId) {
                    LevelUpManager.Instance.UpdatePlayerState(_selectionTokens, _banishTokens, _rerollTokens);
                }
            }
        }

        public class SyncItems : INetMessage {
            private List<PickupIndex> _pickupIndices;
            private List<ItemIndex> _synergizedItems;
            private NetworkInstanceId _targetNetId;

            public SyncItems() { }
            public SyncItems(NetworkInstanceId targetNetId, List<PickupIndex> pickupIndices, List<ItemIndex> synergizedItems) {
                _targetNetId = targetNetId;
                _pickupIndices = pickupIndices;
                _synergizedItems = synergizedItems ?? [.. new ItemIndex[pickupIndices.Count]];
            }

            public void Serialize(NetworkWriter writer) {
                writer.Write(_targetNetId);
                writer.Write(_pickupIndices.Count);
                for (int i = 0; i < _pickupIndices.Count; i++) {
                    writer.Write(_pickupIndices[i]);
                    writer.Write(_synergizedItems != null && i < _synergizedItems.Count ? (int)_synergizedItems[i] : (int)ItemIndex.None);
                }
            }

            public void Deserialize(NetworkReader reader) {
                _targetNetId = reader.ReadNetworkId();
                _pickupIndices = [];
                _synergizedItems = [];
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++) {
                    _pickupIndices.Add(reader.ReadPickupIndex());
                    _synergizedItems.Add((ItemIndex)reader.ReadInt32());
                }
            }

            public void OnReceived() {
                if (RoR2.NetworkUser.readOnlyLocalPlayersList.Count > 0 && RoR2.NetworkUser.readOnlyLocalPlayersList[0].netId == _targetNetId) {
                    LevelUpManager.Instance.UpdateAvailableItems(_pickupIndices, _synergizedItems);
                }
            }
        }

        public class SendItemSelection : INetMessage {
            private PickupIndex _pickupIndex;
            private NetworkInstanceId _netId;

            public SendItemSelection() { }
            public SendItemSelection(PickupIndex pickupIndex, NetworkInstanceId netId) {
                _pickupIndex = pickupIndex;
                _netId = netId;
            }

            public void Serialize(NetworkWriter writer) {
                writer.Write(_pickupIndex);
                writer.Write(_netId);
            }

            public void Deserialize(NetworkReader reader) {
                _pickupIndex = reader.ReadPickupIndex();
                _netId = reader.ReadNetworkId();
            }

            public void OnReceived() {
                if (NetworkServer.active) {
                    LevelUpManager.Instance.HandlePlayerSelection(_netId, _pickupIndex);
                }
            }
        }

        public class SendBanish : INetMessage {
            private int _slotIndex;
            private NetworkInstanceId _netId;

            public SendBanish() { }
            public SendBanish(int slotIndex, NetworkInstanceId netId) {
                _slotIndex = slotIndex;
                _netId = netId;
            }

            public void Serialize(NetworkWriter writer) {
                writer.Write(_slotIndex);
                writer.Write(_netId);
            }

            public void Deserialize(NetworkReader reader) {
                _slotIndex = reader.ReadInt32();
                _netId = reader.ReadNetworkId();
            }

            public void OnReceived() {
                if (NetworkServer.active) {
                    LevelUpManager.Instance.HandlePlayerBanish(_netId, _slotIndex);
                }
            }
        }

        public class SendReroll : INetMessage {
            private int _slotIndex;
            private NetworkInstanceId _netId;

            public SendReroll() { }
            public SendReroll(int slotIndex, NetworkInstanceId netId) {
                _slotIndex = slotIndex;
                _netId = netId;
            }

            public void Serialize(NetworkWriter writer) {
                writer.Write(_slotIndex);
                writer.Write(_netId);
            }

            public void Deserialize(NetworkReader reader) {
                _slotIndex = reader.ReadInt32();
                _netId = reader.ReadNetworkId();
            }

            public void OnReceived() {
                if (NetworkServer.active) {
                    LevelUpManager.Instance.HandlePlayerReroll(_netId, _slotIndex);
                }
            }
        }

        public class SendPickingState : INetMessage {
            private NetworkInstanceId _netId;
            private bool _isPicking;

            public SendPickingState() { }
            public SendPickingState(NetworkInstanceId netId, bool isPicking) {
                _netId = netId;
                _isPicking = isPicking;
            }

            public void Serialize(NetworkWriter writer) {
                writer.Write(_netId);
                writer.Write(_isPicking);
            }

            public void Deserialize(NetworkReader reader) {
                _netId = reader.ReadNetworkId();
                _isPicking = reader.ReadBoolean();
            }

            public void OnReceived() {
                if (NetworkServer.active) {
                    GamePauseManager.HandlePickingState(_netId, _isPicking);
                }
            }
        }

        public class SyncConfig : INetMessage {
            private int _maxLevel;
            private bool _enableMonsterLevelScaling;
            private bool _enableCustomLevelSystem;

            public SyncConfig() { }
            public SyncConfig(int maxLevel, bool enableMonsterLevelScaling, bool enableCustomLevelSystem) {
                _maxLevel = maxLevel;
                _enableMonsterLevelScaling = enableMonsterLevelScaling;
                _enableCustomLevelSystem = enableCustomLevelSystem;
            }

            public void Serialize(NetworkWriter writer) {
                writer.Write(_maxLevel);
                writer.Write(_enableMonsterLevelScaling);
                writer.Write(_enableCustomLevelSystem);
            }

            public void Deserialize(NetworkReader reader) {
                _maxLevel = reader.ReadInt32();
                _enableMonsterLevelScaling = reader.ReadBoolean();
                _enableCustomLevelSystem = reader.ReadBoolean();
            }

            public void OnReceived() {
                if (NetworkServer.active)
                    return;
                ModConfig.UpdateServerConfig(_maxLevel, _enableMonsterLevelScaling, _enableCustomLevelSystem);
            }
        }
    }
}
