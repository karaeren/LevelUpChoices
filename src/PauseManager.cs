using System.Collections.Generic;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace LevelUpChoices
{
    // Handles pausing/unpausing the game when the item selection UI is shown.
    // Singleplayer: Directly sets Time.timeScale = 0/1.
    // Multiplayer: Server tracks how many players are currently picking. Pauses when the first picker opens the UI,
    // unpauses when the last picker closes it. Uses the native PauseStopController when multiplayer pause is enabled.
    public static class GamePauseManager
    {
        private static float savedTimeScale = 1f;
        private static bool pausedByUs = false;
        public static bool IsPausedByUs => pausedByUs;

        private static readonly HashSet<NetworkInstanceId> activePickers = [];

        public static void Pause()
        {
            if (!ModConfig.PauseOnItemSelect.Value)
                return;
            if (pausedByUs)
                return;

            if (IsSinglePlayer())
            {
                PauseSinglePlayer();
            }
            else
            {
                // Tell the server we're picking — the server decides when to pause/unpause
                var localUser = NetworkUser.readOnlyLocalPlayersList;
                if (localUser.Count > 0)
                {
                    new Networking.SendPickingState(localUser[0].netId, true)
                        .Send(R2API.Networking.NetworkDestination.Server);
                }
                pausedByUs = true;
            }
        }

        public static void Unpause()
        {
            if (!pausedByUs)
                return;

            if (IsSinglePlayer())
            {
                UnpauseSinglePlayer();
            }
            else
            {
                // Tell the server we're done — server unpauses when all pickers are done
                var localUser = NetworkUser.readOnlyLocalPlayersList;
                if (localUser.Count > 0)
                {
                    new Networking.SendPickingState(localUser[0].netId, false)
                        .Send(R2API.Networking.NetworkDestination.Server);
                }
            }

            pausedByUs = false;
        }

        // Force-unpause as a safety net (e.g. on run end or disconnect).
        public static void ForceReset()
        {
            if (pausedByUs)
            {
                if (Time.timeScale == 0f)
                    Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;
            }

            pausedByUs = false;
            savedTimeScale = 1f;
            activePickers.Clear();
        }

        // Server-only. Called when a client reports they started or stopped picking.
        // Tracks the count of active pickers and pauses/unpauses accordingly.
        public static void HandlePickingState(NetworkInstanceId netId, bool isPicking)
        {
            if (!NetworkServer.active)
                return;
            if (!ModConfig.PauseOnItemSelect.Value)
                return;

            int previousCount = activePickers.Count;

            if (isPicking)
                activePickers.Add(netId);
            else
                activePickers.Remove(netId);

            int currentCount = activePickers.Count;

            // 0 → 1+ pickers → pause
            if (previousCount == 0 && currentCount > 0)
            {
                ServerPause();
            }
            // 1+ → 0 pickers → unpause
            else if (previousCount > 0 && currentCount == 0)
            {
                ServerUnpause();
            }
        }

        // Server-only. Remove a player from the active pickers (e.g. on disconnect).
        public static void RemovePicker(NetworkInstanceId netId)
        {
            if (!NetworkServer.active)
                return;

            if (activePickers.Remove(netId) && activePickers.Count == 0)
            {
                ServerUnpause();
            }
        }

        private static bool IsSinglePlayer()
        {
            // On the host/server, dontListen means singleplayer (no remote clients)
            if (NetworkServer.active)
                return NetworkServer.dontListen;
            // On a client connected to a remote server, it's multiplayer
            // NetworkClient.active == true means we're connected to a host
            return !NetworkClient.active;
        }

        private static void PauseSinglePlayer()
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            pausedByUs = true;
        }

        private static void UnpauseSinglePlayer()
        {
            Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;
        }

        private static void ServerPause()
        {
            var controller = PauseStopController.instance;
            if (controller != null && controller.allowMultiplayerPause)
            {
                controller.Pause(true);
            }
        }

        private static void ServerUnpause()
        {
            var controller = PauseStopController.instance;
            controller?.Pause(false);
        }
    }
}
