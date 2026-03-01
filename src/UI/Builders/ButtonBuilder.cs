using LevelUpChoices.UI.Constants;
using LevelUpChoices.UI.Services;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LevelUpChoices.UI.Builders
{
    public class ButtonBuilder(UIAssetService assetService)
    {
        private readonly UIAssetService _assetService = assetService;

        public static GameObject CreateAbsoluteButton(
            Transform parent,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color bgColor,
            Color accentColor,
            UnityEngine.Events.UnityAction callback)
        {
            var go = new GameObject(label + "Btn");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().ignoreLayout = true;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            Image bg = go.AddComponent<Image>();
            bg.color = bgColor;

            var accentGo = new GameObject("Accent");
            accentGo.transform.SetParent(go.transform, false);
            accentGo.AddComponent<LayoutElement>().ignoreLayout = true;
            RectTransform accentRt = accentGo.GetComponent<RectTransform>();
            accentRt.anchorMin = new Vector2(0f, 1f);
            accentRt.anchorMax = new Vector2(1f, 1f);
            accentRt.offsetMin = new Vector2(0f, -3f);
            accentRt.offsetMax = new Vector2(0f, 0f);
            accentGo.AddComponent<Image>().color = accentColor;

            Button btn = go.AddComponent<Button>();
            ColorBlock cols = btn.colors;
            cols.normalColor = UIColors.AbsoluteButtonNormal;
            cols.highlightedColor = UIColors.AbsoluteButtonHighlighted;
            cols.pressedColor = UIColors.AbsoluteButtonPressed;
            cols.selectedColor = UIColors.AbsoluteButtonSelected;
            cols.fadeDuration = 0.1f;
            btn.colors = cols;
            btn.targetGraphic = bg;
            btn.onClick.AddListener(callback);

            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(go.transform, false);
            HGTextMeshProUGUI tmp = txtGo.AddComponent<HGTextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 16f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            RectTransform r = txtGo.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(0f, 2f);
            r.offsetMax = new Vector2(0f, -2f);

            EventTrigger trigger = go.AddComponent<EventTrigger>();
            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => UISoundManager.PlayHover());
            trigger.triggers.Add(enterEntry);

            return go;
        }
    }
}
