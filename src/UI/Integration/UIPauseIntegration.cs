using UnityEngine;

namespace LevelUpChoices.UI.Integration {
    public class UIPauseIntegration : MonoBehaviour {
        private GameObject _canvasObject;

        public bool IsPaused { get; private set; } = false;

        public void SetCanvas(GameObject canvasObject) {
            _canvasObject = canvasObject;
        }

        private void OnEnable() {
            On.RoR2.UI.PauseScreenController.OnEnable += OnPauseScreenEnabled;
            On.RoR2.UI.PauseScreenController.OnDisable += OnPauseScreenDisabled;
        }

        private void OnDisable() {
            On.RoR2.UI.PauseScreenController.OnEnable -= OnPauseScreenEnabled;
            On.RoR2.UI.PauseScreenController.OnDisable -= OnPauseScreenDisabled;
        }

        private void OnPauseScreenEnabled(On.RoR2.UI.PauseScreenController.orig_OnEnable orig, RoR2.UI.PauseScreenController self) {
            orig(self);
            IsPaused = true;

            if (GamePauseManager.IsPausedByUs)
                return;

            if (_canvasObject)
                _canvasObject.SetActive(false);
        }

        private void OnPauseScreenDisabled(On.RoR2.UI.PauseScreenController.orig_OnDisable orig, RoR2.UI.PauseScreenController self) {
            orig(self);
            IsPaused = false;

            if (GamePauseManager.IsPausedByUs)
                return;

            if (_canvasObject)
                _canvasObject.SetActive(true);
        }

        private void OnDestroy() {
            On.RoR2.UI.PauseScreenController.OnEnable -= OnPauseScreenEnabled;
            On.RoR2.UI.PauseScreenController.OnDisable -= OnPauseScreenDisabled;
        }
    }
}
