using UnityEngine;
using UnityEngine.AddressableAssets;

namespace LevelUpChoices.UI.Services
{
    public class UIAssetService
    {
        public const string PanelSpriteLocation = "RoR2/Base/UI/texUICleanButton.png";

        private static UIAssetService _instance;
        public static UIAssetService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UIAssetService();
                    _instance.LoadAssets();
                }
                return _instance;
            }
        }

        public Sprite PanelSprite { get; private set; } = null!;

        private void LoadAssets()
        {
            var panelTask = Addressables.LoadAssetAsync<Sprite>(PanelSpriteLocation);
            PanelSprite = panelTask.WaitForCompletion();
            if (PanelSprite == null)
            {
                Log.Error("Failed to load panel sprite: " + PanelSpriteLocation);
            }

        }
    }
}
