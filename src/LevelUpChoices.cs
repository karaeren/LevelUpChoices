using BepInEx;
using LevelUpChoices.Extensions;
using R2API;
using R2API.ContentManagement;
using R2API.Networking;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace LevelUpChoices
{
    // Dependencies
    [BepInDependency(NetworkingAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInDependency(R2APIContentManager.PluginGUID)]
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
        public const string PluginVersion = "1.1.3";

        public void Awake()
        {
            Log.Init(Logger);

            // Initialize configuration and Risk Of Options UI
            ModConfig.Init(Config, Info);
            Artifacts.ChoiceArtifact.Init(Info);
            Artifacts.EqualityArtifact.Init(Info);
            ItemCatalog.availability.CallWhenAvailable(Integrations.Init);
            ItemCatalog.availability.CallWhenAvailable(ItemSimilarityManager.Initialize);
            ItemCatalog.availability.CallWhenAvailable(Artifacts.EqualityArtifact.InitItems);

            // Register Network Messages
            NetworkingAPI.RegisterMessageType<Networking.SyncPlayerState>();
            NetworkingAPI.RegisterMessageType<Networking.SyncItems>();
            NetworkingAPI.RegisterMessageType<Networking.SendItemSelection>();
            NetworkingAPI.RegisterMessageType<Networking.SendBanish>();
            NetworkingAPI.RegisterMessageType<Networking.SendReroll>();
            NetworkingAPI.RegisterMessageType<Networking.SendPickingState>();
            NetworkingAPI.RegisterMessageType<Networking.SyncConfig>();

            // Init Managers
            var logicObject = new GameObject("LevelUpLogic");
            DontDestroyOnLoad(logicObject);
            logicObject.AddComponent<LevelUpManager>();
            logicObject.AddComponent<UI.Integration.UIPauseIntegration>();
            logicObject.AddComponent<UI.ItemSelectUI>();
            logicObject.AddComponent<Hooks.ExperienceHook>();
            logicObject.AddComponent<Hooks.InteractableSpawnHook>();
            logicObject.AddComponent<DebugManager>();

            Log.Info("LevelUpChoices initialized.");
        }
    }
}
