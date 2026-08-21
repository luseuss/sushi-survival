using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Player;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 캐릭터 발밑에 붙는 체력바. PlayerHealth의 변경 이벤트만 구독하므로
    /// 매 프레임 폴링하지 않는다.
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private PlayerHealth playerHealth;
        [Tooltip("Image Type을 Filled로 설정한 채움 이미지.")]
        [SerializeField] private Image fillImage;

        private void OnEnable()
        {
            if (playerHealth == null) return;

            playerHealth.OnHealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            if (playerHealth == null) return;

            playerHealth.OnHealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (fillImage == null) return;

            fillImage.fillAmount = HealthBarLogic.ComputeFillAmount(current, max);
        }
    }
}
