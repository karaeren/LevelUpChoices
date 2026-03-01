using UnityEngine;

namespace LevelUpChoices.UI.Components
{
    public class BackdropPanelComponent : MonoBehaviour
    {
        private GameObject _backdrop;

        public void Initialize(GameObject backdrop)
        {
            _backdrop = backdrop;
        }

        public void Show()
        {
            _backdrop?.SetActive(true);
        }

        public void Hide()
        {
            _backdrop?.SetActive(false);
        }
    }
}
