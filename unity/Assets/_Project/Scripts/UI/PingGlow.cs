using UnityEngine;
using UnityEngine.UI;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 버튼 뒤에서 커지며 사라지는 빛 테두리를 반복해 눈길을 끈다(와사비 버튼 강조).
    /// 버튼 바로 앞 형제로 두면 버튼 그림 뒤에 깔려 테두리만 밖으로 퍼져 보인다. 대상 버튼의 위치·크기를 따라가고,
    /// 버튼이 꺼져 있거나 아직 작게 접혀 있으면(팝업 등장 중) 함께 숨는다.
    /// 팝업이 떠 있는 동안 게임 시간이 멈추므로 실시간(unscaled) 시간을 쓴다.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class PingGlow : MonoBehaviour
    {
        [Tooltip("빛이 둘러싸 퍼질 버튼. 이 버튼과 같은 자리·크기를 따라간다.")]
        [SerializeField] private RectTransform target;
        [Tooltip("한 번 퍼져 나가는 데 걸리는 시간(초).")]
        [SerializeField] private float period = 1.3f;
        [Tooltip("가장 크게 퍼졌을 때의 배율.")]
        [SerializeField] private float maxScale = 1.15f;
        [Tooltip("퍼지기 시작할 때의 투명도. 끝에서는 0이 된다.")]
        [Range(0f, 1f)]
        [SerializeField] private float startAlpha = 0.7f;

        private Image _image;
        private RectTransform _rect;
        private Color _baseColor;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _rect = (RectTransform)transform;
            _image.raycastTarget = false;
            _baseColor = _image.color;
        }

        private void Update()
        {
            bool visible = target == null || (target.gameObject.activeInHierarchy && target.localScale.x > 0.95f);

            if (target != null)
            {
                _rect.anchorMin = target.anchorMin;
                _rect.anchorMax = target.anchorMax;
                _rect.pivot = target.pivot;
                _rect.anchoredPosition = target.anchoredPosition;
                _rect.sizeDelta = target.sizeDelta;
            }

            float cycle = period > 0.01f ? (Time.unscaledTime % period) / period : 0f;

            // 처음에 빠르게 퍼지고 끝에서 천천히 사라진다.
            float eased = 1f - (1f - cycle) * (1f - cycle);
            _rect.localScale = Vector3.one * Mathf.Lerp(1f, maxScale, eased);

            Color color = _baseColor;
            color.a = visible ? startAlpha * (1f - cycle) : 0f;
            _image.color = color;
        }
    }
}
