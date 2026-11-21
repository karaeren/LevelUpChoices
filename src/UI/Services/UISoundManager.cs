using RoR2;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LevelUpChoices.UI.Services
{
    public static class UISoundManager
    {
        public const string HoverSoundName = "Play_UI_menuHover";
        public const string ClickSoundName = "Play_UI_menuClick";

        internal static void AddHoverSound(GameObject gameObject)
        {
            var trigger = gameObject.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener(_ => PlayHover());
            trigger.triggers.Add(entry);
        }

        public static void PlayHover()
        {
            Util.PlaySound(HoverSoundName, RoR2Application.instance.gameObject);
        }

        public static void PlayClick()
        {
            Util.PlaySound(ClickSoundName, RoR2Application.instance.gameObject);
        }
    }
}
