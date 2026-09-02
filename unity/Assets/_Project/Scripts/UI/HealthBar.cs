using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Core;
using SushiSurvival.Player;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 플레이어 체력바. 캐릭터 종류와 무관한 HUD 코너 오브젝트라 인스펙터로
    /// 미리 연결할 수 없다 — GameManager가 스폰 직후 SetTarget으로 알려준다
    /// (CameraFollow.SetTarget과 같은 패턴).
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private PlayerHealth playerHealth;
        [Tooltip("Image Type을 Filled로 설정한 채움 이미지.")]
        [SerializeField] private Image fillImage;
        [Tooltip("캐릭터 초상화. 비워두면 표시하지 않는다.")]
        [SerializeField] private Image portraitImage;
        [Tooltip("초당 채움 변화량. 2면 0→1이 0.5초 걸린다.")]
        [SerializeField] private float fillSpeed = 2f;
        [Tooltip("RunState.Playing이 아닐 때(대화 중 등) 숨기는 데 쓴다. 비워두면 항상 보인다.")]
        [SerializeField] private CanvasGroup canvasGroup;

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

        /// <summary>
        /// 런타임에 플레이어가 스폰된 뒤 GameManager가 호출한다. 이전 대상이
        /// 있으면(재시작 등) 먼저 구독을 해제해 중복 구독을 막는다.
        /// </summary>
        public void SetTarget(PlayerHealth health, Sprite portrait)
        {
            if (playerHealth != null)
                playerHealth.OnHealthChanged -= HandleHealthChanged;

            playerHealth = health;

            if (playerHealth != null)
                playerHealth.OnHealthChanged += HandleHealthChanged;

            if (portraitImage != null)
                portraitImage.sprite = portrait;
        }

        private void Update()
        {
            // Update가 계속 돌아야 상태가 Playing으로 바뀌는 순간을 다시 잡을 수
            // 있으므로, 여기서는 GameObject를 끄지 않고 CanvasGroup 알파만
            // 조절한다(자기 자신을 SetActive(false)하면 이 메서드 자체가 멈춘다).
            if (canvasGroup != null && GameManager.Instance != null)
            {
                bool playing = GameManager.Instance.CurrentState == RunState.Playing;
                canvasGroup.alpha = playing ? 1f : 0f;
            }

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
