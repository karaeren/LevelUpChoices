using System;
using LevelUpChoices.Extensions;
using LevelUpChoices.UI.Constants;
using LevelUpChoices.UI.Services;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LevelUpChoices.UI.Builders {
    public class CardBuilder(UIAssetService assetService) {
        private readonly UIAssetService _assetService = assetService;
        private readonly ButtonBuilder _buttonBuilder = new(assetService);
        private readonly UIElementBuilder _elementBuilder = new(assetService);

        public GameObject CreateCard(
            Transform parent,
            PickupIndex pickupIndex,
            int slotIndex,
            ItemIndex synergy,
            Action<PickupIndex> onItemClicked,
            Action<int> onBanishClicked,
            Action<int> onRerollClicked) {
            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            ItemDef itemDef = ItemCatalog.GetItemDef(pickupDef?.itemIndex ?? ItemIndex.None);

            Color tierColor = Color.white;

            if (itemDef != null) {
                ItemTierDef tierDef = ItemTierCatalog.GetItemTierDef(itemDef.tier);
                if (tierDef != null)
                    tierColor = ColorCatalog.GetColor(tierDef.colorIndex);
            }

            var card = new GameObject($"Card_{slotIndex}");
            card.transform.SetParent(parent, false);

            Image cardImg = card.AddComponent<Image>();
            if (_assetService.PanelSprite != null) {
                cardImg.sprite = _assetService.PanelSprite;
                cardImg.type = Image.Type.Sliced;
            }
            cardImg.color = UIColors.CardBg;

            Button cardBtn = card.AddComponent<Button>();
            cardBtn.targetGraphic = cardImg;
            ColorBlock cardBtnColors = cardBtn.colors;
            cardBtnColors.normalColor = UIColors.CardButtonNormal;
            cardBtnColors.highlightedColor = UIColors.CardButtonHighlighted;
            cardBtnColors.pressedColor = UIColors.CardButtonPressed;
            cardBtnColors.fadeDuration = 0.08f;
            cardBtn.colors = cardBtnColors;
            cardBtn.onClick.AddListener(() => onItemClicked?.Invoke(pickupIndex));

            EventTrigger hoverTrigger = card.AddComponent<EventTrigger>();
            var hoverEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            hoverEntry.callback.AddListener(_ => UISoundManager.PlayHover());
            hoverTrigger.triggers.Add(hoverEntry);

            LayoutElement cardLe = card.AddComponent<LayoutElement>();
            cardLe.preferredWidth = 0;
            cardLe.flexibleWidth = 1;

            UIElementBuilder.AddVerticalLayout(card, new RectOffset(6, 6, 8, 52), 4, false, true, false, true);

            // Icon background
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(card.transform, false);
            LayoutElement imgLe = iconGo.AddComponent<LayoutElement>();
            imgLe.minHeight = 90;
            imgLe.preferredHeight = 90;
            imgLe.flexibleHeight = 0;

            Image iconBg = iconGo.AddComponent<Image>();
            iconBg.color = UIColors.GetIconBackgroundColor(tierColor);

            // Icon sprite
            GameObject iconGo2 = UIElementBuilder.MakeUIObject("IconSprite", iconGo.transform,
                new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.85f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            RectTransform iconRect = iconGo2.GetComponent<RectTransform>();
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            Image iconImg = iconGo2.AddComponent<Image>();
            iconImg.color = Color.white;
            iconImg.preserveAspect = true;
            if (pickupDef?.iconSprite != null)
                iconImg.sprite = pickupDef.iconSprite;

            // Name spacer
            UIElementBuilder.MakeSpacer(card.transform, "NameSpacer", 2f, 2f, 0f);

            // Owned count lookup
            CharacterMaster localMaster = LocalUserManager.GetFirstLocalUser()?.cachedMaster;
            int ownedCount = localMaster?.inventory?.GetItemCountEffective(itemDef?.itemIndex ?? ItemIndex.None) ?? 0;

            // Name label
            string displayName = itemDef != null ? Language.GetString(itemDef.nameToken) : "Unknown";
            displayName += ownedCount > 0 ? $"  <style=cIsHealing>({ownedCount})</style>" : "";

            if (synergy != ItemIndex.None) {
                ItemDef synergyDef = ItemCatalog.GetItemDef(synergy);
                if (synergyDef != null) {
                    string synergyName = Language.GetString(synergyDef.nameToken);
                    string synergyColor = ColorUtility.ToHtmlStringRGB(tierColor);
                    displayName += $"\n<size=14><color=#FFFFFF>Synergizes with <color=#{synergyColor}>{synergyName}</color></color></size>";
                }
            }

            var nameLabelGo = new GameObject("NameLabel");
            nameLabelGo.transform.SetParent(card.transform, false);
            HGTextMeshProUGUI nameTmp = nameLabelGo.AddComponent<HGTextMeshProUGUI>();
            nameTmp.text = displayName;
            nameTmp.fontSize = 22f;
            nameTmp.color = tierColor;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.enableWordWrapping = true;
            nameLabelGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Description
            if (ModConfig.ShowItemDescriptions.Value && itemDef != null) {
                string lgDesc = Integrations.LookingGlassEnabled
                    ? LookingGlassIntegration.GetItemDescription(itemDef, ownedCount, localMaster, true)
                    : null;
                string desc = "\n" + (lgDesc ?? Language.GetString(itemDef.pickupToken));
                var descLabelGo = new GameObject("Description");
                descLabelGo.transform.SetParent(card.transform, false);
                HGTextMeshProUGUI descTmp = descLabelGo.AddComponent<HGTextMeshProUGUI>();
                descTmp.text = desc;
                descTmp.fontSize = 16f;
                descTmp.color = UIColors.DescriptionText;
                descTmp.alignment = TextAlignmentOptions.Center;
                descTmp.enableWordWrapping = true;
                descLabelGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // Flex spacer
            UIElementBuilder.MakeSpacer(card.transform, "FlexSpacer", 1f, 1f, 1f);

            // BANISH button
            ButtonBuilder.CreateAbsoluteButton(
                card.transform,
                "BANISH",
                new Vector2(0f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(6f, 6f),
                new Vector2(-3f, 44f),
                UIColors.BanishBg,
                UIColors.BanishAccent,
                () => onBanishClicked?.Invoke(slotIndex));

            // REROLL button
            ButtonBuilder.CreateAbsoluteButton(
                card.transform,
                "REROLL",
                new Vector2(0.5f, 0f),
                new Vector2(1f, 0f),
                new Vector2(3f, 6f),
                new Vector2(-6f, 44f),
                UIColors.RerollBg,
                UIColors.RerollAccent,
                () => onRerollClicked?.Invoke(slotIndex));

            return card;
        }
    }
}
