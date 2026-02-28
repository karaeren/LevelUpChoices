using System.Collections.Generic;
using LevelUpChoices.UI.Builders;
using UnityEngine;

namespace LevelUpChoices.UI.Components
{
    public class ItemRowComponent : MonoBehaviour
    {
        private readonly List<GameObject> _cards = [];

        public void Initialize(UIElementBuilder elementBuilder)
        {
            elementBuilder.AddHorizontalLayout(gameObject, new RectOffset(16, 16, 12, 16), 12, true, true, true, true);
            gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        }

        public void ClearCards()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
            _cards.Clear();
        }

        public void AddCard(GameObject card)
        {
            _cards.Add(card);
        }

        public void RebuildLayout()
        {
            foreach (Transform child in transform)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(child.GetComponent<RectTransform>());
            }
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }
    }
}
