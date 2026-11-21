using LevelUpChoices.UI.Constants;
using LevelUpChoices.UI.Services;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LevelUpChoices.UI.Builders
{
    public class ButtonBuilder
    {
        public GameObject CreateAbsoluteButton(
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

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var bg = go.AddComponent<Image>();
            bg.color = bgColor;

            var accentGo = new GameObject("Accent");
            accentGo.transform.SetParent(go.transform, false);
            accentGo.AddComponent<LayoutElement>().ignoreLayout = true;
            var accentRt = accentGo.GetComponent<RectTransform>();
            accentRt.anchorMin = new Vector2(0f, 1f);
            accentRt.anchorMax = new Vector2(1f, 1f);
            accentRt.offsetMin = new Vector2(0f, -3f);
            accentRt.offsetMax = new Vector2(0f, 0f);
            accentGo.AddComponent<Image>().color = accentColor;

            var btn = go.AddComponent<Button>();
            var cols = btn.colors;
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
            var tmp = txtGo.AddComponent<HGTextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 16f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            var r = txtGo.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(0f, 2f);
            r.offsetMax = new Vector2(0f, -2f);

            UISoundManager.AddHoverSound(go);

            return go;
        }
    }
}
