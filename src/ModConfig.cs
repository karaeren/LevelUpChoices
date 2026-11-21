using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using LevelUpChoices.UI;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace LevelUpChoices
{
    public static class ModConfig
    {
        // Shared
        public static ConfigEntry<bool> AlwaysEnableMod { get; private set; }
        public static ConfigEntry<bool> PauseOnItemSelect { get; private set; }
        public static ConfigEntry<int> DebugPassword { get; private set; }

        public static bool IsModEnabled => AlwaysEnableMod != null && (AlwaysEnableMod.Value || Artifacts.ChoiceArtifact.IsEnabled());

        // Client
        public static ConfigEntry<KeyboardShortcut> ToggleMenuKey { get; private set; }
        public static ConfigEntry<float> UIScale { get; private set; }
        public static ConfigEntry<bool> ShowItemDescriptions { get; private set; }
        public static ConfigEntry<bool> EnableNotifications { get; private set; }

        // Server
        public static ConfigEntry<bool> EnableInteractableRemoval { get; private set; }
        public static ConfigEntry<float> InteractableBudgetMultiplier { get; private set; }
        public static ConfigEntry<string> ItemBlacklist { get; private set; }
        public static ConfigEntry<bool> EnableEquipmentChoices { get; private set; }
        public static ConfigEntry<float> EquipmentRoundChance { get; private set; }
        public static ConfigEntry<bool> EnableCustomLevelSystem { get; private set; }
        public static ConfigEntry<bool> EnableMonsterLevelScaling { get; private set; }
        public static ConfigEntry<int> MaxLevel { get; private set; }

        private static readonly HashSet<string> RunItemBlacklist = new(StringComparer.OrdinalIgnoreCase);

        internal static bool IsItemBlacklisted(string itemName) => RunItemBlacklist.Contains(itemName);

        private static void CaptureItemBlacklist(Run run)
        {
            if (!NetworkServer.active)
                return;

            // Keep the same list for every player and the Artifact of Equality until the next run.
            RunItemBlacklist.Clear();
            RunItemBlacklist.UnionWith(ParseItemBlacklist(ItemBlacklist.Value));
        }

        internal static IEnumerable<string> ParseItemBlacklist(string value)
        {
            foreach (string entry in value.Split(','))
            {
                string itemName = entry.Trim();
                if (itemName.Length > 0)
                    yield return itemName;
            }
        }

        public static int MaxLevelValue => _maxLevelOverride ?? MaxLevel.Value;
        private static int? _maxLevelOverride;

        public static bool EnableMonsterLevelScalingValue => _enableMonsterLevelScalingOverride ?? EnableMonsterLevelScaling.Value;
        private static bool? _enableMonsterLevelScalingOverride;

        public static bool EnableCustomLevelSystemValue => _enableCustomLevelSystemOverride ?? EnableCustomLevelSystem.Value;
        private static bool? _enableCustomLevelSystemOverride;

        public static void UpdateServerConfig(int maxLevel, bool monsterScaling, bool customLevel)
        {
            _maxLevelOverride = maxLevel;
            _enableMonsterLevelScalingOverride = monsterScaling;
            _enableCustomLevelSystemOverride = customLevel;
            Log.Info($"Server config synced: MaxLevel={maxLevel}, MonsterScaling={monsterScaling}, CustomLevel={customLevel}");
            OnServerConfigSynced?.Invoke();
        }

        public static void ResetServerConfig()
        {
            _maxLevelOverride = null;
            _enableMonsterLevelScalingOverride = null;
            _enableCustomLevelSystemOverride = null;
        }

        public static event System.Action OnServerConfigSynced;

        public static ConfigEntry<int> ItemChoiceCount { get; private set; }
        public static ConfigEntry<int> LevelsPerBanishToken { get; private set; }
        public static ConfigEntry<int> StartingBanishTokens { get; private set; }
        public static ConfigEntry<int> StartingRerollTokens { get; private set; }
        public static ConfigEntry<bool> RerollTokenRefreshOnPick { get; private set; }
        public static ConfigEntry<float> SimilarItemChance { get; private set; }
        public static ConfigEntry<int> SimilarItemCount { get; private set; }
        public static ConfigEntry<float> SimilarityThreshold { get; private set; }

        // Tier weights (early game – 0 tokens used)
        public static ConfigEntry<float> EarlyWeightTier1 { get; private set; }
        public static ConfigEntry<float> EarlyWeightTier2 { get; private set; }
        public static ConfigEntry<float> EarlyWeightTier3 { get; private set; }
        public static ConfigEntry<float> EarlyWeightLunar { get; private set; }
        public static ConfigEntry<float> EarlyWeightBoss { get; private set; }

        // Tier weights at the end of the curve
        public static ConfigEntry<float> LateWeightTier1 { get; private set; }
        public static ConfigEntry<float> LateWeightTier2 { get; private set; }
        public static ConfigEntry<float> LateWeightTier3 { get; private set; }
        public static ConfigEntry<float> LateWeightLunar { get; private set; }
        public static ConfigEntry<float> LateWeightBoss { get; private set; }

        public static void Init(ConfigFile config, BepInEx.PluginInfo pluginInfo)
        {

            // Shared
            AlwaysEnableMod = config.Bind(
                            "Shared", "Always Enable Mod", false,
                            "Enables LevelUpChoices regardless of artifact usage. Works in modes where Artifacts are disabled too!");

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

            InteractableBudgetMultiplier = config.Bind(
                "Server", "Interactable Budget Multiplier", 0.9f,
                new ConfigDescription("Host setting. Scales the stage director's interactable budget when LevelUpChoices is enabled. 1 uses the normal budget. Changes apply on the next stage.",
                    new AcceptableValueRange<float>(0f, 2f)));

            ItemBlacklist = config.Bind(
                "Server", "Item Blacklist", string.Empty,
                "Host setting. Comma-separated internal item names, e.g. Syringe, Bear. Excludes them from player choices and Artifact of Equality enemy grants. Changes apply on the next run.");
            Run.onRunStartGlobal += CaptureItemBlacklist;

            EnableEquipmentChoices = config.Bind(
                "Server", "Enable Equipment Choices", true,
                "Host setting. Offer equipment on each player's tenth selection round, then randomly on later rounds. Equipment rounds cannot occur back-to-back.");

            EquipmentRoundChance = config.Bind(
                "Server", "Equipment Round Chance", 12f,
                new ConfigDescription("Host setting. Percent chance to offer equipment after round ten. Equipment offers are spaced at least one round apart, even if you choose items instead.",
                    new AcceptableValueRange<float>(0f, 100f)));

            EnableCustomLevelSystem = config.Bind(
                "Server", "Enable Level System", true,
                "Use it to unlock the 94 level cap and use a custom XP curve.");

            EnableMonsterLevelScaling = config.Bind(
                "Server", "Enable Monster Level Scaling", true,
                "Scale monster levels based on Max Level. Keeps enemies relevant at high player levels.");

            MaxLevel = config.Bind(
                "Server", "Max Level", 256,
                new ConfigDescription("Determines the absolute max level achievable. The XP will automatically scale. A very high max level means first few levels will be very easy.",
                new AcceptableValueRange<int>(94, 9999)));

            ItemChoiceCount = config.Bind(
                "Server", "Item Choices", 3,
                "Number of item choices presented on each level up.");

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

            SimilarItemChance = config.Bind(
                "Server", "Similar Item Chance", 30f,
                "Chance (0-100) for a rolled item to be replaced with an item similar to one you already own.");

            SimilarItemCount = config.Bind(
                "Server", "Similar Item Count", 8,
                "Number of similar items to map per item (5 to 20). Requires restart.");

            SimilarityThreshold = config.Bind(
                "Server", "Similarity Threshold", 0.15f,
                "The minimum TF-IDF cosine similarity score required (0.0 to 1.0) for two items to be considered similar.");

            // Early tier weights
            EarlyWeightTier1 = config.Bind("Server", "Early Weight – Common", 77f,
                "Category weight for Common (Tier 1) items at 0 tokens used.");
            EarlyWeightTier2 = config.Bind("Server", "Early Weight – Uncommon", 19f,
                "Category weight for Uncommon (Tier 2) items at 0 tokens used.");
            EarlyWeightTier3 = config.Bind("Server", "Early Weight – Legendary", 2f,
                "Category weight for Legendary (Tier 3) items at 0 tokens used.");
            EarlyWeightLunar = config.Bind("Server", "Early Weight – Lunar", 1f,
                "Category weight for Lunar items at 0 tokens used.");
            EarlyWeightBoss = config.Bind("Server", "Early Weight – Boss", 1f,
                "Category weight for Boss items at 0 tokens used.");

            // Late tier weights
            LateWeightTier1 = config.Bind("Server", "Late Weight – Common", 21f,
                $"Category weight for Common (Tier 1) items at {PlayerDropTable.LateWeightPickCount}+ tokens used.");
            LateWeightTier2 = config.Bind("Server", "Late Weight – Uncommon", 41f,
                $"Category weight for Uncommon (Tier 2) items at {PlayerDropTable.LateWeightPickCount}+ tokens used.");
            LateWeightTier3 = config.Bind("Server", "Late Weight – Legendary", 20f,
                $"Category weight for Legendary (Tier 3) items at {PlayerDropTable.LateWeightPickCount}+ tokens used.");
            LateWeightLunar = config.Bind("Server", "Late Weight – Lunar", 10f,
                $"Category weight for Lunar items at {PlayerDropTable.LateWeightPickCount}+ tokens used.");
            LateWeightBoss = config.Bind("Server", "Late Weight – Boss", 8f,
                $"Category weight for Boss items at {PlayerDropTable.LateWeightPickCount}+ tokens used.");

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
            ModSettingsManager.AddOption(new CheckBoxOption(AlwaysEnableMod));
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
            ModSettingsManager.AddOption(new StepSliderOption(InteractableBudgetMultiplier,
                new StepSliderConfig { min = 0f, max = 2f, increment = 0.05f, FormatString = "{0:0.00}x" }));
            ModSettingsManager.AddOption(new GenericButtonOption("Item Blacklist", "Server",
                "Edit the items excluded from player choices and Artifact of Equality enemy grants. Changes apply on the next run.",
                "Edit", ItemBlacklistEditor.Open));
            ModSettingsManager.AddOption(new CheckBoxOption(EnableEquipmentChoices));
            ModSettingsManager.AddOption(new SliderOption(EquipmentRoundChance,
                new SliderConfig { min = 0f, max = 100f, FormatString = "{0:0}%" }));
            ModSettingsManager.AddOption(new CheckBoxOption(EnableCustomLevelSystem));
            ModSettingsManager.AddOption(new CheckBoxOption(EnableMonsterLevelScaling));

            ModSettingsManager.AddOption(new IntSliderOption(MaxLevel,
                            new IntSliderConfig { min = 94, max = 9999 }));

            ModSettingsManager.AddOption(new IntSliderOption(ItemChoiceCount,
                new IntSliderConfig { min = 1, max = 10 }));

            ModSettingsManager.AddOption(new IntSliderOption(LevelsPerBanishToken,
                new IntSliderConfig { min = 1, max = 50 }));

            ModSettingsManager.AddOption(new IntSliderOption(StartingBanishTokens,
                new IntSliderConfig { min = 0, max = 10 }));

            ModSettingsManager.AddOption(new IntSliderOption(StartingRerollTokens,
                new IntSliderConfig { min = 0, max = 10 }));

            ModSettingsManager.AddOption(new CheckBoxOption(RerollTokenRefreshOnPick));

            ModSettingsManager.AddOption(new SliderOption(SimilarItemChance,
                new SliderConfig { min = 0f, max = 100f, FormatString = "{0:0}%" }));

            ModSettingsManager.AddOption(new IntSliderOption(SimilarItemCount,
                new IntSliderConfig { min = 5, max = 20 }));

            ModSettingsManager.AddOption(new SliderOption(SimilarityThreshold,
                new SliderConfig { min = 0f, max = 1f, FormatString = "{0:0.00}" }));

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
