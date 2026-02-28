using System.Collections;
using System.Collections.Generic;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;

namespace LevelUpChoices
{
    public class LevelUpArtifact : IContentPackProvider
    {
        public static ArtifactDef ArtifactDef;

        private static string _basePath;

        public ContentPack contentPack = new();

        public string identifier => "LevelUpChoices.LevelUpArtifact";

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            ArtifactDef = ScriptableObject.CreateInstance<ArtifactDef>();
            ArtifactDef.cachedName = "ARTIFACT_LEVELUPCHOICES";
            ArtifactDef.nameToken = "ARTIFACT_LEVELUPCHOICES_NAME";
            ArtifactDef.descriptionToken = "ARTIFACT_LEVELUPCHOICES_DESC";

            if (!string.IsNullOrEmpty(_basePath))
            {
                string onPath = System.IO.Path.Combine(_basePath, "Assets", "AoC_On.png");
                string offPath = System.IO.Path.Combine(_basePath, "Assets", "AoC_Off.png");

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

            LanguageAPI.Add("ARTIFACT_LEVELUPCHOICES_NAME", "Artifact of Choice");
            LanguageAPI.Add("ARTIFACT_LEVELUPCHOICES_DESC", "Choose an item out of multiple options when you level up. Alters level scaling and maximum level.");

            ContentManager.collectContentPackProviders += ContentManager_collectContentPackProviders;
        }

        private static void ContentManager_collectContentPackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
        {
            addContentPackProvider(new LevelUpArtifact());
        }

        public static bool IsEnabled()
        {
            if (RunArtifactManager.instance && ArtifactDef)
            {
                return RunArtifactManager.instance.IsArtifactEnabled(ArtifactDef);
            }
            return false;
        }
    }
}
