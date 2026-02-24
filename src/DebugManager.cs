using System;
using System.IO;
using System.Linq;
using System.Text;
using LevelUpChoices.Extensions;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace LevelUpChoices
{
    public class DebugManager : MonoBehaviour
    {
        private bool CheckDebugEnabled()
        {
            if (ModConfig.DebugPassword == null)
                return false;
            // Stupid check to avoid accidentally leaving debug code enabled
            // because i didn't want to handle debug/release builds...
            if (ModConfig.DebugPassword.Value != 3169)
                return false;
            return true;
        }

        private void Update()
        {
            try
            {

                if (Input.GetKeyDown(KeyCode.F2))
                {
                    if (!CheckDebugEnabled())
                        return;

                    if (NetworkServer.active)
                    {
                        TeamManager.instance.GiveTeamExperience(TeamIndex.Player, (TeamManager.instance.GetTeamNextLevelExperience(TeamIndex.Player) - TeamManager.instance.GetTeamCurrentLevelExperience(TeamIndex.Player)) / 6);
                        Log.Info("Granted debug XP");
                    }
                }
                else if (Input.GetKeyDown(KeyCode.F4))
                {
                    if (!CheckDebugEnabled())
                        return;

                    DumpAllItemsToJson();
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        // Writes a comprehensive JSON diagnostic dump of every item and equipment in the game.
        private static void DumpAllItemsToJson()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("{");

                // ========== METADATA ==========
                sb.AppendLine("  \"_metadata\": {");
                sb.AppendLine($"    \"dumpTime\": \"{Esc(System.DateTime.UtcNow.ToString("o"))}\",");
                sb.AppendLine($"    \"itemCatalogCount\": {ItemCatalog.itemCount},");
                sb.AppendLine($"    \"pickupCatalogCount\": {PickupCatalog.pickupCount},");
                sb.AppendLine($"    \"equipmentCatalogCount\": {EquipmentCatalog.equipmentCount}");
                sb.AppendLine("  },");

                // ========== ITEM TIER DEFS ==========
                sb.AppendLine("  \"itemTierDefs\": [");
                bool firstTier = true;
                var allTiers = new[] {
                    ItemTier.Tier1, ItemTier.Tier2, ItemTier.Tier3,
                    ItemTier.Lunar, ItemTier.Boss, ItemTier.NoTier,
                    ItemTier.VoidTier1, ItemTier.VoidTier2, ItemTier.VoidTier3,
                    ItemTier.VoidBoss, ItemTier.AssignedAtRuntime
                };
                foreach (var tier in allTiers)
                {
                    if (!firstTier)
                        sb.AppendLine(",");
                    firstTier = false;
                    var td = ItemTierCatalog.GetItemTierDef(tier);
                    sb.AppendLine("    {");
                    sb.AppendLine($"      \"tier\": \"{tier}\",");
                    sb.AppendLine($"      \"tierDefExists\": {Bool(td != null)},");
                    if (td != null)
                    {
                        sb.AppendLine($"      \"isDroppable\": {Bool(td.isDroppable)},");
                        sb.AppendLine($"      \"canScrap\": {Bool(td.canScrap)},");
                        sb.AppendLine($"      \"canRestack\": {Bool(td.canRestack)},");
                        sb.AppendLine($"      \"colorIndex\": \"{td.colorIndex}\",");
                        sb.AppendLine($"      \"darkColorIndex\": \"{td.darkColorIndex}\",");
                        sb.AppendLine($"      \"pickupRules\": \"{td.pickupRules}\",");
                        sb.AppendLine($"      \"bgIconTexture\": \"{Esc(td.bgIconTexture != null ? td.bgIconTexture.name : "null")}\",");
                        sb.AppendLine($"      \"highlightPrefab\": \"{Esc(td.highlightPrefab != null ? td.highlightPrefab.name : "null")}\",");
                        sb.AppendLine($"      \"dropletDisplayPrefab\": \"{Esc(td.dropletDisplayPrefab != null ? td.dropletDisplayPrefab.name : "null")}\"");
                    }
                    else
                    {
                        sb.AppendLine($"      \"_note\": \"ItemTierDef is null for this tier\"");
                    }
                    sb.Append("    }");
                }
                sb.AppendLine();
                sb.AppendLine("  ],");

                // ========== ALL ITEMS ==========
                sb.AppendLine("  \"items\": [");
                bool firstItem = true;
                foreach (var index in ItemCatalog.allItems)
                {
                    if (!firstItem)
                        sb.AppendLine(",");
                    firstItem = false;

                    var def = ItemCatalog.GetItemDef(index);
                    bool isNull = def == null;

                    sb.AppendLine("    {");
                    sb.AppendLine($"      \"itemIndex\": {(int)index},");
                    sb.AppendLine($"      \"itemDefExists\": {Bool(!isNull)},");

                    if (isNull)
                    {
                        sb.AppendLine($"      \"skipReason\": \"DEF_NULL\"");
                        sb.Append("    }");
                        continue;
                    }

                    // --- ItemDef core ---
                    sb.AppendLine($"      \"name\": \"{Esc(def.name)}\",");
                    sb.AppendLine($"      \"nameToken\": \"{Esc(def.nameToken)}\",");
                    sb.AppendLine($"      \"displayName\": \"{Esc(SafeGetString(def.nameToken))}\",");
                    sb.AppendLine($"      \"pickupToken\": \"{Esc(def.pickupToken)}\",");
                    sb.AppendLine($"      \"pickupDescription\": \"{Esc(SafeGetString(def.pickupToken))}\",");
                    sb.AppendLine($"      \"descriptionToken\": \"{Esc(def.descriptionToken)}\",");
                    sb.AppendLine($"      \"fullDescription\": \"{Esc(SafeGetString(Integrations.lookingGlassEnabled ? LookingGlassIntegration.GetItemDescription(def, 0, null, false) : def.descriptionToken))}\",");
                    sb.AppendLine($"      \"loreToken\": \"{Esc(def.loreToken)}\",");

                    // --- Tier info ---
                    sb.AppendLine($"      \"tier\": \"{def.tier}\",");
                    var tierDef = ItemTierCatalog.GetItemTierDef(def.tier);
                    sb.AppendLine($"      \"tierDef\": {{");
                    if (tierDef != null)
                    {
                        sb.AppendLine($"        \"isDroppable\": {Bool(tierDef.isDroppable)},");
                        sb.AppendLine($"        \"canScrap\": {Bool(tierDef.canScrap)},");
                        sb.AppendLine($"        \"canRestack\": {Bool(tierDef.canRestack)},");
                        sb.AppendLine($"        \"pickupRules\": \"{tierDef.pickupRules}\"");
                    }
                    else
                    {
                        sb.AppendLine($"        \"_note\": \"null\"");
                    }
                    sb.AppendLine($"      }},");

                    // --- Flags ---
                    sb.AppendLine($"      \"hidden\": {Bool(def.hidden)},");
                    sb.AppendLine($"      \"canRemove\": {Bool(def.canRemove)},");

                    // --- Tags ---
                    string tagsStr = def.tags != null
                        ? string.Join(", ", def.tags)
                        : "null";
                    sb.AppendLine($"      \"tags\": \"{Esc(tagsStr)}\",");
                    sb.AppendLine($"      \"tagsArray\": [{(def.tags != null ? string.Join(", ", def.tags.Select(t => $"\"{t}\"")) : "")}],");

                    // --- Prefabs / model paths ---
                    sb.AppendLine($"      \"pickupIconSprite\": \"{Esc(def.pickupIconSprite != null ? def.pickupIconSprite.name : "null")}\",");

                    // --- Unlock / Expansion ---
                    sb.AppendLine($"      \"unlockableDef\": \"{Esc(def.unlockableDef != null ? def.unlockableDef.cachedName : "null")}\",");
                    sb.AppendLine($"      \"requiredExpansion\": \"{Esc(def.requiredExpansion != null ? def.requiredExpansion.name : "null")}\",");

                    // --- PickupCatalog info ---
                    var pickupIdx = PickupCatalog.FindPickupIndex(index);
                    bool hasPickup = pickupIdx != PickupIndex.none;
                    sb.AppendLine($"      \"pickup\": {{");
                    sb.AppendLine($"        \"pickupIndex\": {pickupIdx.value},");
                    sb.AppendLine($"        \"hasPickup\": {Bool(hasPickup)},");
                    if (hasPickup)
                    {
                        var pdef = PickupCatalog.GetPickupDef(pickupIdx);
                        if (pdef != null)
                        {
                            sb.AppendLine($"        \"internalName\": \"{Esc(pdef.internalName)}\",");
                            sb.AppendLine($"        \"nameToken\": \"{Esc(pdef.nameToken)}\",");
                            sb.AppendLine($"        \"interactContextToken\": \"{Esc(pdef.interactContextToken)}\",");
                            sb.AppendLine($"        \"isLunar\": {Bool(pdef.isLunar)},");
                            sb.AppendLine($"        \"isBoss\": {Bool(pdef.isBoss)},");
                            sb.AppendLine($"        \"itemIndex\": {(int)pdef.itemIndex},");
                            sb.AppendLine($"        \"itemTier\": \"{pdef.itemTier}\",");
                            sb.AppendLine($"        \"equipmentIndex\": {(int)pdef.equipmentIndex},");
                            sb.AppendLine($"        \"coinValue\": {pdef.coinValue},");
                            sb.AppendLine($"        \"baseColor\": \"{Esc(pdef.baseColor.ToString())}\",");
                            sb.AppendLine($"        \"darkColor\": \"{Esc(pdef.darkColor.ToString())}\",");
                            sb.AppendLine($"        \"iconSprite\": \"{Esc(pdef.iconSprite != null ? pdef.iconSprite.name : "null")}\",");
                            sb.AppendLine($"        \"displayPrefab\": \"{Esc(pdef.displayPrefab != null ? pdef.displayPrefab.name : "null")}\",");
                            sb.AppendLine($"        \"dropletDisplayPrefab\": \"{Esc(pdef.dropletDisplayPrefab != null ? pdef.dropletDisplayPrefab.name : "null")}\"");
                        }
                        else
                        {
                            sb.AppendLine($"        \"_note\": \"PickupDef is null despite valid PickupIndex\"");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"        \"_note\": \"no pickup entry\"");
                    }
                    sb.AppendLine($"      }},");

                    // --- Run availability ---
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
                    sb.AppendLine($"      \"run\": {{");
                    sb.AppendLine($"        \"runInstanceExists\": {Bool(runExists)},");
                    sb.AppendLine($"        \"isAvailableInRun\": {Bool(runAvailable)}");
                    sb.AppendLine($"      }},");

                    // --- Filter analysis (what LevelUpChoices does) ---
                    bool isBanned = PlayerDropTable.BannedItemNames.Contains(def.name);
                    bool ignoreForDL = def.tags != null && def.tags.Contains(ItemTag.IgnoreForDropList);
                    bool isAllowedTier = def.tier is (
                        ItemTier.Tier1 or ItemTier.Tier2 or ItemTier.Tier3
                        or ItemTier.Boss or ItemTier.Lunar);

                    string skipReason = "INCLUDED";
                    if (def.hidden)
                        skipReason = "HIDDEN";
                    else if (ignoreForDL)
                        skipReason = "IGNORE_FOR_DROP_LIST";
                    else if (isBanned)
                        skipReason = "BANNED";
                    else if (!isAllowedTier)
                        skipReason = "INVALID_TIER";
                    else if (!hasPickup)
                        skipReason = "NO_PICKUP_INDEX";
                    else if (!runExists)
                        skipReason = "RUN_INSTANCE_NULL";
                    else if (!runAvailable)
                        skipReason = "NOT_AVAILABLE_IN_RUN";

                    sb.AppendLine($"      \"filter\": {{");
                    sb.AppendLine($"        \"isBanned\": {Bool(isBanned)},");
                    sb.AppendLine($"        \"ignoreForDropList\": {Bool(ignoreForDL)},");
                    sb.AppendLine($"        \"isAllowedTier\": {Bool(isAllowedTier)},");
                    sb.AppendLine($"        \"skipReason\": \"{skipReason}\"");
                    sb.AppendLine($"      }}");

                    sb.Append("    }");
                }
                sb.AppendLine();
                sb.AppendLine("  ],");

                // ========== ALL EQUIPMENT ==========
                sb.AppendLine("  \"equipment\": [");
                bool firstEquip = true;
                foreach (var eqIdx in EquipmentCatalog.allEquipment)
                {
                    if (!firstEquip)
                        sb.AppendLine(",");
                    firstEquip = false;

                    var edef = EquipmentCatalog.GetEquipmentDef(eqIdx);
                    sb.AppendLine("    {");
                    sb.AppendLine($"      \"equipmentIndex\": {(int)eqIdx},");
                    sb.AppendLine($"      \"equipmentDefExists\": {Bool(edef != null)},");

                    if (edef == null)
                    {
                        sb.AppendLine($"      \"_note\": \"EquipmentDef is null\"");
                        sb.Append("    }");
                        continue;
                    }

                    sb.AppendLine($"      \"name\": \"{Esc(edef.name)}\",");
                    sb.AppendLine($"      \"nameToken\": \"{Esc(edef.nameToken)}\",");
                    sb.AppendLine($"      \"displayName\": \"{Esc(SafeGetString(edef.nameToken))}\",");
                    sb.AppendLine($"      \"pickupToken\": \"{Esc(edef.pickupToken)}\",");
                    sb.AppendLine($"      \"pickupDescription\": \"{Esc(SafeGetString(edef.pickupToken))}\",");
                    sb.AppendLine($"      \"descriptionToken\": \"{Esc(edef.descriptionToken)}\",");
                    sb.AppendLine($"      \"loreToken\": \"{Esc(edef.loreToken)}\",");
                    sb.AppendLine($"      \"isLunar\": {Bool(edef.isLunar)},");
                    sb.AppendLine($"      \"isBoss\": {Bool(edef.isBoss)},");
                    sb.AppendLine($"      \"cooldown\": {edef.cooldown},");
                    sb.AppendLine($"      \"enigmaCompatible\": {Bool(edef.enigmaCompatible)},");
                    sb.AppendLine($"      \"canDrop\": {Bool(edef.canDrop)},");
                    sb.AppendLine($"      \"canBeRandomlyTriggered\": {Bool(edef.canBeRandomlyTriggered)},");
                    sb.AppendLine($"      \"appearsInSinglePlayer\": {Bool(edef.appearsInSinglePlayer)},");
                    sb.AppendLine($"      \"appearsInMultiPlayer\": {Bool(edef.appearsInMultiPlayer)},");
                    sb.AppendLine($"      \"colorIndex\": \"{edef.colorIndex}\",");
                    sb.AppendLine($"      \"dropOnDeathChance\": {edef.dropOnDeathChance},");
                    sb.AppendLine($"      \"passiveBuffDef\": \"{Esc(edef.passiveBuffDef != null ? edef.passiveBuffDef.name : "null")}\",");
                    sb.AppendLine($"      \"pickupIconSprite\": \"{Esc(edef.pickupIconSprite != null ? edef.pickupIconSprite.name : "null")}\",");
                    sb.AppendLine($"      \"unlockableDef\": \"{Esc(edef.unlockableDef != null ? edef.unlockableDef.cachedName : "null")}\",");
                    sb.AppendLine($"      \"requiredExpansion\": \"{Esc(edef.requiredExpansion != null ? edef.requiredExpansion.name : "null")}\",");

                    // Equipment pickup info
                    var ePickupIdx = PickupCatalog.FindPickupIndex(eqIdx);
                    bool eHasPickup = ePickupIdx != PickupIndex.none;
                    sb.AppendLine($"      \"pickup\": {{");
                    sb.AppendLine($"        \"pickupIndex\": {ePickupIdx.value},");
                    sb.AppendLine($"        \"hasPickup\": {Bool(eHasPickup)}");
                    sb.AppendLine($"      }}");

                    sb.Append("    }");
                }
                sb.AppendLine();
                sb.AppendLine("  ]");

                sb.AppendLine("}");

                string dir = System.IO.Path.Combine(BepInEx.Paths.BepInExRootPath, "LevelUpChoices");
                string path = System.IO.Path.Combine(dir, "item_dump.json");
                Directory.CreateDirectory(dir);
                File.WriteAllText(path, sb.ToString());
                Log.Info($"Item dump written to {path}");
            }
            catch (System.Exception e)
            {
                Log.Error(e);
            }
        }

        private static string Bool(bool v) => v ? "true" : "false";

        private static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";

            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", "\\n").Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }

        private static string SafeGetString(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return "";
                return Language.GetString(token) ?? "";
            }
            catch { return ""; }
        }
    }
}
