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

        [Header("채움 범위 (화면 전체 크기 이미지용)")]
        [Tooltip("채움 이미지가 화면 전체 크기인데 막대가 그 일부 구간에만 그려져 있으면 켠다. 그림에서 불투명한 가로 구간을 " +
                 "찾아 그 구간에 체력 0~100%를 맞춘다. 스프라이트가 Read/Write 가능해야 한다. 끄면 이미지 전체 너비를 쓴다.")]
        [SerializeField] private bool mapToOpaqueRange;
        [Range(0f, 1f)]
        [SerializeField] private float fillRangeStart;
        [Range(0f, 1f)]
        [SerializeField] private float fillRangeEnd = 1f;

        [Header("캐릭터 따라다니기")]
        [Tooltip("켜면 이 체력바가 움직이는 플레이어 아래를 따라다닌다. 끄면 놓아 둔 자리에 고정된다.")]
        [SerializeField] private bool followPlayer;
        [Tooltip("따라다닐 때 움직일 사각형. 비워두면 이 오브젝트 자신이다. 부모가 캔버스여야 한다.")]
        [SerializeField] private RectTransform followRoot;
        [Tooltip("플레이어 위치에서 체력바까지의 월드 오프셋. 아래로 내리려면 y를 음수로 둔다. 카메라 확대에 맞춰 화면 거리도 함께 변한다.")]
        [SerializeField] private Vector2 worldOffset = new Vector2(0f, -0.7f);

        private float _currentFill = 1f;
        private float _targetFill = 1f;
        private Camera _camera;
        private Canvas _canvas;

        private void Awake()
        {
            if (mapToOpaqueRange) DetectFillRange();

            if (followRoot == null) followRoot = transform as RectTransform;
        }

        // 카메라가 움직인 뒤에 위치를 잡아야 흔들리지 않는다 — 모든 LateUpdate가 끝나고 캔버스를 그리기 직전에 맞춘다.
        private void UpdateFollowPosition()
        {
            if (!followPlayer || playerHealth == null || followRoot == null) return;

            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            var parentRect = followRoot.parent as RectTransform;
            if (parentRect == null) return;

            if (_canvas == null) _canvas = parentRect.GetComponentInParent<Canvas>();

            Vector3 world = playerHealth.transform.position + (Vector3)worldOffset;
            Vector3 screen = _camera.WorldToScreenPoint(world);

            Camera uiCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screen, uiCamera, out Vector2 local))
                return;

            // 앵커가 부모의 정중앙(0.5, 0.5)일 때 anchoredPosition은 부모 사각형 중심에서 잰 거리다.
            followRoot.anchoredPosition = local - parentRect.rect.center;
        }

        // 막대 그림이 차지하는 가로 구간을 한 번만 찾는다. 읽을 수 없는 스프라이트면 설정된 범위를 그대로 쓴다.
        private void DetectFillRange()
        {
            if (fillImage == null || fillImage.sprite == null) return;

            Texture2D texture = fillImage.sprite.texture;
            if (texture == null || !texture.isReadable)
            {
                Debug.LogWarning($"{name}: 채움 스프라이트가 Read/Write가 아니라 막대 구간을 자동으로 찾지 못합니다. 설정한 범위를 씁니다.");
                return;
            }

            if (HealthBarLogic.TryFindOpaqueRangeX(texture.GetPixels32(), texture.width, texture.height, 16,
                                                    out float start, out float end))
            {
                fillRangeStart = start;
                fillRangeEnd = end;
            }
        }

        private void OnEnable()
        {
            Canvas.willRenderCanvases += UpdateFollowPosition;

            if (playerHealth == null) return;

            playerHealth.OnHealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            Canvas.willRenderCanvases -= UpdateFollowPosition;

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
            fillImage.fillAmount = mapToOpaqueRange
                ? HealthBarLogic.MapFill(_currentFill, fillRangeStart, fillRangeEnd)
                : _currentFill;
        }

        private void HandleHealthChanged(float current, float max)
        {
            _targetFill = HealthBarLogic.ComputeFillAmount(current, max);
        }
    }
}
