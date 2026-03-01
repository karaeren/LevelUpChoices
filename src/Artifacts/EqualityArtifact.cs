using System.Collections;
using System.Collections.Generic;
using System.Linq;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;
using UnityEngine.Networking;

namespace LevelUpChoices.Artifacts
{
    public class EqualityArtifact : IContentPackProvider
    {
        public static ArtifactDef ArtifactDef;

        private static string _basePath;

        public ContentPack contentPack = new();

        public string identifier => "LevelUpChoices.EqualityArtifact";

        private static int _lastMonsterLevel = 1;
        private static int _itemGrantsCount = 0;
        private static Inventory _monsterTeamInventory;

        private static List<ItemIndex> _tier1Items = new();
        private static List<ItemIndex> _tier2Items = new();
        private static List<ItemIndex> _tier3Items = new();

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            ArtifactDef = ScriptableObject.CreateInstance<ArtifactDef>();
            ArtifactDef.cachedName = "ARTIFACT_EQUALITY";
            ArtifactDef.nameToken = "ARTIFACT_EQUALITY_NAME";
            ArtifactDef.descriptionToken = "ARTIFACT_EQUALITY_DESC";

            if (!string.IsNullOrEmpty(_basePath))
            {
                string onPath = System.IO.Path.Combine(_basePath, "Assets", "AoE_On.png");
                string offPath = System.IO.Path.Combine(_basePath, "Assets", "AoE_Off.png");

                ArtifactDef.smallIconSelectedSprite = LoadSprite(onPath);
                ArtifactDef.smallIconDeselectedSprite = LoadSprite(offPath);
            }

            contentPack.artifactDefs.Add([ArtifactDef]);

            args.ReportProgress(1f);
            yield break;
        }

