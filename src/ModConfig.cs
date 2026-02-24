using System.IO;
using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using UnityEngine;

namespace LevelUpChoices
{
    public static class ModConfig
    {
        // Shared
        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<bool> PauseOnItemSelect;
        public static ConfigEntry<int> DebugPassword;

        // Client
        public static ConfigEntry<KeyboardShortcut> ToggleMenuKey;
        public static ConfigEntry<float> UIScale;
        public static ConfigEntry<bool> ShowItemDescriptions;
        public static ConfigEntry<bool> EnableNotifications;

        // Server
        public static ConfigEntry<bool> EnableInteractableRemoval;
        public static ConfigEntry<int> ItemChoiceCount;
        public static ConfigEntry<float> ExperienceStartMultiplier;
        public static ConfigEntry<float> ExperienceGrowthRate;
        public static ConfigEntry<int> LevelsPerBanishToken;
        public static ConfigEntry<int> StartingBanishTokens;
        public static ConfigEntry<int> StartingRerollTokens;
        public static ConfigEntry<bool> RerollTokenRefreshOnPick;

        // Tier weights (early game – 0 tokens used)
        public static ConfigEntry<float> EarlyWeightTier1;
        public static ConfigEntry<float> EarlyWeightTier2;
        public static ConfigEntry<float> EarlyWeightTier3;
        public static ConfigEntry<float> EarlyWeightLunar;
        public static ConfigEntry<float> EarlyWeightBoss;

        // Tier weights (late game – 30+ tokens used)
        public static ConfigEntry<float> LateWeightTier1;
        public static ConfigEntry<float> LateWeightTier2;
        public static ConfigEntry<float> LateWeightTier3;
        public static ConfigEntry<float> LateWeightLunar;
        public static ConfigEntry<float> LateWeightBoss;

        public static void Init(ConfigFile config, BepInEx.PluginInfo pluginInfo)
        {

            // Shared
            ModEnabled = config.Bind(
                            "Shared", "Mod Enabled", true,
                            "Master toggle – when disabled the mod does nothing.");

            PauseOnItemSelect = config.Bind(
                "Shared", "Pause On Item Select", true,
                "Pause the game while the item selection UI is open. In singleplayer, pauses directly. In multiplayer, uses the built-in pause system (requires host with multiplayer pause enabled).");

            DebugPassword = config.Bind(
                "Shared", "Debug Password", 1,
                "This is only to be used by the developer for debugging the game.");

            // Client
            ToggleMenuKey = config.Bind(
                            "Client", "Toggle Menu Key", new KeyboardShortcut(KeyCode.F3),
                            "Key to open / close the level-up item selection menu.");

            UIScale = config.Bind(
                "Client", "UI Scale", 1.0f,
                "Scale multiplier for the level-up selection UI (0.5 = half size, 2.0 = double).");

            ShowItemDescriptions = config.Bind(
                "Client", "Show Item Descriptions", true,
                "Display the pickup description text under each item card.");

            EnableNotifications = config.Bind(
                "Client", "Enable Notifications", true,
                "Shows the notification badge when item selection is available.");

            // Server
            EnableInteractableRemoval = config.Bind(
                            "Server", "Remove Chests & Interactables", true,
                            "Remove chests, printers, shrines and other item-giving interactables from stages.");

            ItemChoiceCount = config.Bind(
                "Server", "Item Choices", 3,
                "Number of item choices presented on each level up.");

            ExperienceStartMultiplier = config.Bind(
                "Server", "XP Start Multiplier", 1.69f,
                "Experience multiplier applied at level 1.");

            ExperienceGrowthRate = config.Bind(
                "Server", "XP Growth Rate", 1.169f,
                "Multiplier compounded per level (effective mult = Start * Growth^(level-1)).");

            LevelsPerBanishToken = config.Bind(
                "Server", "Levels Per Banish Token", 10,
                "Every N levels the player gains an extra banish token.");

            StartingBanishTokens = config.Bind(
                "Server", "Starting Banish Tokens", 1,
                "Banish tokens each player starts with at the beginning of a run.");

            StartingRerollTokens = config.Bind(
                "Server", "Starting Reroll Tokens", 1,
                "Reroll tokens each player starts with at the beginning of a run.");

            RerollTokenRefreshOnPick = config.Bind(
                "Server", "Reroll Token Refresh On Pick", true,
                "When enabled, the player's reroll token is restored to 1 after picking an item.");

            // Early tier weights
            EarlyWeightTier1 = config.Bind("Server", "Early Weight – Common", 79f,
                "Category weight for Common (Tier 1) items at 0 tokens used.");
            EarlyWeightTier2 = config.Bind("Server", "Early Weight – Uncommon", 15f,
                "Category weight for Uncommon (Tier 2) items at 0 tokens used.");
            EarlyWeightTier3 = config.Bind("Server", "Early Weight – Legendary", 4f,
                "Category weight for Legendary (Tier 3) items at 0 tokens used.");
            EarlyWeightLunar = config.Bind("Server", "Early Weight – Lunar", 1f,
                "Category weight for Lunar items at 0 tokens used.");
            EarlyWeightBoss = config.Bind("Server", "Early Weight – Boss", 1f,
                "Category weight for Boss items at 0 tokens used.");

            // Late tier weights
            LateWeightTier1 = config.Bind("Server", "Late Weight – Common", 20f,
                "Category weight for Common (Tier 1) items at 30+ tokens used.");
            LateWeightTier2 = config.Bind("Server", "Late Weight – Uncommon", 41f,
                "Category weight for Uncommon (Tier 2) items at 30+ tokens used.");
            LateWeightTier3 = config.Bind("Server", "Late Weight – Legendary", 21f,
                "Category weight for Legendary (Tier 3) items at 30+ tokens used.");
            LateWeightLunar = config.Bind("Server", "Late Weight – Lunar", 10f,
                "Category weight for Lunar items at 30+ tokens used.");
            LateWeightBoss = config.Bind("Server", "Late Weight – Boss", 8f,
                "Category weight for Boss items at 30+ tokens used.");

            // Register with Risk Of Options
            ModSettingsManager.SetModDescription("Level Up Choices – pick items when your team levels up.");

            try
            {
                string pluginDir = System.IO.Path.GetDirectoryName(pluginInfo.Location);
                string folderName = System.IO.Path.Combine(pluginDir, "icons");
                var files = Directory.GetFiles(folderName);
                if (files.Length > 0)
                {
                    string file = files[0];
                    byte[] bytes = File.ReadAllBytes(file);
                    Texture2D tex = new(256, 256, TextureFormat.ARGB32, false, false);
                    tex.LoadImage(bytes);
                    tex.filterMode = FilterMode.Point;
                    Sprite icon = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 1, SpriteMeshType.Tight, Vector4.zero, true);
                    ModSettingsManager.SetModIcon(icon);
                }
            }
            catch (System.Exception e)
            {
                Log.Warning($"Failed to load mod icon: {e.Message}");
            }

