using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    /// <summary>무기 설명창의 한 줄 — "Lv 1 → 2"와 그때 바뀌는 내용.</summary>
    public class WeaponGuideEntry : MonoBehaviour
    {
        [SerializeField] private Text levelText;
        [SerializeField] private Text descriptionText;

        public void Bind(WeaponGuideStep step)
        {
            if (levelText != null)
                levelText.text = $"Lv {step.FromLevel} → {step.ToLevel}";

            if (descriptionText != null)
                descriptionText.text = step.Text;
        }
    }
}
