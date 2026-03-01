using System.Collections.Generic;
using System.Collections.ObjectModel;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace LevelUpChoices {
    // Handles pausing/unpausing the game when the item selection UI is shown.
    // Singleplayer: Directly sets Time.timeScale = 0/1.
    // Multiplayer: Server tracks how many players are currently picking. Pauses when the first picker opens the UI,
    // unpauses when the last picker closes it. Uses the native PauseStopController when multiplayer pause is enabled.
    public static class GamePauseManager {
        private static float s_savedTimeScale = 1f;

        public static bool IsPausedByUs { get; private set; } = false;

        private static readonly HashSet<NetworkInstanceId> s_activePickers = [];

        public static void Pause() {
            if (!ModConfig.PauseOnItemSelect.Value)
                return;
            if (IsPausedByUs)
                return;

            if (IsSinglePlayer()) {
                PauseSinglePlayer();
            }
            else {
                // Tell the server we're picking — the server decides when to pause/unpause
                ReadOnlyCollection<NetworkUser> localUser = NetworkUser.readOnlyLocalPlayersList;
                if (localUser.Count > 0) {
                    new Networking.SendPickingState(localUser[0].netId, true)
                        .Send(R2API.Networking.NetworkDestination.Server);
                }
                IsPausedByUs = true;
            }
        }

        public static void Unpause() {
            if (!IsPausedByUs)
                return;

            if (IsSinglePlayer()) {
                UnpauseSinglePlayer();
            }
            else {
                // Tell the server we're done — server unpauses when all pickers are done
                ReadOnlyCollection<NetworkUser> localUser = NetworkUser.readOnlyLocalPlayersList;
                if (localUser.Count > 0) {
                    new Networking.SendPickingState(localUser[0].netId, false)
                        .Send(R2API.Networking.NetworkDestination.Server);
                }
            }

            IsPausedByUs = false;
        }

        // Force-unpause as a safety net (e.g. on run end or disconnect).
        public static void ForceReset() {
            if (IsPausedByUs) {
                if (Time.timeScale == 0f)
                    Time.timeScale = s_savedTimeScale > 0f ? s_savedTimeScale : 1f;
            }

            IsPausedByUs = false;
            s_savedTimeScale = 1f;
            s_activePickers.Clear();
        }

        // Server-only. Called when a client reports they started or stopped picking.
        // Tracks the count of active pickers and pauses/unpauses accordingly.
        public static void HandlePickingState(NetworkInstanceId netId, bool isPicking) {
            if (!NetworkServer.active)
                return;
            if (!ModConfig.PauseOnItemSelect.Value)
                return;

            int previousCount = s_activePickers.Count;

            if (isPicking)
                s_activePickers.Add(netId);
            else
                s_activePickers.Remove(netId);

            int currentCount = s_activePickers.Count;

            // 0 → 1+ pickers → pause
            if (previousCount == 0 && currentCount > 0) {
                ServerPause();
            }
            // 1+ → 0 pickers → unpause
            else if (previousCount > 0 && currentCount == 0) {
                ServerUnpause();
            }
        }

        // Server-only. Remove a player from the active pickers (e.g. on disconnect).
        public static void RemovePicker(NetworkInstanceId netId) {
            if (!NetworkServer.active)
                return;

            if (s_activePickers.Remove(netId) && s_activePickers.Count == 0) {
                ServerUnpause();
            }
        }

        private static bool IsSinglePlayer() {
            // On the host/server, dontListen means singleplayer (no remote clients)
            if (NetworkServer.active)
                return NetworkServer.dontListen;
            // On a client connected to a remote server, it's multiplayer
            // NetworkClient.active == true means we're connected to a host
            return !NetworkClient.active;
        }

        private static void PauseSinglePlayer() {
            s_savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            IsPausedByUs = true;
        }

        private static void UnpauseSinglePlayer() {
            Time.timeScale = s_savedTimeScale > 0f ? s_savedTimeScale : 1f;
        }

        private static void ServerPause() {
            PauseStopController controller = PauseStopController.instance;
            if (controller != null && controller.allowMultiplayerPause) {
                controller.Pause(true);
            }
        }

        private static void ServerUnpause() {
            PauseStopController controller = PauseStopController.instance;
            controller?.Pause(false);
        }
    }
}
