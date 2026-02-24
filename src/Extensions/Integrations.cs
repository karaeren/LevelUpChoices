using System;

namespace LevelUpChoices.Extensions
{
    public static class Integrations
    {
        public static bool lookingGlassEnabled = false;

        public static void Init()
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