        private static Sprite LoadSprite(string path)
        {
            if (System.IO.File.Exists(path))
            {
                byte[] bytes = System.IO.File.ReadAllBytes(path);
                Texture2D tex = new(256, 256, TextureFormat.ARGB32, false, false);
                tex.LoadImage(bytes);
                tex.filterMode = FilterMode.Point;
                return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 1, SpriteMeshType.Tight, Vector4.zero, true);
            }
            return null;
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(contentPack, args.output);
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }

        internal static void Init(BepInEx.PluginInfo pluginInfo)
        {
            _basePath = System.IO.Path.GetDirectoryName(pluginInfo.Location);
            _lastMonsterLevel = 1;
            _itemGrantsCount = 0;

            LanguageAPI.Add("ARTIFACT_EQUALITY_NAME", "Artifact of Equality");
            LanguageAPI.Add("ARTIFACT_EQUALITY_DESC", "Enemies receive a random item when they level up. After all, we're all equal in death.");

            ContentManager.collectContentPackProviders += ContentManager_collectContentPackProviders;

            RunArtifactManager.onArtifactEnabledGlobal += OnArtifactEnabled;
            RunArtifactManager.onArtifactDisabledGlobal += OnArtifactDisabled;
            Run.onRunStartGlobal += OnRunStartGlobal;
            Run.onRunDestroyGlobal += OnRunDestroyGlobal;
            SpawnCard.onSpawnedServerGlobal += OnServerCardSpawnedGlobal;

            On.RoR2.Run.RecalculateDifficultyCoefficentInternal += Run_RecalculateDifficultyCoefficentInternal;
        }

        private static void Run_RecalculateDifficultyCoefficentInternal(On.RoR2.Run.orig_RecalculateDifficultyCoefficentInternal orig, Run self)
        {
            orig(self);
            if (NetworkServer.active && IsEnabled())
            {
                // Safety: if _lastMonsterLevel is 0 (uninitialized static), treat it as level 1
                if (_lastMonsterLevel <= 0)
                    _lastMonsterLevel = 1;

                int newLevel = ExperienceHook.GetCurrentMonsterLevel();
                if (newLevel > _lastMonsterLevel)
                {
                    int levelsGained = newLevel - _lastMonsterLevel;
                    for (int i = 0; i < levelsGained; i++)
                    {
                        GrantItemToMonsters();
                    }
                    _lastMonsterLevel = newLevel;
                }
            }
        }

        [SystemInitializer(typeof(ItemCatalog))]
        public static void InitItems()
        {
            if (_tier1Items.Count > 0)
                return; // Already initialized

            _tier1Items.Clear();
            _tier2Items.Clear();
            _tier3Items.Clear();

            // Build item lists when catalog is ready
            foreach (var itemDef in ItemCatalog.allItemDefs)
            {
                if (
                    itemDef.ContainsTag(ItemTag.AIBlacklist) ||
                    itemDef.ContainsTag(ItemTag.OnKillEffect) ||
                    itemDef.ContainsTag(ItemTag.SprintRelated) ||
                    itemDef.ContainsTag(ItemTag.EquipmentRelated) ||
                    itemDef.ContainsTag(ItemTag.IgnoreForDropList) ||
                    itemDef.ContainsTag(ItemTag.Scrap) ||
                    itemDef.ContainsTag(ItemTag.PriorityScrap)
                )
                    continue;

                if (itemDef.tier == ItemTier.Tier1)
                    _tier1Items.Add(itemDef.itemIndex);
                else if (itemDef.tier == ItemTier.Tier2)
                    _tier2Items.Add(itemDef.itemIndex);
                else if (itemDef.tier == ItemTier.Tier3)
                    _tier3Items.Add(itemDef.itemIndex);
            }
            Log.Info($"Initialized monster item pools. T1: {_tier1Items.Count}, T2: {_tier2Items.Count}, T3: {_tier3Items.Count}");
        }

        private static void ContentManager_collectContentPackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
        {
            addContentPackProvider(new EqualityArtifact());
        }

        public static bool IsEnabled()
        {
            if (RunArtifactManager.instance && ArtifactDef)
            {
                return RunArtifactManager.instance.IsArtifactEnabled(ArtifactDef);
            }
            return false;
        }

        private static void OnArtifactEnabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (artifactDef == ArtifactDef && NetworkServer.active)
            {
                Log.Info("EqualityArtifact enabled.");
                // In case it's enabled mid-run (e.g. commands), we can initialize
                if (Run.instance)
                {
                    if (_monsterTeamInventory == null)
                    {
                        CreateMonsterInventory();
                    }
                    // Sync level so we don't grant items for levels already reached
                    _lastMonsterLevel = ExperienceHook.GetCurrentMonsterLevel();
                }
            }
        }

        private static void OnArtifactDisabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (artifactDef == ArtifactDef && NetworkServer.active)
            {
                Log.Info("EqualityArtifact disabled.");
                if (_monsterTeamInventory)
                {
                    NetworkServer.Destroy(_monsterTeamInventory.gameObject);
                    _monsterTeamInventory = null;
                }
            }
        }

        private static void CreateMonsterInventory()
        {
            if (_monsterTeamInventory)
                return;

            GameObject inventoryPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/MonsterTeamGainsItemsArtifactInventory");
            if (inventoryPrefab)
            {
                _monsterTeamInventory = UnityEngine.Object.Instantiate(inventoryPrefab).GetComponent<Inventory>();
                _monsterTeamInventory.GetComponent<TeamFilter>().teamIndex = TeamIndex.Monster;
                NetworkServer.Spawn(_monsterTeamInventory.gameObject);
            }
            else
            {
                Log.Error("Failed to load monster inventory prefab!");
            }
        }

        private static void OnRunStartGlobal(Run run)
        {
            if (NetworkServer.active)
            {
                _itemGrantsCount = 0;
                _lastMonsterLevel = 1; // Reset to 1 for the new run

                if (IsEnabled())
                {
                    CreateMonsterInventory();
                    // We don't grant items here anymore, Run_RecalculateDifficultyCoefficentInternal will catch the 1 -> N jump immediately.
                }
            }
        }

        private static void OnRunDestroyGlobal(Run run)
        {
            if (_monsterTeamInventory)
            {
                NetworkServer.Destroy(_monsterTeamInventory.gameObject);
                _monsterTeamInventory = null;
            }
            _lastMonsterLevel = 1;
            _itemGrantsCount = 0;
        }

        private static void OnServerCardSpawnedGlobal(SpawnCard.SpawnResult spawnResult)
        {
            if (!NetworkServer.active || !IsEnabled() || !_monsterTeamInventory)
                return;

            CharacterMaster characterMaster = spawnResult.spawnedInstance ? spawnResult.spawnedInstance.GetComponent<CharacterMaster>() : null;
            if (characterMaster && characterMaster.teamIndex == TeamIndex.Monster)
            {
                characterMaster.inventory.AddItemsFrom(_monsterTeamInventory);
            }
        }

        private static void GrantItemToMonsters()
        {
            if (!_monsterTeamInventory)
            {
                Log.Warning("Attempted to grant item but monster inventory is null!");
                return;
            }

            int sequenceIndex = _itemGrantsCount % 5;
            _itemGrantsCount++;

            List<ItemIndex> pool = null;

            switch (sequenceIndex)
            {
                case 0: // White
                case 1: // White
                    pool = _tier1Items;
                    break;
                case 2: // Green
                case 3: // Green
                    pool = _tier2Items;
                    break;
                case 4: // Red
                    pool = _tier3Items;
                    break;
            }

            if (pool != null && pool.Count > 0)
            {
                Xoroshiro128Plus rng = Run.instance.treasureRng;
                ItemIndex chosenItem = pool[rng.RangeInt(0, pool.Count)];

                _monsterTeamInventory.GiveItemPermanent(chosenItem, 1);

                // Give to existing monsters
                foreach (var master in CharacterMaster.readOnlyInstancesList)
                {
                    if (master.teamIndex == TeamIndex.Monster && master.inventory)
                    {
                        master.inventory.GiveItemPermanent(chosenItem, 1);
                    }
                }

                // Announce to chat
                ItemDef itemDef = ItemCatalog.GetItemDef(chosenItem);
                if (itemDef != null)
                {
                    ColorCatalog.ColorIndex cIndex = ItemTierCatalog.GetItemTierDef(itemDef.tier)?.colorIndex ?? ColorCatalog.ColorIndex.Unaffordable;
                    string colorHex = ColorCatalog.GetColorHexString(cIndex);
                    string itemName = Language.GetString(itemDef.nameToken);
                    string message = $"Enemies levelled up. They gained <color=#{colorHex}>{itemName}</color>!";
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = message });
                }
            }
            else
            {
                Log.Warning($"Pool for sequence index {sequenceIndex} is empty! Pool size T1: {_tier1Items.Count}, T2: {_tier2Items.Count}, T3: {_tier3Items.Count}");
            }
        }
    }
}
