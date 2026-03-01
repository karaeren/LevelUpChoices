using LevelUpChoices.UI.Services;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LevelUpChoices.UI.Builders
{
    public class UIElementBuilder(UIAssetService assetService)
    {
        private readonly UIAssetService _assetService = assetService;

        public static HGTextMeshProUGUI MakeLabel(
            Transform parent,
            string text,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment,
            bool wordWrap)
        {
            var gameObject = new GameObject("Label", typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);

            HGTextMeshProUGUI textComponent = gameObject.AddComponent<HGTextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.color = color;
            textComponent.alignment = alignment;
            textComponent.enableWordWrapping = wordWrap;

            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            return textComponent;
        }

        public static Image AddPanel(GameObject gameObject, Color color, bool sliced, Sprite sprite)
        {
            Image image = gameObject.AddComponent<Image>();
            image.color = color;
            image.type = (sliced && sprite != null) ? Image.Type.Sliced : Image.Type.Simple;
            if (sprite != null)
                image.sprite = sprite;

            return image;
        }

        public static void AddButtonSound(Button btn)
        {
            EventTrigger eventTrigger = btn.gameObject.AddComponent<EventTrigger>();

            var hoverEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter
            };
            hoverEntry.callback.AddListener(_ => UISoundManager.PlayHover());

            var clickEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerClick
            };
            clickEntry.callback.AddListener(_ => UISoundManager.PlayClick());

        }

        public static LayoutElement MakeSpacer(Transform parent, string name, float minHeight, float preferredHeight, float flexibleHeight)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minHeight = minHeight;
            le.preferredHeight = preferredHeight;
            le.flexibleHeight = flexibleHeight;
            return le;
        }

        public static GameObject MakeUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
            return go;
        }

        public static VerticalLayoutGroup AddVerticalLayout(GameObject go, RectOffset padding, float spacing = 0f,
            bool childControlHeight = false, bool childControlWidth = true,
            bool childForceExpandHeight = false, bool childForceExpandWidth = true)
        {
            VerticalLayoutGroup layout = go.AddComponent<VerticalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childControlHeight = childControlHeight;
            layout.childControlWidth = childControlWidth;
            layout.childForceExpandHeight = childForceExpandHeight;
            layout.childForceExpandWidth = childForceExpandWidth;
            return layout;
        }

        public static HorizontalLayoutGroup AddHorizontalLayout(GameObject go, RectOffset padding, float spacing = 0f,
            bool childControlHeight = true, bool childControlWidth = true,
            bool childForceExpandHeight = true, bool childForceExpandWidth = true)
        {
            HorizontalLayoutGroup layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childControlHeight = childControlHeight;
            layout.childControlWidth = childControlWidth;
            layout.childForceExpandHeight = childForceExpandHeight;
            layout.childForceExpandWidth = childForceExpandWidth;
            return layout;
        }
    }
}
