using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;

namespace LevelUpChoices
{
    /// Per-player drop table backed by a Dictionary<ItemIndex, float>.
    /// Weights are recalculated lazily when UsedTokens changes.
    /// Banished items are permanently removed from the table.
    public class PlayerDropTable
    {
        private static readonly HashSet<string> BannedItemNames = new HashSet<string>
        {
            "TreasureCache"
        };

        // ItemIndex → individual item weight (category weight / items in tier)
        private readonly Dictionary<ItemIndex, float> _weights = new Dictionary<ItemIndex, float>();
        // Avoids GetItemDef on every recalc
        private readonly Dictionary<ItemIndex, ItemTier> _tiers = new Dictionary<ItemIndex, ItemTier>();
        // Maintained incrementally so RecalculateWeights never needs a count pass
        private readonly Dictionary<ItemTier, int> _tierCounts = new Dictionary<ItemTier, int>();
        private int _lastCalculatedTokens = -1;

        /// Build the table from all valid items in ItemCatalog and compute initial weights.
        /// Call once per run when the player's state is first created.
        public void Initialize()
        {
            _weights.Clear();
            _tiers.Clear();
            _tierCounts.Clear();
            _lastCalculatedTokens = -1;

            foreach (var index in ItemCatalog.allItems)
            {
                var def = ItemCatalog.GetItemDef(index);
                if (def == null) continue;
                if (def.hidden) continue;
                if (def.tags.Contains(ItemTag.IgnoreForDropList)) continue;
                if (BannedItemNames.Contains(def.name)) continue;
                // Only include tiers we actually weight; skip Void, NoTier, etc.
                if (def.tier is not (ItemTier.Tier1 or ItemTier.Tier2 or ItemTier.Tier3
                                     or ItemTier.Boss or ItemTier.Lunar)) continue;

                _tiers[index] = def.tier;
                _weights[index] = 0f; // assigned properly in RecalculateWeights

                if (!_tierCounts.ContainsKey(def.tier)) _tierCounts[def.tier] = 0;
                _tierCounts[def.tier]++;
            }

            RecalculateWeights(0);
        }

        /// Update every item's weight based on the current UsedTokens value.
        /// Skips recalculation if tokens &gt; 30 and weights are already at the cap,
        /// or if UsedTokens hasn't changed since last call.
        public void RecalculateWeights(int usedTokens)
        {
            if (usedTokens == _lastCalculatedTokens) return;
            if (usedTokens > 30 && _lastCalculatedTokens >= 30) return;
            _lastCalculatedTokens = usedTokens;

            // --- Two-phase lerp ---
            // At 0:   T1=79   T2=15   T3=4    Lunar=1   Boss=1
            // At 10:  T1=55   T2=34   T3=8    Lunar=2   Boss=1
            // At 20:  T1=37.5 T2=37.5 T3=14.5 Lunar=6   Boss=4.5
            // At 30+: T1=20   T2=41   T3=21   Lunar=10  Boss=8
            float t1to2 = Mathf.Clamp01(usedTokens / 10f);
            float t2to3 = Mathf.Clamp01((usedTokens - 10f) / 20f);

            float w1 = Mathf.Lerp(79f, Mathf.Lerp(55f, 20f, t2to3), t1to2);
            float w2 = Mathf.Lerp(15f, Mathf.Lerp(34f, 41f, t2to3), t1to2);
            float w3 = Mathf.Lerp(4f, Mathf.Lerp(8f, 21f, t2to3), t1to2);
            float wLunar = Mathf.Lerp(1f, Mathf.Lerp(2f, 10f, t2to3), t1to2);
            float wBoss = Mathf.Lerp(1f, Mathf.Lerp(1f, 8f, t2to3), t1to2);

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

                int count = _tierCounts.TryGetValue(kv.Value, out int c) ? c : 1;
                _weights[kv.Key] = count > 0 ? tierWeight / count : 0f;
            }
        }

        /// Permanently remove an item from the table (banish) and immediately
        /// redistribute its tier's weight across remaining items.
        public void Remove(ItemIndex item)
        {
            if (!_tiers.TryGetValue(item, out var tier)) return;

            _tiers.Remove(item);
            _weights.Remove(item);
            _tierCounts[tier]--;

            // Force immediate redistribution with the same token count
            int tokens = _lastCalculatedTokens;
            _lastCalculatedTokens = -1;
            RecalculateWeights(tokens < 0 ? 0 : tokens);
        }

        /// Pick a random item by weight, excluding any indices in <paramref name="exclude"/>.
        public ItemIndex Roll(ICollection<ItemIndex> exclude = null)
        {
            // Compute total weight of eligible items
            float total = 0f;
            foreach (var kv in _weights)
            {
                if (exclude != null && exclude.Contains(kv.Key)) continue;
                total += kv.Value;
            }

            if (total <= 0f)
            {
                Log.Warning("PlayerDropTable: no eligible items to roll!");
                return ItemIndex.None;
            }

            float roll = Random.value * total;
            float cumulative = 0f;
            ItemIndex last = ItemIndex.None;

            foreach (var kv in _weights)
            {
                if (exclude != null && exclude.Contains(kv.Key)) continue;
                cumulative += kv.Value;
                last = kv.Key;
                if (roll <= cumulative)
                    return kv.Key;
            }

            // Floating-point edge case: return last valid item
            return last;
        }
    }
}
