using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 버튼 호버 확대 + 눌림 축소. ButtonJuiceBootstrap이 씬의 모든 Button에 자동으로
    /// 붙이므로 씬/프리팹에 따로 배선하지 않는다. 일시정지·팝업 중에도 움직여야 해서
    /// unscaled 시간을 쓴다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
                                IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float hoverScale = 1.06f;
        [SerializeField] private float pressedScale = 0.94f;
        [Tooltip("배율 전환 속도. 클수록 빠르다.")]
        [SerializeField] private float speed = 16f;

        private Button _button;
        private float _current = 1f;
        private float _lastApplied = 1f;
        private float _appearScale = 1f;
        private bool _over;
        private bool _pressed;

        /// <summary>
        /// 등장 연출(LevelUpPanel 등)이 조절하는 스케일. 호버/눌림 배율과 곱해서 한 곳에서만
        /// localScale에 쓴다 — 둘이 각자 쓰면 서로 덮어쓴다.
        /// </summary>
        public float AppearScale
        {
            get => _appearScale;
            set
            {
                _appearScale = value;
                Apply();
            }
        }

        private void Awake() => _button = GetComponent<Button>();

        private void OnDisable()
        {
            _over = false;
            _pressed = false;
            _current = 1f;
            Apply();
        }

        private void Update()
        {
            float target = 1f;
            if (_button.interactable)
                target = _pressed ? pressedScale : (_over ? hoverScale : 1f);

            _current = Mathf.Lerp(_current, target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
            if (Mathf.Abs(_current - target) < 0.001f) _current = target;

            Apply();
        }

        // 값이 변했을 때만 쓴다. 평소(=1)에는 다른 스크립트가 만지는 스케일을 건드리지 않는다.
        private void Apply()
        {
            float scale = _appearScale * _current;
            if (Mathf.Approximately(scale, _lastApplied)) return;

            _lastApplied = scale;
            transform.localScale = Vector3.one * scale;
        }

        public void OnPointerEnter(PointerEventData eventData) => _over = true;

        public void OnPointerExit(PointerEventData eventData)
        {
            _over = false;
            _pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData) => _pressed = true;

        public void OnPointerUp(PointerEventData eventData) => _pressed = false;
    }
}
