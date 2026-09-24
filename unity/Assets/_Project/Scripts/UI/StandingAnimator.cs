using UnityEngine;
using UnityEngine.UI;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 대화창의 큰 입상 연출. 화자가 바뀌어 스프라이트가 달라지면 옆에서 미끄러지며 페이드인하고,
    /// 서 있는 동안엔 숨 쉬듯 위아래로 살짝 움직인다. 패널이 런타임에 붙이므로 씬 배선이 필요 없다.
    /// 대화 중엔 timeScale이 0일 수 있어 unscaled 시간으로 진행한다.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class StandingAnimator : MonoBehaviour
    {
        private const float EnterDuration = 0.35f;
        private const float EnterOffsetX = -70f;
        private const float BobAmplitude = 7f;
        private const float BobPeriod = 3.2f;

        private Image _image;
        private RectTransform _rect;
        private Vector2 _basePosition;
        private bool _hasBase;
        private Sprite _shown;
        private float _enterElapsed = EnterDuration;
        private float _bobClock;

        private void Awake() => Cache();

        private void Cache()
        {
            if (_image != null) return;

            _image = GetComponent<Image>();
            _rect = transform as RectTransform;
        }

        /// <summary>null이면 입상을 숨긴다. 직전과 같은 스프라이트면 등장 연출을 다시 하지 않는다.</summary>
        public void Show(Sprite sprite)
        {
            Cache();

            if (sprite == null)
            {
                _shown = null;
                gameObject.SetActive(false);
                return;
            }

            // 씬에 놓인 위치를 기준으로 삼는다. 첫 표시 때 한 번만 잡는다.
            if (!_hasBase)
            {
                _basePosition = _rect.anchoredPosition;
                _hasBase = true;
            }

            bool wasHidden = !gameObject.activeSelf;
            _image.sprite = sprite;
            gameObject.SetActive(true);

            if (wasHidden || sprite != _shown)
            {
                _shown = sprite;
                _enterElapsed = 0f;
                Apply(0f);
            }
        }

        private void OnDisable()
        {
            _shown = null;
            if (_hasBase && _rect != null) _rect.anchoredPosition = _basePosition;
        }

        private void Update()
        {
            if (!_hasBase) return;

            _bobClock += Time.unscaledDeltaTime;

            if (_enterElapsed < EnterDuration)
                _enterElapsed += Time.unscaledDeltaTime;

            Apply(Mathf.Clamp01(_enterElapsed / EnterDuration));
        }

        private void Apply(float enterProgress)
        {
            float eased = 1f - Mathf.Pow(1f - enterProgress, 3f);

            float bob = Mathf.Sin(_bobClock * Mathf.PI * 2f / BobPeriod) * BobAmplitude * eased;
            Vector2 offset = new Vector2(EnterOffsetX * (1f - eased), bob);
            _rect.anchoredPosition = _basePosition + offset;

            Color color = _image.color;
            color.a = eased;
            _image.color = color;
        }
    }
}
