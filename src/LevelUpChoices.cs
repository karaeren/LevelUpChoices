using BepInEx;
using R2API.Networking;
using UnityEngine;

namespace LevelUpChoices
{
    [BepInDependency(NetworkingAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class LevelUpChoices : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "karaeren";
        public const string PluginName = "LevelUpChoices";
        public const string PluginVersion = "1.0.0";

        public void Awake()
        {
            Log.Init(Logger);

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

            Log.Info("LevelUpChoices initialized.");
        }

        // using RoR2;
        // using UnityEngine.Networking;
        // private void Update()
        // {
        //     if (Input.GetKeyDown(KeyCode.F2))
        //     {
        //         Log.Info("F2 pressed. Forcing Level Up.");
        //         if (NetworkServer.active)
        //         {
        //             TeamManager.instance.GiveTeamExperience(TeamIndex.Player, TeamManager.instance.GetTeamNextLevelExperience(TeamIndex.Player) - TeamManager.instance.GetTeamCurrentLevelExperience(TeamIndex.Player) + 1);
        //         }
        //         else
        //         {
        //             Log.Warning("F2 pressed but not host/server. Cannot give experience.");
        //         }
        //     }
        // }
    }
}
