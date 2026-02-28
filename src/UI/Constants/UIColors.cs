using UnityEngine;

namespace LevelUpChoices.UI.Constants
{
    public static class UIColors
    {

        public static readonly Color GoldAccent = new(1f, 0.84f, 0.22f);
        public static readonly Color DarkPanelBg = new(0.06f, 0.06f, 0.09f, 0.95f);
        public static readonly Color CardBg = new(0.05f, 0.05f, 0.07f, 0.95f);
        public static readonly Color TitleBarBg = new(0.08f, 0.08f, 0.12f, 1f);
        public static readonly Color TitleBarInner = new(1f, 0.85f, 0.2f, 0.08f);
        public static readonly Color HeaderBg = new(0.05f, 0.05f, 0.08f, 0.8f);
        public static readonly Color HeaderText = new(0.78f, 0.85f, 1f);
        public static readonly Color Backdrop = new(0f, 0f, 0f, 0.6f);
        public static readonly Color NotificationBg = new(0.08f, 0.08f, 0.12f, 0.95f);
        public static readonly Color NotificationInner = new(1f, 0.85f, 0.2f, 0.15f);
        public static readonly Color CardButtonNormal = Color.white;
        public static readonly Color CardButtonHighlighted = new(1.3f, 1.3f, 1.3f, 1f);
        public static readonly Color CardButtonPressed = new(0.7f, 0.7f, 0.7f, 1f);
        public static readonly Color DescriptionText = new(0.7f, 0.7f, 0.75f);
        public static readonly Color BanishBg = new(0.55f, 0.08f, 0.08f, 0.92f);
        public static readonly Color BanishAccent = new(0.95f, 0.25f, 0.25f, 1f);
        public static readonly Color RerollBg = new(0.08f, 0.18f, 0.45f, 0.92f);
        public static readonly Color RerollAccent = new(0.3f, 0.55f, 1f, 1f);
        public static readonly Color AbsoluteButtonNormal = Color.white;
        public static readonly Color AbsoluteButtonHighlighted = new(0.8f, 0.8f, 0.8f, 1f);
        public static readonly Color AbsoluteButtonPressed = new(0.5f, 0.5f, 0.5f, 1f);
        public static readonly Color AbsoluteButtonSelected = Color.white;

        public static Color GetIconBackgroundColor(Color tierColor)
        {
            return new Color(tierColor.r * 0.5f, tierColor.g * 0.5f, tierColor.b * 0.5f, 0.4f);
        }
    }
}
