using System.Collections;
using System.Collections.Generic;
using LevelUpChoices.Extensions;
using LevelUpChoices.UI.Builders;
using LevelUpChoices.UI.Components;
using LevelUpChoices.UI.Constants;
using LevelUpChoices.UI.Integration;
using LevelUpChoices.UI.Services;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace LevelUpChoices.UI
{
    public class ItemSelectUI : MonoBehaviour
    {
        public static ItemSelectUI Instance;

        public bool IsVisible { get; private set; } = false;

        private GameObject canvasObject;
        private GameObject containerObject;

        private UIAssetService assetService;
        private UIElementBuilder elementBuilder;
        private CardBuilder cardBuilder;

        private TokenHeaderComponent tokenHeader;
        private BackdropPanelComponent backdrop;
        private NotificationPanelComponent notification;
        private ItemRowComponent itemRow;
        private UIPauseIntegration pauseIntegration;

        private float lastClickTime = 0f;
        private const float ClickCooldown = 0.15f;

        private void Awake()
        {
            if (Instance)
                Destroy(Instance);
            Instance = this;

            assetService = UIAssetService.Instance;
            elementBuilder = new UIElementBuilder(assetService);
            cardBuilder = new CardBuilder(assetService);

            BuildUI();
        }

        private void BuildUI()
        {
            canvasObject = new GameObject("LevelUpCanvas");
            DontDestroyOnLoad(canvasObject);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasObject.AddComponent<MPEventSystemProvider>().fallBackToMainEventSystem = true;
            canvasObject.AddComponent<MPEventSystemLocator>();

            // Pause integration
            pauseIntegration = GetComponent<UIPauseIntegration>();
            pauseIntegration?.SetCanvas(canvasObject);

            // Backdrop
            var backdropGo = elementBuilder.MakeUIObject("Backdrop", canvasObject.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var backdropImg = backdropGo.AddComponent<Image>();
            backdropImg.color = UIColors.Backdrop;
            backdropGo.SetActive(false);

            backdrop = canvasObject.AddComponent<BackdropPanelComponent>();
            backdrop.Initialize(backdropGo);

            // Container
            containerObject = elementBuilder.MakeUIObject("Container", canvasObject.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(1560f, 0f), Vector2.zero);
            containerObject.AddComponent<CursorOpener>();

            elementBuilder.AddPanel(containerObject, UIColors.DarkPanelBg, true, assetService.PanelSprite);

            elementBuilder.AddVerticalLayout(containerObject, new RectOffset(0, 0, 0, 0), 0f, false, true, false, true);
            containerObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Title bar
            var titleBarContainer = TitleBarComponent.Create(containerObject.transform, elementBuilder);
            var titleBarInner = titleBarContainer.transform.Find("TitleBar");

            // Token header overlay (Top-Left of TitleBar)
            var headerGo = elementBuilder.MakeUIObject("TokenHeader", titleBarInner,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(12f, 0f));
            elementBuilder.AddPanel(headerGo, new Color(0, 0, 0, 0.6f), false, null);

            var hLayout = elementBuilder.AddHorizontalLayout(headerGo, new RectOffset(16, 16, 6, 6), 24f, true, true, false, false);
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            var headerCsf = headerGo.AddComponent<ContentSizeFitter>();
            headerCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            headerCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var selectLabel = elementBuilder.MakeLabel(headerGo.transform, "SELECT", 18f,
                UIColors.HeaderText, TextAlignmentOptions.Center, false);
            selectLabel.fontStyle = FontStyles.Bold;

            var banishLabel = elementBuilder.MakeLabel(headerGo.transform, "BANISH", 18f,
                UIColors.HeaderText, TextAlignmentOptions.Center, false);
            banishLabel.fontStyle = FontStyles.Bold;

            var rerollLabel = elementBuilder.MakeLabel(headerGo.transform, "REROLL", 18f,
                UIColors.HeaderText, TextAlignmentOptions.Center, false);
            rerollLabel.fontStyle = FontStyles.Bold;

            tokenHeader = canvasObject.AddComponent<TokenHeaderComponent>();
            tokenHeader.Initialize(selectLabel, banishLabel, rerollLabel);

            // Item row
            var itemRowGo = new GameObject("ItemRow");
            itemRowGo.transform.SetParent(containerObject.transform, false);
            itemRow = itemRowGo.AddComponent<ItemRowComponent>();
            itemRow.Initialize(elementBuilder);

            containerObject.SetActive(false);

            // Notification panel
            var notifPanel = elementBuilder.MakeUIObject("NotificationPanel", canvasObject.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(280f, 68f), new Vector2(0f, 28f));

            elementBuilder.AddPanel(notifPanel, UIColors.NotificationBg, true, null);

            var notifInner = elementBuilder.MakeUIObject("NotifInner", notifPanel.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var notifInnerRect = notifInner.GetComponent<RectTransform>();
            notifInnerRect.offsetMin = new Vector2(2f, 2f);
            notifInnerRect.offsetMax = new Vector2(-2f, -2f);
            var notifInnerImg = notifInner.AddComponent<Image>();
            notifInnerImg.sprite = assetService.PanelSprite;
            notifInnerImg.type = Image.Type.Sliced;
            notifInnerImg.color = UIColors.NotificationInner;

            var notifAccent = elementBuilder.MakeUIObject("AccentBar", notifInner.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var notifAccentRect = notifAccent.GetComponent<RectTransform>();
            notifAccentRect.offsetMin = new Vector2(0f, -3f);
            notifAccentRect.offsetMax = new Vector2(0f, 0f);
            var notifAccentImg = notifAccent.AddComponent<Image>();
            notifAccentImg.color = UIColors.GoldAccent;

            var notifLabel = elementBuilder.MakeLabel(notifInner.transform, "LEVEL UP!\nPress F3", 18f,
                Color.white, TextAlignmentOptions.Center, false);
            var ntRect = notifLabel.GetComponent<RectTransform>();
            ntRect.anchorMin = Vector2.zero;
            ntRect.anchorMax = Vector2.one;
            ntRect.offsetMin = new Vector2(8f, 4f);
            ntRect.offsetMax = new Vector2(-8f, -4f);

            notification = canvasObject.AddComponent<NotificationPanelComponent>();
            notification.Initialize(notifPanel, notifLabel);
            notifPanel.SetActive(false);
        }

        public void UpdateTokens()
        {
            tokenHeader?.UpdateTokens(
                LevelUpManager.Instance.AvailableTokens,
                LevelUpManager.Instance.BanishTokens,
                LevelUpManager.Instance.RerollTokens);
        }

        public void ShowChoices(List<PickupIndex> pickupIndices, List<ItemIndex> synergies = null)
        {
            if (IsVisible)
                return;

            IsVisible = true;

            float scale = ModConfig.UIScale.Value;
            containerObject.GetComponent<RectTransform>().localScale = new Vector3(scale, scale, 1f);

            if (canvasObject && !canvasObject.activeSelf)
                canvasObject.SetActive(true);

            backdrop?.Show();
            containerObject.SetActive(true);
            UpdateTokens();
            UpdateOptions(pickupIndices, synergies);

            GamePauseManager.Pause();
        }

        public void UpdateOptions(List<PickupIndex> pickupIndices, List<ItemIndex> synergies = null)
        {
            if (itemRow == null)
                return;

            itemRow.ClearCards();

            for (int i = 0; i < pickupIndices.Count; i++)
            {
                int slotIndex = i;
                var pickupIndex = pickupIndices[i];
                var synergy = synergies != null && i < synergies.Count ? synergies[i] : ItemIndex.None;

                var card = cardBuilder.CreateCard(
                    itemRow.transform,
                    pickupIndex,
                    slotIndex,
                    synergy,
                    OnItemClicked,
                    OnBanishClicked,
                    OnRerollClicked);

                itemRow.AddCard(card);
            }

            itemRow.RebuildLayout();
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerObject.GetComponent<RectTransform>());
        }

        public void Hide()
        {
            if (!IsVisible)
                return;
            IsVisible = false;
            GamePauseManager.Unpause();
            StartCoroutine(HideNextFrame());
        }

        private IEnumerator HideNextFrame()
        {
            yield return null;
            EventSystem.current?.SetSelectedGameObject(null);
            containerObject.SetActive(false);
            backdrop?.Hide();
        }

        public void ShowNotification()
        {
            if (!ModConfig.EnableNotifications.Value)
                return;
            string keyName = ModConfig.ToggleMenuKey.Value.MainKey.ToString();
            notification?.Show(LevelUpManager.Instance.AvailableTokens, keyName);
        }

        private void Update()
        {
            bool isGamePaused = (pauseIntegration != null && pauseIntegration.IsPaused) || Time.timeScale == 0f;
            bool showNotif = ModConfig.EnableNotifications.Value
                && LevelUpManager.Instance != null
                && LevelUpManager.Instance.AvailableTokens > 0
                && !IsVisible && !isGamePaused;

            if (showNotif && notification != null)
            {
                string keyName = ModConfig.ToggleMenuKey.Value.MainKey.ToString();
                notification.Show(LevelUpManager.Instance.AvailableTokens, keyName);
            }
            else
            {
                notification?.Hide();
            }
        }

        private void OnItemClicked(PickupIndex pickupIndex)
        {
            if (Time.unscaledTime - lastClickTime < ClickCooldown)
                return;
            lastClickTime = Time.unscaledTime;

            UISoundManager.PlayClick();

            if (!LevelUpManager.Instance.SpendTokenLocal())
                return;

            var localUsers = NetworkUser.readOnlyLocalPlayersList;
            if (localUsers.Count == 0)
                return;
            var netId = localUsers[0].netId;

            if (NetworkServer.active)
            {
                LevelUpManager.Instance.HandlePlayerSelection(netId, pickupIndex);
            }
            else
            {
                new Networking.SendItemSelection(pickupIndex, netId)
                    .Send(R2API.Networking.NetworkDestination.Server);
            }
        }

        private void OnBanishClicked(int slotIndex)
        {
            if (LevelUpManager.Instance.BanishTokens <= 0)
                return;

            UISoundManager.PlayClick();

            var localUsers = NetworkUser.readOnlyLocalPlayersList;
            if (localUsers.Count == 0)
                return;
            var netId = localUsers[0].netId;

            if (NetworkServer.active)
            {
                LevelUpManager.Instance.HandlePlayerBanish(netId, slotIndex);
            }
            else
            {
                new Networking.SendBanish(slotIndex, netId)
                    .Send(R2API.Networking.NetworkDestination.Server);
            }
        }

        private void OnRerollClicked(int slotIndex)
        {
            if (LevelUpManager.Instance.RerollTokens <= 0)
                return;

            UISoundManager.PlayClick();

            var localUsers = NetworkUser.readOnlyLocalPlayersList;
            if (localUsers.Count == 0)
                return;
            var netId = localUsers[0].netId;

            if (NetworkServer.active)
            {
                LevelUpManager.Instance.HandlePlayerReroll(netId, slotIndex);
            }
            else
            {
                new Networking.SendReroll(slotIndex, netId)
                    .Send(R2API.Networking.NetworkDestination.Server);
            }
        }
    }
}
