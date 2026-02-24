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
