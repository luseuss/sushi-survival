using System;
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Data;

namespace SushiSurvival.UI
{
    [RequireComponent(typeof(Button))]
    public class AffinityChoiceButton : MonoBehaviour
    {
        [SerializeField] private Text choiceText;

        private Button _button;
        private AffinityDialogueChoice _choice;
        private Action<AffinityDialogueChoice> _onChosen;

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

        public void Bind(AffinityDialogueChoice choice, Action<AffinityDialogueChoice> onChosen)
        {
            _choice = choice;
            _onChosen = onChosen;

            gameObject.SetActive(true);

            if (choiceText != null)
                choiceText.text = choice.choiceText;
        }

        public void Clear()
        {
            _choice = null;
            _onChosen = null;
            gameObject.SetActive(false);
        }

        private void HandleClick()
        {
            if (_choice == null) return;

            _onChosen?.Invoke(_choice);
        }
    }
}