            RegisterSharedOptions();
            RegisterClientOptions();
            RegisterServerOptions();
        }

        // Risk Of Options registration

        private static void RegisterSharedOptions()
        {
            ModSettingsManager.AddOption(new CheckBoxOption(ModEnabled));
            ModSettingsManager.AddOption(new CheckBoxOption(PauseOnItemSelect));
            ModSettingsManager.AddOption(new IntSliderOption(DebugPassword,
                new IntSliderConfig { min = 1, max = 9999 }));

        }

        private static void RegisterClientOptions()
        {
            ModSettingsManager.AddOption(new KeyBindOption(ToggleMenuKey));

            ModSettingsManager.AddOption(new StepSliderOption(UIScale,
                new StepSliderConfig { min = 0.5f, max = 2.0f, increment = 0.1f }));

            ModSettingsManager.AddOption(new CheckBoxOption(ShowItemDescriptions));
            ModSettingsManager.AddOption(new CheckBoxOption(EnableNotifications));
        }

        private static void RegisterServerOptions()
        {
            ModSettingsManager.AddOption(new CheckBoxOption(EnableInteractableRemoval));

            ModSettingsManager.AddOption(new IntSliderOption(ItemChoiceCount,
                new IntSliderConfig { min = 1, max = 10 }));

            ModSettingsManager.AddOption(new StepSliderOption(ExperienceStartMultiplier,
                new StepSliderConfig { min = 0.5f, max = 5.0f, increment = 0.01f }));

            ModSettingsManager.AddOption(new StepSliderOption(ExperienceGrowthRate,
                new StepSliderConfig { min = 1.0f, max = 2.0f, increment = 0.001f }));

            ModSettingsManager.AddOption(new IntSliderOption(LevelsPerBanishToken,
                new IntSliderConfig { min = 1, max = 50 }));

            ModSettingsManager.AddOption(new IntSliderOption(StartingBanishTokens,
                new IntSliderConfig { min = 0, max = 10 }));

            ModSettingsManager.AddOption(new IntSliderOption(StartingRerollTokens,
                new IntSliderConfig { min = 0, max = 10 }));

            ModSettingsManager.AddOption(new CheckBoxOption(RerollTokenRefreshOnPick));

            // Early weights
            ModSettingsManager.AddOption(new SliderOption(EarlyWeightTier1,
                new SliderConfig { min = 0f, max = 100f }));
            ModSettingsManager.AddOption(new SliderOption(EarlyWeightTier2,
                new SliderConfig { min = 0f, max = 100f }));
            ModSettingsManager.AddOption(new SliderOption(EarlyWeightTier3,
                new SliderConfig { min = 0f, max = 100f }));
            ModSettingsManager.AddOption(new SliderOption(EarlyWeightLunar,
                new SliderConfig { min = 0f, max = 100f }));
            ModSettingsManager.AddOption(new SliderOption(EarlyWeightBoss,
                new SliderConfig { min = 0f, max = 100f }));

            // Late weights
            ModSettingsManager.AddOption(new SliderOption(LateWeightTier1,
                new SliderConfig { min = 0f, max = 100f }));
            ModSettingsManager.AddOption(new SliderOption(LateWeightTier2,
                new SliderConfig { min = 0f, max = 100f }));
            ModSettingsManager.AddOption(new SliderOption(LateWeightTier3,
                new SliderConfig { min = 0f, max = 100f }));
            ModSettingsManager.AddOption(new SliderOption(LateWeightLunar,
                new SliderConfig { min = 0f, max = 100f }));
            ModSettingsManager.AddOption(new SliderOption(LateWeightBoss,
                new SliderConfig { min = 0f, max = 100f }));
        }
    }
}
