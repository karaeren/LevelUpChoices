using System;
using System.Reflection;
using LookingGlass.ItemStatsNameSpace;
using RoR2;

namespace LevelUpChoices.Extensions {
    internal class LookingGlassIntegration {
        // Cached reflection handle for ItemStats.GetItemDescription(ItemDef, int, CharacterMaster, bool, bool)
        private static MethodInfo s_getItemDescriptionMethod;

        internal static void Init() {
            var assembly = Assembly.GetAssembly(typeof(ItemDefinitions));
            Type itemStatsType = assembly?.GetType("LookingGlass.ItemStatsNameSpace.ItemStats");

            if (itemStatsType != null) {
                s_getItemDescriptionMethod = itemStatsType.GetMethod(
                    "GetItemDescription",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(ItemDef), typeof(int), typeof(CharacterMaster), typeof(bool), typeof(bool)],
                    null);
            }

            if (s_getItemDescriptionMethod != null) {
                Log.Info("LookingGlass integration initialized successfully.");
            }
            else {
                Log.Warning("LookingGlass integration: could not find ItemStats.GetItemDescription. Integration will not work.");
            }
        }

        private static bool s_hasLoggedError = false;

        internal static string GetItemDescription(
            ItemDef itemDef, int itemCount, CharacterMaster master,
            bool withOneMore, bool forceNew = false) {
            if (s_getItemDescriptionMethod == null)
                return null;

            try {
                return (string)s_getItemDescriptionMethod.Invoke(
                    null, [itemDef, itemCount, master, withOneMore, forceNew]);
            }
            catch (Exception e) {
                if (!s_hasLoggedError) {
                    Log.Error($"LookingGlass GetItemDescription reflection call failed: {e}\nSuppressing further errors from this method.");
                    s_hasLoggedError = true;
                }
                return null;
            }
        }
    }
}
