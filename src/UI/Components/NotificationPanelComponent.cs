using LevelUpChoices.UI.Constants;
using RoR2.UI;
using TMPro;
using UnityEngine;

namespace LevelUpChoices.UI.Components
{
    public class NotificationPanelComponent : MonoBehaviour
    {
        private HGTextMeshProUGUI _label;
        private GameObject _panel;

        public void Initialize(GameObject panel, HGTextMeshProUGUI label)
        {
            _panel = panel;
            _label = label;
        }

        public void Show(int unusedTokens, string keyName)
        {
            if (_panel == null)
                return;

            string tokenWord = unusedTokens == 1 ? "TOKEN" : "TOKENS";
            string newText = $"{unusedTokens} UNUSED {tokenWord}\nPRESS {keyName}";

            if (_label != null && _label.text != newText)
            {
                _label.text = newText;
            }

            if (!_panel.activeSelf)
            {
                _panel.SetActive(true);
            }
        }

        public void Hide()
        {
            if (_panel != null && _panel.activeSelf)
            {
                _panel.SetActive(false);
            }
        }

        public bool IsVisible => _panel != null && _panel.activeSelf;
    }
}
