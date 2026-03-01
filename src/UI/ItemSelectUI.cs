using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public static ItemSelectUI Instance { get; private set; }

        public bool IsVisible { get; private set; } = false;

        private GameObject _canvasObject;
        private GameObject _containerObject;

        private UIAssetService _assetService;
        private UIElementBuilder _elementBuilder;
        private CardBuilder _cardBuilder;

        private TokenHeaderComponent _tokenHeader;
        private BackdropPanelComponent _backdrop;
        private NotificationPanelComponent _notification;
        private ItemRowComponent _itemRow;
        private UIPauseIntegration _pauseIntegration;

        private float _lastClickTime = 0f;
        private const float ClickCooldown = 0.15f;

        private void Awake()
        {
            if (Instance)
                Destroy(Instance);
            Instance = this;

            _assetService = UIAssetService.Instance;
            _elementBuilder = new UIElementBuilder(_assetService);
            _cardBuilder = new CardBuilder(_assetService);

            BuildUI();
        }

        private void BuildUI()
        {
            _canvasObject = new GameObject("LevelUpCanvas");
            DontDestroyOnLoad(_canvasObject);
            Canvas canvas = _canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = _canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            _canvasObject.AddComponent<GraphicRaycaster>();
            _canvasObject.AddComponent<MPEventSystemProvider>().fallBackToMainEventSystem = true;
            _canvasObject.AddComponent<MPEventSystemLocator>();

            // Pause integration
            _pauseIntegration = GetComponent<UIPauseIntegration>();
            _pauseIntegration?.SetCanvas(_canvasObject);

            // Backdrop
            GameObject backdropGo = UIElementBuilder.MakeUIObject("Backdrop", _canvasObject.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image backdropImg = backdropGo.AddComponent<Image>();
            backdropImg.color = UIColors.Backdrop;
            backdropGo.SetActive(false);

            _backdrop = _canvasObject.AddComponent<BackdropPanelComponent>();
            _backdrop.Initialize(backdropGo);

            // Container
            _containerObject = UIElementBuilder.MakeUIObject("Container", _canvasObject.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(1560f, 0f), Vector2.zero);
            _containerObject.AddComponent<CursorOpener>();

            UIElementBuilder.AddPanel(_containerObject, UIColors.DarkPanelBg, true, _assetService.PanelSprite);

            UIElementBuilder.AddVerticalLayout(_containerObject, new RectOffset(0, 0, 0, 0), 0f, false, true, false, true);
            _containerObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Title bar
            GameObject titleBarContainer = TitleBarComponent.Create(_containerObject.transform);
            Transform titleBarInner = titleBarContainer.transform.Find("TitleBar");

            // Token header overlay (Top-Left of TitleBar)
            GameObject headerGo = UIElementBuilder.MakeUIObject("TokenHeader", titleBarInner,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(12f, 0f));
            UIElementBuilder.AddPanel(headerGo, new Color(0, 0, 0, 0.6f), false, null);

            HorizontalLayoutGroup hLayout = UIElementBuilder.AddHorizontalLayout(headerGo, new RectOffset(16, 16, 6, 6), 24f, true, true, false, false);
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            ContentSizeFitter headerCsf = headerGo.AddComponent<ContentSizeFitter>();
            headerCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            headerCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            HGTextMeshProUGUI selectLabel = UIElementBuilder.MakeLabel(headerGo.transform, "SELECT", 18f,
                UIColors.HeaderText, TextAlignmentOptions.Center, false);
            selectLabel.fontStyle = FontStyles.Bold;

            HGTextMeshProUGUI banishLabel = UIElementBuilder.MakeLabel(headerGo.transform, "BANISH", 18f,
                UIColors.HeaderText, TextAlignmentOptions.Center, false);
            banishLabel.fontStyle = FontStyles.Bold;

            HGTextMeshProUGUI rerollLabel = UIElementBuilder.MakeLabel(headerGo.transform, "REROLL", 18f,
                UIColors.HeaderText, TextAlignmentOptions.Center, false);
            rerollLabel.fontStyle = FontStyles.Bold;

            _tokenHeader = _canvasObject.AddComponent<TokenHeaderComponent>();
            _tokenHeader.Initialize(selectLabel, banishLabel, rerollLabel);

            // Item row
            var itemRowGo = new GameObject("ItemRow");
            itemRowGo.transform.SetParent(_containerObject.transform, false);
            _itemRow = itemRowGo.AddComponent<ItemRowComponent>();
            _itemRow.Initialize();

            _containerObject.SetActive(false);

            // Notification panel
            GameObject notifPanel = UIElementBuilder.MakeUIObject("NotificationPanel", _canvasObject.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(280f, 68f), new Vector2(0f, 28f));

            UIElementBuilder.AddPanel(notifPanel, UIColors.NotificationBg, true, null);

            GameObject notifInner = UIElementBuilder.MakeUIObject("NotifInner", notifPanel.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            RectTransform notifInnerRect = notifInner.GetComponent<RectTransform>();
            notifInnerRect.offsetMin = new Vector2(2f, 2f);
            notifInnerRect.offsetMax = new Vector2(-2f, -2f);
            Image notifInnerImg = notifInner.AddComponent<Image>();
            notifInnerImg.sprite = _assetService.PanelSprite;
            notifInnerImg.type = Image.Type.Sliced;
            notifInnerImg.color = UIColors.NotificationInner;

            GameObject notifAccent = UIElementBuilder.MakeUIObject("AccentBar", notifInner.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            RectTransform notifAccentRect = notifAccent.GetComponent<RectTransform>();
            notifAccentRect.offsetMin = new Vector2(0f, -3f);
            notifAccentRect.offsetMax = new Vector2(0f, 0f);
            Image notifAccentImg = notifAccent.AddComponent<Image>();
            notifAccentImg.color = UIColors.GoldAccent;

            HGTextMeshProUGUI notifLabel = UIElementBuilder.MakeLabel(notifInner.transform, "LEVEL UP!\nPress F3", 18f,
                Color.white, TextAlignmentOptions.Center, false);
            RectTransform ntRect = notifLabel.GetComponent<RectTransform>();
            ntRect.anchorMin = Vector2.zero;
            ntRect.anchorMax = Vector2.one;
            ntRect.offsetMin = new Vector2(8f, 4f);
            ntRect.offsetMax = new Vector2(-8f, -4f);

            _notification = _canvasObject.AddComponent<NotificationPanelComponent>();
            _notification.Initialize(notifPanel, notifLabel);
            notifPanel.SetActive(false);
        }

        public void UpdateTokens()
        {
            _tokenHeader?.UpdateTokens(
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
            _containerObject.GetComponent<RectTransform>().localScale = new Vector3(scale, scale, 1f);

            if (_canvasObject && !_canvasObject.activeSelf)
                _canvasObject.SetActive(true);

            _backdrop?.Show();
            _containerObject.SetActive(true);
            UpdateTokens();
            UpdateOptions(pickupIndices, synergies);

            GamePauseManager.Pause();
        }

        public void UpdateOptions(List<PickupIndex> pickupIndices, List<ItemIndex> synergies = null)
        {
            if (_itemRow == null)
                return;

            _itemRow.ClearCards();

            for (int i = 0; i < pickupIndices.Count; i++)
            {
                int slotIndex = i;
                PickupIndex pickupIndex = pickupIndices[i];
                ItemIndex synergy = synergies != null && i < synergies.Count ? synergies[i] : ItemIndex.None;

                GameObject card = _cardBuilder.CreateCard(
                    _itemRow.transform,
                    pickupIndex,
                    slotIndex,
                    synergy,
                    OnItemClicked,
                    OnBanishClicked,
                    OnRerollClicked);

                _itemRow.AddCard(card);
            }

            _itemRow.RebuildLayout();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_containerObject.GetComponent<RectTransform>());
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
            _containerObject.SetActive(false);
            _backdrop?.Hide();
        }

        public void ShowNotification()
        {
            if (!ModConfig.EnableNotifications.Value)
                return;
            string keyName = ModConfig.ToggleMenuKey.Value.MainKey.ToString();
            _notification?.Show(LevelUpManager.Instance.AvailableTokens, keyName);
        }

        private void Update()
        {
            bool isGamePaused = (_pauseIntegration != null && _pauseIntegration.IsPaused) || Time.timeScale == 0f;
            bool showNotif = ModConfig.EnableNotifications.Value
                && LevelUpManager.Instance != null
                && LevelUpManager.Instance.AvailableTokens > 0
                && !IsVisible && !isGamePaused;

            if (showNotif && _notification != null)
            {
                string keyName = ModConfig.ToggleMenuKey.Value.MainKey.ToString();
                _notification.Show(LevelUpManager.Instance.AvailableTokens, keyName);
            }
            else
            {
                _notification?.Hide();
            }
        }

        private void OnItemClicked(PickupIndex pickupIndex)
        {
            if (Time.unscaledTime - _lastClickTime < ClickCooldown)
                return;
            _lastClickTime = Time.unscaledTime;

            UISoundManager.PlayClick();

            if (!LevelUpManager.Instance.SpendTokenLocal())
                return;

            ReadOnlyCollection<NetworkUser> localUsers = NetworkUser.readOnlyLocalPlayersList;
            if (localUsers.Count == 0)
                return;
            NetworkInstanceId netId = localUsers[0].netId;

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

            ReadOnlyCollection<NetworkUser> localUsers = NetworkUser.readOnlyLocalPlayersList;
            if (localUsers.Count == 0)
                return;
            NetworkInstanceId netId = localUsers[0].netId;

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

            ReadOnlyCollection<NetworkUser> localUsers = NetworkUser.readOnlyLocalPlayersList;
            if (localUsers.Count == 0)
                return;
            NetworkInstanceId netId = localUsers[0].netId;

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
