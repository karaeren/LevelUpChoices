using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace LevelUpChoices
{
    public class LevelUpUI : MonoBehaviour
    {
        public static LevelUpUI Instance;

        public bool IsVisible { get; private set; } = false;

        private GameObject canvasObject;
        private GameObject containerObject;
        private GameObject notificationPanel;
        private HGTextMeshProUGUI notificationText;
        private HGTextMeshProUGUI tokenHeader;

        private TMP_FontAsset ror2Font;
        private Sprite panelSprite;
        private Sprite buttonSprite;

        private List<GameObject> buttonObjects = new List<GameObject>();
        private bool isPaused = false;

        private void Awake()
        {
            if (Instance) Destroy(Instance);
            Instance = this;

            ror2Font = Addressables.LoadAssetAsync<TMP_FontAsset>("RoR2/Base/Common/Fonts/Bombardier.asset").WaitForCompletion();
            panelSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/UI/texUICleanButton.png").WaitForCompletion();
            buttonSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/UI/texUIHighlightBoxOutlineThick.png").WaitForCompletion();

            BuildUI();

            On.RoR2.UI.PauseScreenController.OnEnable += OnPauseScreenEnabled;
            On.RoR2.UI.PauseScreenController.OnDisable += OnPauseScreenDisabled;
        }

        private void OnDestroy()
        {
            On.RoR2.UI.PauseScreenController.OnEnable -= OnPauseScreenEnabled;
            On.RoR2.UI.PauseScreenController.OnDisable -= OnPauseScreenDisabled;
        }

        private void OnPauseScreenEnabled(On.RoR2.UI.PauseScreenController.orig_OnEnable orig, RoR2.UI.PauseScreenController self)
        {
            orig(self);
            isPaused = true;
            if (canvasObject) canvasObject.SetActive(false);
        }

        private void OnPauseScreenDisabled(On.RoR2.UI.PauseScreenController.orig_OnDisable orig, RoR2.UI.PauseScreenController self)
        {
            orig(self);
            isPaused = false;
            if (canvasObject) canvasObject.SetActive(true);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private HGTextMeshProUGUI MakeLabel(Transform parent, string text, float fontSize, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center, bool wordWrap = false)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<HGTextMeshProUGUI>();
            if (ror2Font) tmp.font = ror2Font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.enableWordWrapping = wordWrap;
            return tmp;
        }

        private Image MakePanel(Transform parent, Color color, bool sliced = true)
        {
            var img = parent.gameObject.AddComponent<Image>();
            if (panelSprite)
            {
                img.sprite = panelSprite;
                img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            }
            img.color = color;
            return img;
        }

        private Color GetTierColor(ItemDef itemDef)
        {
            if (itemDef == null) return Color.white;
            var tierDef = ItemTierCatalog.GetItemTierDef(itemDef.tier);
            if (tierDef == null) return Color.white;
            return ColorCatalog.GetColor(tierDef.colorIndex);
        }

        // ─── Build ────────────────────────────────────────────────────────────────

        private void BuildUI()
        {
            // Canvas
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

            // ── Main container ───────────────────────────────────────────────────
            containerObject = new GameObject("Container");
            containerObject.transform.SetParent(canvasObject.transform, false);
            var containerRect = containerObject.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.sizeDelta = new Vector2(1560f, 0f);
            containerRect.anchoredPosition = Vector2.zero;
            containerObject.AddComponent<CursorOpener>();

            MakePanel(containerObject.transform, new Color(0.04f, 0.04f, 0.06f, 0.97f));

            var mainLayout = containerObject.AddComponent<VerticalLayoutGroup>();
            mainLayout.childControlHeight = false;
            mainLayout.childControlWidth = true;
            mainLayout.childForceExpandHeight = false;
            mainLayout.spacing = 4;
            mainLayout.padding = new RectOffset(12, 12, 10, 10);
            containerObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            // ── Title bar ────────────────────────────────────────────────────────
            var titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(containerObject.transform, false);
            var titleBarImg = titleBar.AddComponent<Image>();
            titleBarImg.color = new Color(1f, 0.82f, 0.18f, 0.12f);   // subtle gold tint
            var titleLe = titleBar.AddComponent<LayoutElement>();
            titleLe.preferredHeight = 36;

            var titleTmp = MakeLabel(titleBar.transform, "CHOOSE AN ITEM",
                32f, new Color(1f, 0.88f, 0.35f));
            var titleTmpRect = titleTmp.GetComponent<RectTransform>();
            titleTmpRect.anchorMin = Vector2.zero;
            titleTmpRect.anchorMax = Vector2.one;

            // ── Token header ─────────────────────────────────────────────────────
            var headerGo = new GameObject("TokenHeader");
            headerGo.transform.SetParent(containerObject.transform, false);
            var headerLe = headerGo.AddComponent<LayoutElement>();
            headerLe.preferredHeight = 24;
            tokenHeader = MakeLabel(headerGo.transform, "Loading...", 24f, new Color(0.75f, 0.88f, 1f));
            var thRect = tokenHeader.GetComponent<RectTransform>();
            thRect.anchorMin = Vector2.zero;
            thRect.anchorMax = Vector2.one;

            // ── Item row ─────────────────────────────────────────────────────────
            var itemRow = new GameObject("ItemRow");
            itemRow.transform.SetParent(containerObject.transform, false);
            var rowLayout = itemRow.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;
            rowLayout.spacing = 16;
            itemRow.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            containerObject.SetActive(false);

            // ── Notification badge ───────────────────────────────────────────────
            notificationPanel = new GameObject("NotificationPanel");
            notificationPanel.transform.SetParent(canvasObject.transform, false);
            var notifRect = notificationPanel.AddComponent<RectTransform>();
            notifRect.anchorMin = new Vector2(0.5f, 0f);
            notifRect.anchorMax = new Vector2(0.5f, 0f);
            notifRect.pivot = new Vector2(0.5f, 0f);
            notifRect.anchoredPosition = new Vector2(0f, 24f);
            notifRect.sizeDelta = new Vector2(260f, 62f);

            var notifImg = notificationPanel.AddComponent<Image>();
            if (panelSprite) { notifImg.sprite = panelSprite; notifImg.type = Image.Type.Sliced; }
            notifImg.color = new Color(0.9f, 0.68f, 0.08f, 0.88f);

            // Gold top-edge accent
            var accent = new GameObject("Accent");
            accent.transform.SetParent(notificationPanel.transform, false);
            var accentRect = accent.AddComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 1f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.offsetMin = new Vector2(0f, -5f);
            accentRect.offsetMax = new Vector2(0f, 0f);
            var accentImg = accent.AddComponent<Image>();
            accentImg.color = new Color(1f, 0.88f, 0.18f, 1f);

            notificationText = MakeLabel(notificationPanel.transform, "LEVEL UP!\nPress F3", 18f, Color.white);
            var ntRect = notificationText.GetComponent<RectTransform>();
            ntRect.anchorMin = Vector2.zero;
            ntRect.anchorMax = Vector2.one;

            notificationPanel.SetActive(false);
        }

        public void UpdateTokens()
        {
            if (tokenHeader)
                tokenHeader.text =
                    $"<color=#FFE066>SELECT</color>  {LevelUpManager.Instance.AvailableTokens}   " +
                    $"<color=#FF6060>BANISH</color>  {LevelUpManager.Instance.BanishTokens}   " +
                    $"<color=#60AAFF>REROLL</color>  {LevelUpManager.Instance.RerollTokens}";
        }

        public void ShowChoices(List<PickupIndex> pickupIndices)
        {
            if (IsVisible) return;
            IsVisible = true;
            containerObject.SetActive(true);
            UpdateTokens();
            UpdateOptions(pickupIndices);
        }

        public void UpdateOptions(List<PickupIndex> pickupIndices)
        {
            Transform itemRow = containerObject.transform.Find("ItemRow");
            if (!itemRow) return;

            // DestroyImmediate prevents 1-frame ghost children causing layout flicker
            for (int c = itemRow.childCount - 1; c >= 0; c--)
                DestroyImmediate(itemRow.GetChild(c).gameObject);
            buttonObjects.Clear();

            for (int i = 0; i < pickupIndices.Count; i++)
            {
                int slotIndex = i;
                var pickupIndex = pickupIndices[i];
                var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                var itemDef = ItemCatalog.GetItemDef(pickupDef?.itemIndex ?? ItemIndex.None);
                Color tierColor = GetTierColor(itemDef);

                // ── Card – VLG directly on card, no CardInner indirection ──────
                var card = new GameObject($"Card_{i}");
                card.transform.SetParent(itemRow, false);

                var cardImg = card.AddComponent<Image>();
                if (panelSprite) { cardImg.sprite = panelSprite; cardImg.type = Image.Type.Sliced; }
                cardImg.color = new Color(0.08f, 0.08f, 0.12f, 0.96f);

                // Card-level button – clicking anywhere on the card (icon, spacers, name, desc)
                // fires OnItemClicked. The BANISH/REROLL child buttons handle their own clicks.
                var cardBtn = card.AddComponent<Button>();
                cardBtn.targetGraphic = cardImg;
                var cardBtnColors = cardBtn.colors;
                cardBtnColors.normalColor = Color.white;
                cardBtnColors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
                cardBtnColors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
                cardBtnColors.fadeDuration = 0.1f;
                cardBtn.colors = cardBtnColors;
                cardBtn.onClick.AddListener(() => OnItemClicked(pickupIndex));

                var cardLe = card.AddComponent<LayoutElement>();
                cardLe.preferredWidth = 0;
                cardLe.flexibleWidth = 1;

                var vLayout = card.AddComponent<VerticalLayoutGroup>();
                vLayout.childControlHeight = false;
                vLayout.childControlWidth = true;
                vLayout.childForceExpandHeight = false;
                vLayout.childForceExpandWidth = true;
                vLayout.spacing = 6;
                vLayout.padding = new RectOffset(8, 8, 10, 56);
                card.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // Tier-colored top border (absolute overlay, excluded from VLG flow)
                var border = new GameObject("TierBorder");
                border.transform.SetParent(card.transform, false);
                var borderRect = border.AddComponent<RectTransform>();
                borderRect.anchorMin = new Vector2(0f, 1f);
                borderRect.anchorMax = Vector2.one;
                borderRect.offsetMin = new Vector2(0f, -4f);
                borderRect.offsetMax = Vector2.zero;
                border.AddComponent<LayoutElement>().ignoreLayout = true;
                border.AddComponent<Image>().color = tierColor;

                // ── Icon ─────────────────────────────────────────────────────────
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(card.transform, false);
                var imgLe = iconGo.AddComponent<LayoutElement>();
                imgLe.minHeight = 96;
                imgLe.preferredHeight = 96;
                imgLe.flexibleHeight = 0;

                // Tier-tinted background behind icon
                var iconBg = iconGo.AddComponent<Image>();
                iconBg.color = new Color(tierColor.r, tierColor.g, tierColor.b, 0.18f);

                var iconGo2 = new GameObject("IconSprite");
                iconGo2.transform.SetParent(iconGo.transform, false);
                var iconRect = iconGo2.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.1f, 0.1f);
                iconRect.anchorMax = new Vector2(0.9f, 0.9f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
                var iconImg = iconGo2.AddComponent<Image>();
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
                if (pickupDef?.iconSprite != null) iconImg.sprite = pickupDef.iconSprite;

                // ── Name ─────────────────────────────────────────────────────────
                var nameSpace = new GameObject("NameSpacer");
                nameSpace.transform.SetParent(card.transform, false);
                var nameSpaceLe = nameSpace.AddComponent<LayoutElement>();
                nameSpaceLe.minHeight = 2;
                nameSpaceLe.preferredHeight = 2;
                nameSpaceLe.flexibleHeight = 0;

                string displayName = itemDef != null ? Language.GetString(itemDef.nameToken) : "Unknown";
                var nameTmp = MakeLabel(card.transform, displayName, 24f, tierColor,
                    TextAlignmentOptions.Center, true);
                nameTmp.fontStyle = FontStyles.Bold;
                nameTmp.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // ── Description ──────────────────────────────────────────────────
                string desc = itemDef != null ? Language.GetString(itemDef.pickupToken) : "";
                var descTmp = MakeLabel(card.transform, desc, 18f,
                    new Color(0.72f, 0.72f, 0.78f), TextAlignmentOptions.Center, true);
                descTmp.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // 4px extra gap above buttons
                var btnSpace = new GameObject("BtnSpacer");
                btnSpace.transform.SetParent(card.transform, false);
                var btnSpaceLe = btnSpace.AddComponent<LayoutElement>();
                btnSpaceLe.minHeight = 2;
                btnSpaceLe.preferredHeight = 2;
                btnSpaceLe.flexibleHeight = 0;

                // ── BANISH / REROLL buttons ───────────────────────────────────
                BuildAbsoluteButton(card.transform, "BANISH",
                    new Vector2(0f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(8f, 8f), new Vector2(-4f, 48f),
                    new Color(0.50f, 0.07f, 0.07f, 0.95f),
                    new Color(0.90f, 0.22f, 0.22f, 1f),
                    () => OnBanishClicked(slotIndex));

                BuildAbsoluteButton(card.transform, "REROLL",
                    new Vector2(0.5f, 0f), new Vector2(1f, 0f),
                    new Vector2( 4f,  8f), new Vector2(-8f, 48f),
                    new Color(0.06f, 0.16f, 0.42f, 0.95f),
                    new Color(0.28f, 0.54f, 1.00f, 1f),
                    () => OnRerollClicked(slotIndex));

                buttonObjects.Add(card);
            }

            // Rebuild bottom-up: cards → itemRow → container.
            // ContentSizeFitter chains must be resolved from leaf to root, otherwise
            // a parent reads a stale preferred height from a child that hasn't recalculated yet.
            Transform row = containerObject.transform.Find("ItemRow");
            if (row)
            {
                foreach (Transform child in row)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(child.GetComponent<RectTransform>());
                LayoutRebuilder.ForceRebuildLayoutImmediate(row.GetComponent<RectTransform>());
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerObject.GetComponent<RectTransform>());
        }
        
        private void BuildAbsoluteButton(Transform parent, string label,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax,
            Color bgColor, Color accentColor,
            UnityEngine.Events.UnityAction callback)
        {
            var go = new GameObject(label + "Btn");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().ignoreLayout = true;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            // Flat background – no sprite so Button tinting is clean
            var bg = go.AddComponent<Image>();
            bg.color = bgColor;

            // 4px accent bar pinned to the top edge
            var accentGo = new GameObject("Accent");
            accentGo.transform.SetParent(go.transform, false);
            accentGo.AddComponent<LayoutElement>().ignoreLayout = true;
            var accentRt = accentGo.GetComponent<RectTransform>();
            accentRt.anchorMin = new Vector2(0f, 1f);
            accentRt.anchorMax = new Vector2(1f, 1f);
            accentRt.offsetMin = new Vector2(0f, -4f);
            accentRt.offsetMax = new Vector2(0f,  0f);
            accentGo.AddComponent<Image>().color = accentColor;

            // Button dims bg on hover by multiplying its color
            var btn = go.AddComponent<Button>();
            var cols = btn.colors;
            cols.normalColor      = Color.white;
            cols.highlightedColor = new Color(0.70f, 0.70f, 0.70f, 1f);
            cols.pressedColor     = new Color(0.50f, 0.50f, 0.50f, 1f);
            cols.selectedColor    = Color.white;
            cols.fadeDuration     = 0.12f;
            btn.colors = cols;
            btn.targetGraphic = bg;
            btn.onClick.AddListener(callback);

            // Label
            var txt = MakeLabel(go.transform, label, 18f, Color.white);
            txt.fontStyle = FontStyles.Bold;
            var r = txt.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(0f,  4f);
            r.offsetMax = new Vector2(0f, -4f);
        }

        public void Hide()
        {
            if (!IsVisible) return;
            IsVisible = false;
            StartCoroutine(HideNextFrame());
        }

        private IEnumerator HideNextFrame()
        {
            // Defer one frame so any active button-click event finishes processing
            // before the CursorOpener and MPEventSystem components are disabled.
            // This prevents the camera / input system from getting stuck.
            yield return null;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
            containerObject.SetActive(false);
        }

        public void ShowNotification()
        {
            notificationPanel.SetActive(true);
            notificationText.text = "LEVEL UP!\nPress F3";
            StartCoroutine(FlashNotification());
        }

        private System.Collections.IEnumerator FlashNotification()
        {
            float duration = 3f;
            float elapsed = 0f;
            var bg = notificationPanel.GetComponent<Image>();
            Color baseColor = bg.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.PingPong(elapsed * 2f, 0.4f) + 0.48f;
                bg.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }
            bg.color = baseColor;

            if (LevelUpManager.Instance.AvailableTokens <= 0)
                notificationPanel.SetActive(false);
        }

        private void Update()
        {
            bool showNotif = LevelUpManager.Instance.AvailableTokens > 0 && !IsVisible && !isPaused;
            if (notificationPanel.activeSelf != showNotif)
                notificationPanel.SetActive(showNotif);
        }

        // ─── Event handlers ───────────────────────────────────────────────────────

        private void OnItemClicked(PickupIndex pickupIndex)
        {
            if (LevelUpManager.Instance.SpendTokenLocal())
                new Networking.SendItemSelection(pickupIndex, RoR2.NetworkUser.readOnlyLocalPlayersList[0].netId)
                    .Send(NetworkDestination.Server);
        }

        private void OnBanishClicked(int slotIndex)
        {
            if (LevelUpManager.Instance.BanishTokens > 0)
                new Networking.SendBanish(slotIndex, RoR2.NetworkUser.readOnlyLocalPlayersList[0].netId)
                    .Send(NetworkDestination.Server);
        }

        private void OnRerollClicked(int slotIndex)
        {
            if (LevelUpManager.Instance.RerollTokens > 0)
                new Networking.SendReroll(slotIndex, RoR2.NetworkUser.readOnlyLocalPlayersList[0].netId)
                    .Send(NetworkDestination.Server);
        }
    }
}
