using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    [RequireComponent(typeof(Button))]
    public class LevelUpOptionButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameText;
        [Tooltip("제목 아래 설명 텍스트. 비워두면 설명을 표시하지 않는다.")]
        [SerializeField] private Text descriptionText;
        [Tooltip("마우스를 올렸을 때의 확대 배율.")]
        [SerializeField] private float hoverScale = 1.08f;
        [Tooltip("호버 확대/복귀 속도. 클수록 빠르다.")]
        [SerializeField] private float hoverSpeed = 14f;

        private Button _button;
        private IUpgradeOption _option;
        private Action<IUpgradeOption> _onChosen;
        private float _hover = 1f;
        private bool _pointerOver;

        /// <summary>
        /// 팝업 등장 연출이 조절하는 스케일. 호버 배율과 곱해져서 적용된다 —
        /// 둘 다 localScale을 건드리므로 한 곳(Update)에서만 합쳐서 쓴다.
        /// </summary>
        public float AppearScale { get; set; } = 1f;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);
        }

        private void OnDisable()
        {
            _pointerOver = false;
            _hover = 1f;
        }

        // 팝업이 떠 있는 동안 timeScale이 0이라 반드시 unscaled 시간으로 진행한다.
        private void Update()
        {
            float target = _pointerOver && _button.interactable ? hoverScale : 1f;
            _hover = Mathf.Lerp(_hover, target, 1f - Mathf.Exp(-hoverSpeed * Time.unscaledDeltaTime));
            transform.localScale = Vector3.one * (AppearScale * _hover);
        }

        public void OnPointerEnter(PointerEventData eventData) => _pointerOver = true;

        public void OnPointerExit(PointerEventData eventData) => _pointerOver = false;

        public void Bind(IUpgradeOption option, Action<IUpgradeOption> onChosen)
        {
            _option = option;
            _onChosen = onChosen;

            gameObject.SetActive(true);

            if (nameText != null)
                nameText.text = option.DisplayName;

            if (descriptionText != null)
                descriptionText.text = option.Description;

            if (iconImage != null)
            {
                iconImage.sprite = option.Icon;
                iconImage.enabled = option.Icon != null;
            }
        }

        public void Clear()
        {
            _option = null;
            _onChosen = null;
            gameObject.SetActive(false);
        }

        private void HandleClick()
        {
            if (_option == null) return;

            _onChosen?.Invoke(_option);
        }
    }
}
