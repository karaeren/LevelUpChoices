using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;

namespace LevelUpChoices
{
    // Per-player drop table backed by a Dictionary<ItemIndex, float>.
    // Weights are recalculated lazily when UsedTokens changes.
    // Banished items are permanently removed from the table.
    public class PlayerDropTable
    {
        private const int MidpointPickCount = 14;
        internal const int LateWeightPickCount = 36;

        public static readonly HashSet<string> BannedItemNames =
        [
            // Base game
            "ArtifactKey"
        ];

        // ItemIndex → individual item weight (category weight / items in tier)
        private readonly Dictionary<ItemIndex, float> _weights = [];
        // Avoids GetItemDef on every recalc
        private readonly Dictionary<ItemIndex, ItemTier> _tiers = [];
        // Maintained incrementally so RecalculateWeights never needs a count pass
        private readonly Dictionary<ItemTier, int> _tierCounts = [];
        private int _lastCalculatedTokens = -1;

        // Build the table from all valid items in ItemCatalog and compute initial weights.
        // Call once per run when the player's state is first created.
        public void Initialize(bool enableScraps = false)
        {
            _weights.Clear();
            _tiers.Clear();
            _tierCounts.Clear();
            _lastCalculatedTokens = -1;

            foreach (var index in ItemCatalog.allItems)
            {
                string skipReason = null;
                var def = ItemCatalog.GetItemDef(index);

                if (def == null)
                    skipReason = "DEF_NULL";
                if (def.hidden)
                    skipReason = "HIDDEN";
                if (def.tags.Contains(ItemTag.IgnoreForDropList))
                    skipReason = "IGNORE_FOR_DROP_LIST";
                if (!enableScraps && (def.tags.Contains(ItemTag.Scrap) || def.tags.Contains(ItemTag.PriorityScrap)))
                    skipReason = "SCRAP_ON_NON_DRIFTER";
                if (BannedItemNames.Contains(def.name) || ModConfig.IsItemBlacklisted(def.name))
                    skipReason = "BANNED";
                // Only include tiers we actually weight; skip Void, NoTier, etc.
                if (def.tier is not (ItemTier.Tier1 or ItemTier.Tier2 or ItemTier.Tier3
                                     or ItemTier.Boss or ItemTier.Lunar))
                    skipReason = "INVALID_TIER";

                // Skip items that have no valid pickup entry (common with modded items
                // that are registered in ItemCatalog but not meant to be dropped).
                if (PickupCatalog.FindPickupIndex(index) == PickupIndex.none)
                    skipReason = "NO_PICKUP_INDEX";

                bool runAvailable = false;
                bool runExists = false;
                try
                {
                    if (Run.instance != null)
                    {
                        runExists = true;
                        runAvailable = Run.instance.availableItems.Contains(index);
                    }
                }
                catch { /* Run may not be active */ }

                if (ModConfig.EnableInteractableRemoval.Value && def.name is
                    "TreasureCache" or "TreasureCacheVoid" or "FreeChest" or "ExtraShrineItem"
                    or "LowerPricedChests" or "MultiShopCard" or "PrimalBirthright"
                    or "ITEM_SANDSWEPT_HALLOWED_ICHOR" or "ITEM_SANDSWEPT_SEQUENCED_FATE" or "ITEM_SANDSWEPT_UNIVERSAL_VIP_PASS")
                    skipReason = "INTERACTABLE_REMOVAL_ENABLED";

                if (!runExists)
                    skipReason = "RUN_INSTANCE_NULL";
                else if (!runAvailable)
                    skipReason = "NOT_AVAILABLE_IN_RUN";

                if (skipReason != null)
                {
                    Log.Debug($"Skipping {def?.name ?? index.ToString()} - {skipReason}");
                    continue;
                }

                _tiers[index] = def.tier;
                _weights[index] = 0f; // assigned properly in RecalculateWeights

                if (!_tierCounts.ContainsKey(def.tier))
                    _tierCounts[def.tier] = 0;
                _tierCounts[def.tier]++;
            }

            RecalculateWeights(0);
        }

