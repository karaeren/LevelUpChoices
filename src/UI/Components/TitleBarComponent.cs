using LevelUpChoices.UI.Builders;
using LevelUpChoices.UI.Constants;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LevelUpChoices.UI.Components {
    public static class TitleBarComponent {
        public static GameObject Create(Transform parent) {
            var container = new GameObject("TitleBarContainer");
            container.transform.SetParent(parent, false);

            UIElementBuilder.AddPanel(container, UIColors.TitleBarBg, true, null);
            UIElementBuilder.AddVerticalLayout(container, new RectOffset(0, 0, 0, 0), 0f, false, true, false, true);

            LayoutElement le = container.AddComponent<LayoutElement>();
            le.preferredHeight = 52;

            // Inner title bar
            var inner = new GameObject("TitleBar");
            inner.transform.SetParent(container.transform, false);
            Image innerBg = inner.AddComponent<Image>();
            innerBg.color = UIColors.TitleBarInner;

            LayoutElement innerLe = inner.AddComponent<LayoutElement>();
            innerLe.preferredHeight = 52;

            // Gold line
            GameObject goldLine = UIElementBuilder.MakeUIObject("GoldLine", inner.transform,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            RectTransform goldLineRect = goldLine.GetComponent<RectTransform>();
            goldLineRect.offsetMax = new Vector2(0f, 2f);
            Image goldLineImg = goldLine.AddComponent<Image>();
            goldLineImg.color = UIColors.GoldAccent;

            // Title label
            HGTextMeshProUGUI titleTmp = UIElementBuilder.MakeLabel(inner.transform, "CHOOSE AN ITEM",
                28f, UIColors.GoldAccent, TextAlignmentOptions.Center, false);
            titleTmp.fontStyle = FontStyles.Bold;
            RectTransform titleRect = titleTmp.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(20f, 0f);
            titleRect.offsetMax = new Vector2(-20f, -2f);

            return container;
        }
    }
}
