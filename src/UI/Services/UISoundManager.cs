using RoR2;

namespace LevelUpChoices.UI.Services {
    public static class UISoundManager {
        public const string HoverSoundName = "Play_UI_menuHover";
        public const string ClickSoundName = "Play_UI_menuClick";

        public static void PlayHover() {
            Util.PlaySound(HoverSoundName, RoR2Application.instance.gameObject);
        }

        public static void PlayClick() {
            Util.PlaySound(ClickSoundName, RoR2Application.instance.gameObject);
        }
    }
}
