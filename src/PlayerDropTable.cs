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
                if (BannedItemNames.Contains(def.name))
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

                if (ModConfig.EnableInteractableRemoval.Value && (
                    def.name == "TreasureCache" || def.name == "ExtraShrineItem"
                    || def.name == "LowerPricedChests" || def.name == "MultiShopCard"
                    || def.name == "ITEM_SANDSWEPT_HALLOWED_ICHOR" || def.name == "ITEM_SANDSWEPT_SEQUENCED_FATE"
                    || def.name == "ITEM_SANDSWEPT_UNIVERSAL_VIP_PASS" || def.name == "PrimalBirthright"
                    ))
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
        // Skips recalculation if tokens>30 (late game) and weights are already at the cap,
        // or if UsedTokens hasn't changed since last call.
        public void RecalculateWeights(int usedTokens)
        {
            if (usedTokens == _lastCalculatedTokens)
                return;
            if (usedTokens > 30 && _lastCalculatedTokens >= 30)
                return;
            _lastCalculatedTokens = usedTokens;

            // --- Two-phase lerp (early → late configurable weights) ---
            float t1to2 = Mathf.Clamp01(usedTokens / 10f);
            float t2to3 = Mathf.Clamp01((usedTokens - 10f) / 20f);

            float earlyT1 = ModConfig.EarlyWeightTier1.Value;
            float earlyT2 = ModConfig.EarlyWeightTier2.Value;
            float earlyT3 = ModConfig.EarlyWeightTier3.Value;
            float earlyLunar = ModConfig.EarlyWeightLunar.Value;
            float earlyBoss = ModConfig.EarlyWeightBoss.Value;

            float lateT1 = ModConfig.LateWeightTier1.Value;
            float lateT2 = ModConfig.LateWeightTier2.Value;
            float lateT3 = ModConfig.LateWeightTier3.Value;
            float lateLunar = ModConfig.LateWeightLunar.Value;
            float lateBoss = ModConfig.LateWeightBoss.Value;

            // Mid-point is the average of early and late
            float midT1 = (earlyT1 + lateT1) * 0.5f;
            float midT2 = (earlyT2 + lateT2) * 0.5f;
            float midT3 = (earlyT3 + lateT3) * 0.5f;
            float midLunar = (earlyLunar + lateLunar) * 0.5f;
            float midBoss = (earlyBoss + lateBoss) * 0.5f;

            float w1 = Mathf.Lerp(earlyT1, Mathf.Lerp(midT1, lateT1, t2to3), t1to2);
            float w2 = Mathf.Lerp(earlyT2, Mathf.Lerp(midT2, lateT2, t2to3), t1to2);
            float w3 = Mathf.Lerp(earlyT3, Mathf.Lerp(midT3, lateT3, t2to3), t1to2);
            float wLunar = Mathf.Lerp(earlyLunar, Mathf.Lerp(midLunar, lateLunar, t2to3), t1to2);
            float wBoss = Mathf.Lerp(earlyBoss, Mathf.Lerp(midBoss, lateBoss, t2to3), t1to2);

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

        public ItemIndex Roll(float luck = 0f, ICollection<ItemIndex> exclude = null)
        {
            int extraRolls = Mathf.FloorToInt(Mathf.Abs(luck));
            if (Random.value < Mathf.Abs(luck) - extraRolls)
            {
                extraRolls++;
            }
            int rolls = 1 + extraRolls;

            ItemIndex bestResult = ItemIndex.None;
            int bestTierValue = luck >= 0 ? int.MinValue : int.MaxValue;

            for (int i = 0; i < rolls; i++)
            {
                ItemIndex roll = RollSingle(exclude);
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

            return bestResult != ItemIndex.None ? bestResult : RollSingle(exclude);
        }

        private ItemIndex RollSingle(ICollection<ItemIndex> exclude = null)
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

            float roll = Random.value * total;
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
