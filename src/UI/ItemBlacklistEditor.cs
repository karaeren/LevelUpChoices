using System;
using System.Collections.Generic;
using System.Linq;
using LevelUpChoices.UI.Builders;
using LevelUpChoices.UI.Constants;
using LevelUpChoices.UI.Services;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

namespace LevelUpChoices.UI
{
    internal sealed class ItemBlacklistEditor
    {
        private const int MaxSuggestions = 6;
        private const float RowHeight = 36f;
        private const float RowSpacing = 4f;
        private static SimpleDialogBox _dialog;

        private readonly UIElementBuilder _ui = new(UIAssetService.Instance);
        private readonly HashSet<string> _blocked = new(ModConfig.ParseItemBlacklist(ModConfig.ItemBlacklist.Value), StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _items = new(StringComparer.OrdinalIgnoreCase);
        private readonly TMP_InputField _search;
        private readonly Transform _suggestions;
        private readonly RectTransform _blockedRows;
        private readonly HGTextMeshProUGUI _blockedHeading;

        public static void Open()
        {
            if (_dialog)
                return;

            _dialog = SimpleDialogBox.Create(EventSystem.current as MPEventSystem);
            _dialog.headerLabel.text = "Item Blacklist";
            if (NetworkClient.active && !NetworkServer.active)
            {
                _dialog.descriptionLabel.text = "Only the host can edit the blacklist during multiplayer.";
                _dialog.AddCancelButton("CANCEL");
                return;
            }

            new ItemBlacklistEditor(_dialog);
        }

        private ItemBlacklistEditor(SimpleDialogBox dialog)
        {
            foreach (var item in ItemCatalog.allItemDefs)
            {
                if (!item || item.hidden || string.IsNullOrEmpty(item.name) || item.ContainsTag(ItemTag.IgnoreForDropList))
                    continue;

                string label = Language.GetString(item.nameToken);
                _items.TryAdd(item.name, string.IsNullOrEmpty(label) ? item.name : label);
            }

            // The native dialog resizes to fit its description, which holds the editor.
            var body = dialog.descriptionLabel.gameObject;
            var dialogRect = (RectTransform)dialog.transform;
            float width = dialogRect.rect.width + 60f;
            dialogRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            body.transform.parent.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredWidth = width;
            dialog.descriptionLabel.text = string.Empty;
            dialog.descriptionLabel.raycastTarget = false;
            _ui.AddVerticalLayout(body, new RectOffset(16, 16, 8, 8), spacing: 8f, childControlHeight: true);

            _search = CreateSearch(body.transform);
            var columns = CreateObject("Lists", body.transform);
            SetHeight(columns, 24f + MaxSuggestions * (RowHeight + RowSpacing));

            var available = CreateColumn("AvailableItems", columns.transform, left: true);
            AddLabel(available, "Suggestions", 17f);
            _suggestions = available;

            var blocked = CreateColumn("BlockedItems", columns.transform, left: false);
            _blockedHeading = AddLabel(blocked, "Blocked items", 17f);
            _blockedRows = CreateBlockedList(blocked);
            var notice = AddLabel(body.transform, "These will take effect on the next run.", 17f);
            notice.color = new Color(1f, 0.68f, 0.68f);

            dialog.AddActionButton(() =>
            {
                ModConfig.ItemBlacklist.Value = string.Join(", ", _blocked.OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
            }, "SAVE");
            dialog.AddCancelButton("CANCEL");

            _search.onValueChanged.AddListener(RefreshSuggestions);
            RefreshLists();
        }

        private TMP_InputField CreateSearch(Transform parent)
        {
            var field = CreateObject("ItemSearch", parent);
            SetHeight(field, 40f);
            field.AddComponent<MPEventSystemLocator>();
            var background = _ui.AddPanel(field, UIColors.TitleBarBg, false, null);

            var viewport = CreateObject("TextViewport", field.transform);
            Stretch((RectTransform)viewport.transform, new Vector2(10f, 3f), new Vector2(-10f, -3f));
            viewport.AddComponent<UnityEngine.UI.RectMask2D>();
            var text = AddLabel(viewport.transform, string.Empty, 20f);
            var placeholder = AddLabel(viewport.transform, "Search by item name…", 20f);
            placeholder.color = UIColors.DescriptionText;
            Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
            Stretch(placeholder.rectTransform, Vector2.zero, Vector2.zero);

            var input = field.AddComponent<TMP_InputField>();
            input.textViewport = (RectTransform)viewport.transform;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.targetGraphic = background;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.richText = false;
            return input;
        }

        private Transform CreateColumn(string name, Transform parent, bool left)
        {
            var column = CreateObject(name, parent);
            // Each column gets half the available width, regardless of its contents.
            var rect = (RectTransform)column.transform;
            rect.anchorMin = new Vector2(left ? 0f : 0.5f, 0f);
            rect.anchorMax = new Vector2(left ? 0.5f : 1f, 1f);
            rect.offsetMin = new Vector2(left ? 0f : 6f, 0f);
            rect.offsetMax = new Vector2(left ? -6f : 0f, 0f);
            _ui.AddVerticalLayout(column, new RectOffset(), spacing: RowSpacing, childControlHeight: true);
            return rect;
        }

        private RectTransform CreateBlockedList(Transform parent)
        {
            var list = CreateObject("BlockedList", parent);
            list.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1f;
            _ui.AddPanel(list, UIColors.HeaderBg, false, null);
            var viewport = CreateObject("Viewport", list.transform);
            var viewportRect = (RectTransform)viewport.transform;
            Stretch(viewportRect, Vector2.zero, Vector2.zero);
            viewport.AddComponent<UnityEngine.UI.RectMask2D>();

            var rows = CreateObject("Rows", viewport.transform);
            var rowsRect = (RectTransform)rows.transform;
            rowsRect.anchorMin = new Vector2(0f, 1f);
            rowsRect.anchorMax = Vector2.one;
            rowsRect.pivot = new Vector2(0.5f, 1f);
            rowsRect.sizeDelta = Vector2.zero;
            rowsRect.anchoredPosition = Vector2.zero;
            _ui.AddVerticalLayout(rows, new RectOffset(), spacing: RowSpacing, childControlHeight: true);
            rows.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

            var scroll = list.AddComponent<UnityEngine.UI.ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = rowsRect;
            scroll.horizontal = false;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = RowHeight;
            return rowsRect;
        }

        private void RefreshSuggestions(string query)
        {
            // The section heading is the first child and stays visible as results change.
            ClearRows(_suggestions, firstRow: 1);
            query = query.Trim();
            if (query.Length == 0)
            {
                AddLabel(_suggestions, "Type to find an item.", 17f);
                return;
            }

            var matches = _items
                .Where(item => !_blocked.Contains(item.Key) && !PlayerDropTable.BannedItemNames.Contains(item.Key))
                .Select(item => (Name: item.Key, Label: item.Value, Rank: MatchRank(item.Key, item.Value, query)))
                .Where(item => item.Rank < 3)
                .OrderBy(item => item.Rank)
                .ThenBy(item => item.Label, StringComparer.CurrentCultureIgnoreCase)
                .Take(MaxSuggestions)
                .ToList();

            if (matches.Count == 0)
                AddLabel(_suggestions, "No matching unblocked items.", 17f);

            foreach (var item in matches)
            {
                AddRow(_suggestions, "+", FormatName(item.Name), () =>
                {
                    _blocked.Add(item.Name);
                    RefreshLists();
                });
            }
        }

        private static int MatchRank(string name, string label, string query)
        {
            if (name.Equals(query, StringComparison.OrdinalIgnoreCase) || label.Equals(query, StringComparison.CurrentCultureIgnoreCase))
                return 0;
            if (name.StartsWith(query, StringComparison.OrdinalIgnoreCase) || label.StartsWith(query, StringComparison.CurrentCultureIgnoreCase))
                return 1;
            if (name.Contains(query, StringComparison.OrdinalIgnoreCase) || label.Contains(query, StringComparison.CurrentCultureIgnoreCase))
                return 2;
            return 3;
        }

        private void RefreshBlockedList()
        {
            ClearRows(_blockedRows, firstRow: 0);
            _blockedHeading.text = $"Blocked items ({_blocked.Count})";
            if (_blocked.Count == 0)
                AddLabel(_blockedRows, "No items blocked.", 17f);

            foreach (string name in _blocked.OrderBy(FormatName, StringComparer.CurrentCultureIgnoreCase))
            {
                AddRow(_blockedRows, "−", FormatName(name), () =>
                {
                    _blocked.Remove(name);
                    RefreshLists();
                });
            }
        }

        private void RefreshLists()
        {
            RefreshBlockedList();
            RefreshSuggestions(_search.text);
        }

        private string FormatName(string name)
        {
            return _items.TryGetValue(name, out string label) && label != name ? $"{label} ({name})" : name;
        }

        private HGTextMeshProUGUI AddLabel(Transform parent, string text, float fontSize)
        {
            var label = _ui.MakeLabel(parent, text, fontSize, Color.white, TextAlignmentOptions.MidlineLeft, false);
            label.raycastTarget = false;
            label.richText = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        private void AddRow(Transform parent, string action, string text, UnityAction click)
        {
            var row = CreateObject("ItemRow", parent);
            SetHeight(row, RowHeight);
            var button = row.AddComponent<MPButton>();
            button.targetGraphic = _ui.AddPanel(row, Color.white, false, null);
            var colors = button.colors;
            colors.normalColor = new Color(0.16f, 0.18f, 0.24f);
            colors.highlightedColor = colors.selectedColor = new Color(0.32f, 0.37f, 0.46f);
            colors.pressedColor = new Color(0.23f, 0.26f, 0.34f);
            button.colors = colors;
            button.onClick.AddListener(click);
            var label = AddLabel(row.transform, $"{action}  {text}", 18f);
            Stretch(label.rectTransform, new Vector2(8f, 0f), new Vector2(-8f, 0f));
        }

        private static void ClearRows(Transform parent, int firstRow)
        {
            for (int i = parent.childCount - 1; i >= firstRow; i--)
            {
                var row = parent.GetChild(i).gameObject;
                row.SetActive(false);
                UnityEngine.Object.Destroy(row);
            }
        }

        private static GameObject CreateObject(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static void SetHeight(GameObject obj, float height)
        {
            obj.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = height;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
