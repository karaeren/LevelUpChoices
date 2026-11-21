using System;
using LevelUpChoices.Extensions;
using LevelUpChoices.UI.Constants;
using LevelUpChoices.UI.Services;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LevelUpChoices.UI.Builders
{
    public class CardBuilder(UIAssetService assetService)
    {
        private readonly UIAssetService _assetService = assetService;
        private readonly ButtonBuilder _buttonBuilder = new();
        private readonly UIElementBuilder _elementBuilder = new();

        public GameObject CreateCard(
            Transform parent,
            PickupIndex pickupIndex,
            int slotIndex,
            ItemIndex synergy,
            Action<PickupIndex> onItemClicked,
            Action<int> onBanishClicked,
            Action<int> onRerollClicked)
        {
            var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            var itemDef = ItemCatalog.GetItemDef(pickupDef?.itemIndex ?? ItemIndex.None);
            var equipmentDef = EquipmentCatalog.GetEquipmentDef(pickupDef?.equipmentIndex ?? EquipmentIndex.None);

            Color tierColor = Color.white;

            if (itemDef != null)
            {
                var tierDef = ItemTierCatalog.GetItemTierDef(itemDef.tier);
                if (tierDef != null)
                    tierColor = ColorCatalog.GetColor(tierDef.colorIndex);
            }
            else if (equipmentDef != null)
            {
                tierColor = ColorCatalog.GetColor(equipmentDef.colorIndex);
            }

            var card = new GameObject($"Card_{slotIndex}");
            card.transform.SetParent(parent, false);

            var cardImg = _elementBuilder.AddPanel(card, UIColors.CardBg, _assetService.PanelSprite);

            var cardBtn = card.AddComponent<Button>();
            cardBtn.targetGraphic = cardImg;
            var cardBtnColors = cardBtn.colors;
            cardBtnColors.normalColor = UIColors.CardButtonNormal;
            cardBtnColors.highlightedColor = UIColors.CardButtonHighlighted;
            cardBtnColors.pressedColor = UIColors.CardButtonPressed;
            cardBtnColors.fadeDuration = 0.08f;
            cardBtn.colors = cardBtnColors;
            cardBtn.onClick.AddListener(() => onItemClicked?.Invoke(pickupIndex));

            UISoundManager.AddHoverSound(card);

            var cardLe = card.AddComponent<LayoutElement>();
            cardLe.preferredWidth = 0;
            cardLe.flexibleWidth = 1;

            _elementBuilder.AddVerticalLayout(card, new RectOffset(6, 6, 8, 52), 4, false, true, false, true);

            // Icon background
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(card.transform, false);
            var imgLe = iconGo.AddComponent<LayoutElement>();
            imgLe.minHeight = 90;
            imgLe.preferredHeight = 90;
            imgLe.flexibleHeight = 0;

            var iconBg = iconGo.AddComponent<Image>();
            iconBg.color = UIColors.GetIconBackgroundColor(tierColor);

            // Icon sprite
            var iconSprite = _elementBuilder.MakeUIObject("IconSprite", iconGo.transform,
                new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.85f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var iconImg = iconSprite.AddComponent<Image>();
            iconImg.color = Color.white;
            iconImg.preserveAspect = true;
            iconImg.enabled = false; // An Image with no sprite draws a white rectangle.
            if (pickupDef != null)
            {
                var asyncIcon = iconSprite.AddComponent<AsyncImage>();
                asyncIcon.renderer = iconImg;
                asyncIcon.disableWhileLoading = true;
                asyncIcon.SetAssetOrDirectReference(pickupDef.iconSpriteReference, pickupDef.iconSprite);
            }

            // Name spacer
            _elementBuilder.MakeSpacer(card.transform, "NameSpacer", 2f, 2f, 0f);

            // Owned count lookup
            var localMaster = LocalUserManager.GetFirstLocalUser()?.cachedMaster;
            int ownedCount = itemDef != null
                ? localMaster?.inventory?.GetItemCountEffective(itemDef.itemIndex) ?? 0
                : 0;

            // Name label
            string displayName = pickupDef != null ? Language.GetString(pickupDef.nameToken) : "Unknown";
            displayName += ownedCount > 0 ? $"  <style=cIsHealing>({ownedCount})</style>" : "";
            if (equipmentDef != null && (localMaster?.inventory?.HasEquipment(equipmentDef.equipmentIndex) ?? false))
                displayName += "  <style=cIsHealing>(Owned)</style>";

            if (synergy != ItemIndex.None)
            {
                var synergyDef = ItemCatalog.GetItemDef(synergy);
                if (synergyDef != null)
                {
                    string synergyName = Language.GetString(synergyDef.nameToken);
                    string synergyColor = ColorUtility.ToHtmlStringRGB(tierColor);
                    displayName += $"\n<size=14><color=#FFFFFF>Synergizes with <color=#{synergyColor}>{synergyName}</color></color></size>";
                }
            }

            var nameLabelGo = new GameObject("NameLabel");
            nameLabelGo.transform.SetParent(card.transform, false);
            var nameTmp = nameLabelGo.AddComponent<HGTextMeshProUGUI>();
            nameTmp.text = displayName;
            nameTmp.fontSize = 22f;
            nameTmp.color = tierColor;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.enableWordWrapping = true;
            nameLabelGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Description
            if (ModConfig.ShowItemDescriptions.Value && (itemDef != null || equipmentDef != null))
            {
                string description;
                if (itemDef != null)
                {
                    description = Integrations.lookingGlassEnabled
                        ? LookingGlassIntegration.GetItemDescription(itemDef, ownedCount, localMaster, true)
                        : null;
                    description ??= Language.GetString(itemDef.pickupToken);
                }
                else
                {
                    description = Integrations.lookingGlassEnabled
                        ? LookingGlassIntegration.GetEquipmentDescription(equipmentDef, localMaster)
                        : Language.GetString(equipmentDef.descriptionToken);
                }
                string desc = "\n" + description;
                var descLabelGo = new GameObject("Description");
                descLabelGo.transform.SetParent(card.transform, false);
                var descTmp = descLabelGo.AddComponent<HGTextMeshProUGUI>();
                descTmp.text = desc;
                descTmp.fontSize = 16f;
                descTmp.color = UIColors.DescriptionText;
                descTmp.alignment = TextAlignmentOptions.Center;
                descTmp.enableWordWrapping = true;
                descLabelGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // Flex spacer
            _elementBuilder.MakeSpacer(card.transform, "FlexSpacer", 1f, 1f, 1f);

            // BANISH button
            _buttonBuilder.CreateAbsoluteButton(
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
            _buttonBuilder.CreateAbsoluteButton(
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
