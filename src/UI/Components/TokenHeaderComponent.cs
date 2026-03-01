using RoR2.UI;
using UnityEngine;

namespace LevelUpChoices.UI.Components {
    public class TokenHeaderComponent : MonoBehaviour {
        private HGTextMeshProUGUI _selectLabel;
        private HGTextMeshProUGUI _banishLabel;
        private HGTextMeshProUGUI _rerollLabel;

        public void Initialize(HGTextMeshProUGUI selectLabel, HGTextMeshProUGUI banishLabel, HGTextMeshProUGUI rerollLabel) {
            _selectLabel = selectLabel;
            _banishLabel = banishLabel;
            _rerollLabel = rerollLabel;
        }

        public void UpdateTokens(int selectTokens, int banishTokens, int rerollTokens) {
            _selectLabel?.text = $"<style=cIsHealing>◆ SELECT</style>  {selectTokens}";
            _banishLabel?.text = $"<style=cIsHealth>◆ BANISH</style>  {banishTokens}";
            _rerollLabel?.text = $"<style=cIsUtility>◆ REROLL</style>  {rerollTokens}";

            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null) {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            }
        }
    }
}
