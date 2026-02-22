using BepInEx;
using R2API.Networking;
using UnityEngine;
// using RoR2;
// using UnityEngine.Networking;
namespace LevelUpChoices
{
    [BepInDependency(NetworkingAPI.PluginGUID)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class LevelUpChoices : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "karaeren";
        public const string PluginName = "LevelUpChoices";
        public const string PluginVersion = "1.0.6";

        public void Awake()
        {
            Log.Init(Logger);

            // Initialize configuration and Risk Of Options UI
            ModConfig.Init(Config, Info);

            // Register Network Messages
            NetworkingAPI.RegisterMessageType<Networking.SyncPlayerState>();
            NetworkingAPI.RegisterMessageType<Networking.SyncItems>();
            NetworkingAPI.RegisterMessageType<Networking.SendItemSelection>();
            NetworkingAPI.RegisterMessageType<Networking.SendBanish>();
            NetworkingAPI.RegisterMessageType<Networking.SendReroll>();

            // Init Managers
            var logicObject = new GameObject("LevelUpLogic");
            DontDestroyOnLoad(logicObject);
            logicObject.AddComponent<LevelUpManager>();
            logicObject.AddComponent<LevelUpUI>();
            logicObject.AddComponent<ExperienceHook>();
            logicObject.AddComponent<InteractableSpawnHook>();

            Log.Info("LevelUpChoices initialized.");
        }


        // private void Update()
        // {
        //     if (Input.GetKeyDown(KeyCode.F2))
        //     {
        //         if (NetworkServer.active)
        //         {
        //             TeamManager.instance.GiveTeamExperience(TeamIndex.Player, (TeamManager.instance.GetTeamNextLevelExperience(TeamIndex.Player) - TeamManager.instance.GetTeamCurrentLevelExperience(TeamIndex.Player)) / 6);
        //         }
        //     }
        // }
    }
}
