using System.Collections.Generic;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine.Networking;

namespace LevelUpChoices
{
    public class Networking
    {
        public class SyncPlayerState : INetMessage
        {
            NetworkInstanceId targetNetId;
            int selectionTokens;
            int banishTokens;
            int rerollTokens;

            public SyncPlayerState() { }
            public SyncPlayerState(NetworkInstanceId targetNetId, int selectionTokens, int banishTokens, int rerollTokens)
            {
                this.targetNetId = targetNetId;
                this.selectionTokens = selectionTokens;
                this.banishTokens = banishTokens;
                this.rerollTokens = rerollTokens;
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(targetNetId);
                writer.Write(selectionTokens);
                writer.Write(banishTokens);
                writer.Write(rerollTokens);
            }

            public void Deserialize(NetworkReader reader)
            {
                targetNetId = reader.ReadNetworkId();
                selectionTokens = reader.ReadInt32();
                banishTokens = reader.ReadInt32();
                rerollTokens = reader.ReadInt32();
            }

            public void OnReceived()
            {
                if (RoR2.NetworkUser.readOnlyLocalPlayersList.Count > 0 && RoR2.NetworkUser.readOnlyLocalPlayersList[0].netId == targetNetId)
                {
                    LevelUpManager.Instance.UpdatePlayerState(selectionTokens, banishTokens, rerollTokens);
                }
            }
        }

        public class SyncItems : INetMessage
        {
            List<PickupIndex> pickupIndices;
            List<ItemIndex> synergizedItems;
            NetworkInstanceId targetNetId;

            public SyncItems() { }
            public SyncItems(NetworkInstanceId targetNetId, List<PickupIndex> pickupIndices, List<ItemIndex> synergizedItems)
            {
                this.targetNetId = targetNetId;
                this.pickupIndices = pickupIndices;
                this.synergizedItems = synergizedItems ?? new List<ItemIndex>(new ItemIndex[pickupIndices.Count]);
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(targetNetId);
                writer.Write(pickupIndices.Count);
                for (int i = 0; i < pickupIndices.Count; i++)
                {
                    writer.Write(pickupIndices[i]);
                    writer.Write(synergizedItems != null && i < synergizedItems.Count ? (int)synergizedItems[i] : (int)ItemIndex.None);
                }
            }

            public void Deserialize(NetworkReader reader)
            {
                targetNetId = reader.ReadNetworkId();
                pickupIndices = [];
                synergizedItems = [];
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    pickupIndices.Add(reader.ReadPickupIndex());
                    synergizedItems.Add((ItemIndex)reader.ReadInt32());
                }
            }

            public void OnReceived()
            {
                if (RoR2.NetworkUser.readOnlyLocalPlayersList.Count > 0 && RoR2.NetworkUser.readOnlyLocalPlayersList[0].netId == targetNetId)
                {
                    LevelUpManager.Instance.UpdateAvailableItems(pickupIndices, synergizedItems);
                }
            }
        }

        public class SendItemSelection : INetMessage
        {
            PickupIndex pickupIndex;
            NetworkInstanceId netId;

            public SendItemSelection() { }
            public SendItemSelection(PickupIndex pickupIndex, NetworkInstanceId netId)
            {
                this.pickupIndex = pickupIndex;
                this.netId = netId;
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(pickupIndex);
                writer.Write(netId);
            }

            public void Deserialize(NetworkReader reader)
            {
                pickupIndex = reader.ReadPickupIndex();
                netId = reader.ReadNetworkId();
            }

            public void OnReceived()
            {
                if (NetworkServer.active)
                {
                    LevelUpManager.Instance.HandlePlayerSelection(netId, pickupIndex);
                }
            }
        }

        public class SendBanish : INetMessage
        {
            int slotIndex;
            NetworkInstanceId netId;

            public SendBanish() { }
            public SendBanish(int slotIndex, NetworkInstanceId netId)
            {
                this.slotIndex = slotIndex;
                this.netId = netId;
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(slotIndex);
                writer.Write(netId);
            }

            public void Deserialize(NetworkReader reader)
            {
                slotIndex = reader.ReadInt32();
                netId = reader.ReadNetworkId();
            }

            public void OnReceived()
            {
                if (NetworkServer.active)
                {
                    LevelUpManager.Instance.HandlePlayerBanish(netId, slotIndex);
                }
            }
        }

        public class SendReroll : INetMessage
        {
            int slotIndex;
            NetworkInstanceId netId;

            public SendReroll() { }
            public SendReroll(int slotIndex, NetworkInstanceId netId)
            {
                this.slotIndex = slotIndex;
                this.netId = netId;
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(slotIndex);
                writer.Write(netId);
            }

            public void Deserialize(NetworkReader reader)
            {
                slotIndex = reader.ReadInt32();
                netId = reader.ReadNetworkId();
            }

            public void OnReceived()
            {
                if (NetworkServer.active)
                {
                    LevelUpManager.Instance.HandlePlayerReroll(netId, slotIndex);
                }
            }
        }

        public class SendPickingState : INetMessage
        {
            NetworkInstanceId netId;
            bool isPicking;

            public SendPickingState() { }
            public SendPickingState(NetworkInstanceId netId, bool isPicking)
            {
                this.netId = netId;
                this.isPicking = isPicking;
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(netId);
                writer.Write(isPicking);
            }

            public void Deserialize(NetworkReader reader)
            {
                netId = reader.ReadNetworkId();
                isPicking = reader.ReadBoolean();
            }

            public void OnReceived()
            {
                if (NetworkServer.active)
                {
                    GamePauseManager.HandlePickingState(netId, isPicking);
                }
            }
        }
    }
}