        // Update every item's weight based on the current UsedTokens value.
        // Reuse the previous weights if the pick count is unchanged or the curve is already at its end.
        public void RecalculateWeights(int usedTokens)
        {
            if (usedTokens == _lastCalculatedTokens || (usedTokens >= LateWeightPickCount && _lastCalculatedTokens >= LateWeightPickCount))
                return;
            _lastCalculatedTokens = usedTokens;

            // Reach half the late-game weighting at pick 14, then finish the curve at pick 36.
            float progress = usedTokens <= MidpointPickCount
                ? Mathf.InverseLerp(0, MidpointPickCount, usedTokens) * 0.5f
                : 0.5f + Mathf.InverseLerp(MidpointPickCount, LateWeightPickCount, usedTokens) * 0.5f;

            float w1 = Mathf.Lerp(ModConfig.EarlyWeightTier1.Value, ModConfig.LateWeightTier1.Value, progress);
            float w2 = Mathf.Lerp(ModConfig.EarlyWeightTier2.Value, ModConfig.LateWeightTier2.Value, progress);
            float w3 = Mathf.Lerp(ModConfig.EarlyWeightTier3.Value, ModConfig.LateWeightTier3.Value, progress);
            float wLunar = Mathf.Lerp(ModConfig.EarlyWeightLunar.Value, ModConfig.LateWeightLunar.Value, progress);
            float wBoss = Mathf.Lerp(ModConfig.EarlyWeightBoss.Value, ModConfig.LateWeightBoss.Value, progress);

            // Use cached tiers — no GetItemDef calls, no list allocation
            foreach (var kv in _tiers)
            {
                float tierWeight = kv.Value switch
                {
                    ItemTier.Tier1 => w1,
                    ItemTier.Tier2 => w2,
                    ItemTier.Tier3 => w3,
                    ItemTier.Boss => wBoss,
                    ItemTier.Lunar => wLunar,
                    _ => 0f
                };

                int count = _tierCounts.TryGetValue(kv.Value, out int c) ? c : 0;
                _weights[kv.Key] = count > 0 ? tierWeight / count : 0f;
            }
        }

        // Permanently remove an item from the table (banish) and immediately
        // redistribute its tier's weight across remaining items.
        public void Remove(ItemIndex item)
        {
            if (!_tiers.TryGetValue(item, out var tier))
                return;

            _tiers.Remove(item);
            _weights.Remove(item);
            _tierCounts[tier]--;

            // Force immediate redistribution with the same token count
            int tokens = _lastCalculatedTokens;
            _lastCalculatedTokens = -1;
            RecalculateWeights(tokens < 0 ? 0 : tokens);
        }

        // Pick a random item by weight.
        public bool CanDrop(ItemIndex item)
        {
            return _weights.TryGetValue(item, out float w) && w > 0f;
        }

        public bool HasEligibleItems(ICollection<ItemIndex> exclude = null)
        {
            return _weights.Any(kv => kv.Value > 0f && (exclude == null || !exclude.Contains(kv.Key)));
        }

        public ItemIndex Roll(Xoroshiro128Plus rng, float luck = 0f, ICollection<ItemIndex> exclude = null)
        {
            int extraRolls = Mathf.FloorToInt(Mathf.Abs(luck));
            if (rng.nextNormalizedFloat < Mathf.Abs(luck) - extraRolls)
            {
                extraRolls++;
            }
            int rolls = 1 + extraRolls;

            ItemIndex bestResult = ItemIndex.None;
            int bestTierValue = luck >= 0 ? int.MinValue : int.MaxValue;

            for (int i = 0; i < rolls; i++)
            {
                ItemIndex roll = RollSingle(rng, exclude);
                if (roll == ItemIndex.None)
                    continue;

                int tierValue = GetTierValue(_tiers[roll]);

                if (luck >= 0)
                {
                    if (tierValue > bestTierValue)
                    {
                        bestTierValue = tierValue;
                        bestResult = roll;
                    }
                }
                else
                {
                    if (tierValue < bestTierValue)
                    {
                        bestTierValue = tierValue;
                        bestResult = roll;
                    }
                }
            }

            return bestResult != ItemIndex.None ? bestResult : RollSingle(rng, exclude);
        }

        private ItemIndex RollSingle(Xoroshiro128Plus rng, ICollection<ItemIndex> exclude = null)
        {
            // Compute total weight of eligible items
            float total = 0f;
            foreach (var kv in _weights)
            {
                if (exclude != null && exclude.Contains(kv.Key))
                    continue;
                total += kv.Value;
            }

            if (total <= 0f)
            {
                Log.Warning("No eligible items to roll!");
                return ItemIndex.None;
            }

            float roll = rng.nextNormalizedFloat * total;
            float cumulative = 0f;
            ItemIndex last = ItemIndex.None;

            foreach (var kv in _weights)
            {
                if (exclude != null && exclude.Contains(kv.Key))
                    continue;
                cumulative += kv.Value;
                last = kv.Key;
                if (roll <= cumulative)
                    return kv.Key;
            }

            // Floating-point edge case: return last valid item
            return last;
        }

        private int GetTierValue(ItemTier tier)
        {
            return tier switch
            {
                ItemTier.Tier1 => 1,
                ItemTier.Tier2 => 2,
                ItemTier.Tier3 => 3,
                ItemTier.Boss => 4,
                ItemTier.Lunar => 4,
                ItemTier.VoidTier1 => 1,
                ItemTier.VoidTier2 => 2,
                ItemTier.VoidTier3 => 3,
                ItemTier.VoidBoss => 4,
                _ => 0
            };
        }
    }
}
