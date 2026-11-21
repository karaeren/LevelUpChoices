using System;
using System.Reflection;
using LookingGlass.ItemStatsNameSpace;
using RoR2;

namespace LevelUpChoices.Extensions
{
    internal class LookingGlassIntegration
    {
        // Cached reflection handle for ItemStats.GetItemDescription(ItemDef, int, CharacterMaster, bool, bool)
        private static MethodInfo _getItemDescriptionMethod;
        private static MethodInfo _getEquipmentExtrasMethod;

        internal static void Init()
        {
            var assembly = Assembly.GetAssembly(typeof(ItemDefinitions));
            var itemStatsType = assembly?.GetType("LookingGlass.ItemStatsNameSpace.ItemStats");

            if (itemStatsType != null)
            {
                _getItemDescriptionMethod = itemStatsType.GetMethod(
                    "GetItemDescription",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(ItemDef), typeof(int), typeof(CharacterMaster), typeof(bool), typeof(bool)],
                    null);
                _getEquipmentExtrasMethod = itemStatsType.GetMethod(
                    "GetEquipmentExtras",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(CharacterMaster), typeof(EquipmentIndex)],
                    null);
            }

            if (_getItemDescriptionMethod != null)
            {
                Log.Info("LookingGlass integration initialized successfully.");
            }
            else
            {
                Log.Warning("LookingGlass integration: could not find ItemStats.GetItemDescription. Integration will not work.");
            }
        }

        internal static string GetEquipmentDescription(EquipmentDef equipmentDef, CharacterMaster master)
        {
            string description = Language.GetString(equipmentDef.descriptionToken);
            if (_getEquipmentExtrasMethod == null || !master)
                return description;

            try
            {
                string extras = (string)_getEquipmentExtrasMethod.Invoke(null, [master, equipmentDef.equipmentIndex]);
                return string.IsNullOrEmpty(extras) ? description : description + "\n\n" + extras;
            }
            catch (Exception e)
            {
                Log.Error($"LookingGlass GetEquipmentExtras reflection call failed: {e}");
                return description;
            }
        }

        internal static string GetItemDescription(
            ItemDef itemDef, int itemCount, CharacterMaster master,
            bool withOneMore, bool forceNew = false)
        {
            if (_getItemDescriptionMethod == null)
                return null;

            try
            {
                return (string)_getItemDescriptionMethod.Invoke(
                    null, [itemDef, itemCount, master, withOneMore, forceNew]);
            }
            catch (Exception e)
            {
                Log.Error($"LookingGlass GetItemDescription reflection call failed: {e}");
                return null;
            }
        }
    }
}
