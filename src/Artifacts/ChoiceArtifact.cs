using System.Collections;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;

namespace LevelUpChoices.Artifacts
{
    public class ChoiceArtifact : IContentPackProvider
    {
        public static ArtifactDef ArtifactDef;

        private static string _basePath;

        public ContentPack contentPack = new();

        public string identifier => "LevelUpChoices.ChoiceArtifact";

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            ArtifactDef = ScriptableObject.CreateInstance<ArtifactDef>();
            ArtifactDef.cachedName = "ARTIFACT_CHOICE";
            ArtifactDef.nameToken = "ARTIFACT_CHOICE_NAME";
            ArtifactDef.descriptionToken = "ARTIFACT_CHOICE_DESC";

            if (!string.IsNullOrEmpty(_basePath))
            {
                string onPath = System.IO.Path.Combine(_basePath, "Assets", "AoC_On.png");
                string offPath = System.IO.Path.Combine(_basePath, "Assets", "AoC_Off.png");

                ArtifactDef.smallIconSelectedReference = ArtifactIcons.Load(onPath);
                ArtifactDef.smallIconDeselectedReference = ArtifactIcons.Load(offPath);
            }

            contentPack.artifactDefs.Add([ArtifactDef]);

            args.ReportProgress(1f);
            yield break;
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

            LanguageAPI.Add("ARTIFACT_CHOICE_NAME", "Artifact of Choice");
            LanguageAPI.Add("ARTIFACT_CHOICE_DESC", "Choose an item out of multiple options when you level up.");

            ContentManager.collectContentPackProviders += ContentManager_collectContentPackProviders;
        }

        private static void ContentManager_collectContentPackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
        {
            addContentPackProvider(new ChoiceArtifact());
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
