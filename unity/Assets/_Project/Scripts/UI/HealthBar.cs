using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Player;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 캐릭터 발밑에 붙는 체력바. PlayerHealth의 변경 이벤트로 목표값만 받고,
    /// 실제 표시는 매 프레임 그 쪽으로 부드럽게 옮겨간다 — 스냅으로 바뀌면
    /// 얼마나 깎였는지 체감이 안 된다.
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private PlayerHealth playerHealth;
        [Tooltip("Image Type을 Filled로 설정한 채움 이미지.")]
        [SerializeField] private Image fillImage;
        [Tooltip("초당 채움 변화량. 2면 0→1이 0.5초 걸린다.")]
        [SerializeField] private float fillSpeed = 2f;

        private float _currentFill = 1f;
        private float _targetFill = 1f;

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

        private void Update()
        {
            if (fillImage == null) return;

            _currentFill = HealthBarLogic.MoveTowardsFill(_currentFill, _targetFill, fillSpeed * Time.deltaTime);
            fillImage.fillAmount = _currentFill;
        }

        private void HandleHealthChanged(float current, float max)
        {
            _targetFill = HealthBarLogic.ComputeFillAmount(current, max);
        }
    }
}
