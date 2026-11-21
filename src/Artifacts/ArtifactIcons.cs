using System;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace LevelUpChoices.Artifacts
{
    internal sealed class ArtifactIcons : ResourceProviderBase
    {
        private static readonly ArtifactIcons Provider = new();
        private static readonly ResourceLocationMap Locations = new(LevelUpChoices.PluginGUID + ".ArtifactIcons");

        static ArtifactIcons()
        {
            Addressables.ResourceManager.ResourceProviders.Add(Provider);
            Addressables.AddResourceLocator(Locations);
        }

        internal static AssetReferenceT<Sprite> Load(string path)
        {
            if (!File.Exists(path))
                return new AssetReferenceT<Sprite>(string.Empty);

            Texture2D texture = new(256, 256, TextureFormat.ARGB32, false, false);
            if (!texture.LoadImage(File.ReadAllBytes(path)))
            {
                UnityEngine.Object.Destroy(texture);
                return new AssetReferenceT<Sprite>(string.Empty);
            }

            texture.filterMode = FilterMode.Point;
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 1, SpriteMeshType.Tight, Vector4.zero, true);
            // AssetReference validates the runtime key as a GUID.
            string key = Guid.NewGuid().ToString("N");
            Locations.Add(key, new ResourceLocationBase(key, key, Provider.ProviderId, typeof(Sprite)) { Data = sprite });
            return new AssetReferenceT<Sprite>(key);
        }

        public override Type GetDefaultType(IResourceLocation location) => typeof(Sprite);

        public override void Provide(ProvideHandle handle)
        {
            handle.Complete((Sprite)handle.Location.Data, true, null);
        }
    }
}
