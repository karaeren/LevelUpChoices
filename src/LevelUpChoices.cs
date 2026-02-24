using BepInEx;
using LevelUpChoices.Extensions;
using R2API.Networking;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace LevelUpChoices
{
    // Dependencies
    [BepInDependency(NetworkingAPI.PluginGUID)]
    [BepInDependency(RiskOfOptions.PluginInfo.PLUGIN_GUID)]
    // Soft Dependencies
    [BepInDependency(LookingGlass.PluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    // Compatibility
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class LevelUpChoices : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "karaeren";
        public const string PluginName = "LevelUpChoices";
        public const string PluginVersion = "1.0.9";

        public void Awake()
        {
            Log.Init(Logger);

            // Initialize configuration and Risk Of Options UI
            ModConfig.Init(Config, Info);
            ItemCatalog.availability.CallWhenAvailable(Integrations.Init);

            // Register Network Messages
            NetworkingAPI.RegisterMessageType<Networking.SyncPlayerState>();
            NetworkingAPI.RegisterMessageType<Networking.SyncItems>();
            NetworkingAPI.RegisterMessageType<Networking.SendItemSelection>();
            NetworkingAPI.RegisterMessageType<Networking.SendBanish>();
            NetworkingAPI.RegisterMessageType<Networking.SendReroll>();
            NetworkingAPI.RegisterMessageType<Networking.SendPickingState>();

            // Init Managers
            var logicObject = new GameObject("LevelUpLogic");
            DontDestroyOnLoad(logicObject);
            logicObject.AddComponent<LevelUpManager>();
            logicObject.AddComponent<LevelUpUI>();
            logicObject.AddComponent<ExperienceHook>();
            logicObject.AddComponent<InteractableSpawnHook>();
            logicObject.AddComponent<DebugManager>();

            Log.Info("LevelUpChoices initialized.");
        }
    }
}
