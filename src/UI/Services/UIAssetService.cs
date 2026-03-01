using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace LevelUpChoices.UI.Services
{
    public class UIAssetService
    {
        public const string PanelSpriteLocation = "RoR2/Base/UI/texUICleanButton.png";

        public static UIAssetService Instance
        {
            get
            {
                if (field == null)
                {
                    field = new UIAssetService();
                    field.LoadAssets();
                }
                return field;
            }
        }

        public Sprite PanelSprite { get; private set; } = null!;

        private void LoadAssets()
        {
            AsyncOperationHandle<Sprite> panelTask = Addressables.LoadAssetAsync<Sprite>(PanelSpriteLocation);
            PanelSprite = panelTask.WaitForCompletion();
            if (PanelSprite == null)
            {
                Log.Error("Failed to load panel sprite: " + PanelSpriteLocation);
            }

        }
    }
}
