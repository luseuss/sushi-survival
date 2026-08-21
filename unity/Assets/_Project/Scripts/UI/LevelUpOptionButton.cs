using System;
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    [RequireComponent(typeof(Button))]
    public class LevelUpOptionButton : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameText;

        private Button _button;
        private IUpgradeOption _option;
        private Action<IUpgradeOption> _onChosen;

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

        public void Bind(IUpgradeOption option, Action<IUpgradeOption> onChosen)
        {
            _option = option;
            _onChosen = onChosen;

            gameObject.SetActive(true);

            if (nameText != null)
                nameText.text = option.DisplayName;

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
