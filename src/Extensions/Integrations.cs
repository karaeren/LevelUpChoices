using System;
using RoR2;

namespace LevelUpChoices
{
    internal class Integrations
    {
        internal static bool lookingGlassEnabled = false;

        internal static void Init()
        {
            System.Collections.Generic.Dictionary<string, BepInEx.PluginInfo> pluginInfos = BepInEx.Bootstrap.Chainloader.PluginInfos;

            if (pluginInfos.ContainsKey(LookingGlass.PluginInfo.PLUGIN_GUID))
            {
                try
                {
                    Log.Info("Running code injection for LookingGlass.");
                    LookingGlassIntegration.Init();
                    lookingGlassEnabled = true;
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
    }
}