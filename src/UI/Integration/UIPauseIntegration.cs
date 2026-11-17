using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace LevelUpChoices.UI.Integration
{
    public class UIPauseIntegration : MonoBehaviour
    {
        private GameObject _canvasObject;

        public bool IsPaused { get; private set; } = false;

        public void SetCanvas(GameObject canvasObject)
        {
            _canvasObject = canvasObject;
        }

        private void OnEnable()
        {
            On.RoR2.UI.PauseScreenController.OnEnable += OnPauseScreenEnabled;
            On.RoR2.UI.PauseScreenController.OnDisable += OnPauseScreenDisabled;
            On.RoR2.UI.PauseScreenController.OpenGameMenu += OnGameMenuOpened;
            On.RoR2.PauseStopController.HandlePauseStop += OnPauseRequested;
            On.RoR2.PauseStopController.ClientPauseStop += OnClientPauseChanged;
        }

        private void OnDisable()
        {
            On.RoR2.UI.PauseScreenController.OnEnable -= OnPauseScreenEnabled;
            On.RoR2.UI.PauseScreenController.OnDisable -= OnPauseScreenDisabled;
            On.RoR2.UI.PauseScreenController.OpenGameMenu -= OnGameMenuOpened;
            On.RoR2.PauseStopController.HandlePauseStop -= OnPauseRequested;
            On.RoR2.PauseStopController.ClientPauseStop -= OnClientPauseChanged;
        }

        private void OnPauseScreenEnabled(On.RoR2.UI.PauseScreenController.orig_OnEnable orig, RoR2.UI.PauseScreenController self)
        {
            orig(self);

            if (NetworkServer.active && NetworkServer.dontListen)
            {
                // Restore the running speed before the native pause captures it.
                GamePauseManager.SuspendSinglePlayerPause();
                SetMenuOpen(true);
            }
        }

        private void OnPauseScreenDisabled(On.RoR2.UI.PauseScreenController.orig_OnDisable orig, RoR2.UI.PauseScreenController self)
        {
            SetMenuOpen(HasGameMenu(self));
            orig(self);
        }

        private void OnGameMenuOpened(On.RoR2.UI.PauseScreenController.orig_OpenGameMenu orig, RoR2.UI.PauseScreenController self)
        {
            orig(self);
            SetMenuOpen(true);
        }

        private void LateUpdate()
        {
            SetMenuOpen(HasGameMenu(null));
        }

        private static bool HasGameMenu(RoR2.UI.PauseScreenController excluded)
        {
            foreach (var screen in RoR2.UI.PauseScreenController.instancesList)
            {
                if (screen && screen != excluded && screen.isActiveAndEnabled &&
                    screen.gameMenuPanel && screen.gameMenuPanel.gameObject.activeInHierarchy)
                    return true;
            }

            return false;
        }

        private void SetMenuOpen(bool isOpen)
        {
            if (IsPaused == isOpen)
                return;

            IsPaused = isOpen;
            if (_canvasObject)
                _canvasObject.SetActive(!isOpen);
        }

        private static void OnPauseRequested(On.RoR2.PauseStopController.orig_HandlePauseStop orig, NetworkMessage message)
        {
            // Esc/Resume and other clients cannot release a pause owned by active pickers.
            var self = PauseStopController.instance;
            if (NetworkServer.active && !NetworkServer.dontListen && self && self.allowMultiplayerPause &&
                ModConfig.PauseOnItemSelect.Value && GamePauseManager.HasActivePickers && self.isPaused)
                return;

            orig(message);
        }

        private void OnClientPauseChanged(On.RoR2.PauseStopController.orig_ClientPauseStop orig, PauseStopController self, bool shouldPause)
        {
            orig(self, shouldPause);
            if (!shouldPause && !IsPaused)
                GamePauseManager.ResumeSinglePlayerPause();
        }
    }
}
