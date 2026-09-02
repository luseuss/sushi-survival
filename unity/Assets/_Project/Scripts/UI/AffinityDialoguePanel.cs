using System;
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Data;

namespace SushiSurvival.UI
{
    public class AffinityDialoguePanel : MonoBehaviour
    {
        [Tooltip("패널 루트. 비워두면 이 오브젝트 자신을 켜고 끈다.")]
        [SerializeField] private GameObject root;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Text questionText;
        [Tooltip("선택지 버튼 최대 3개.")]
        [SerializeField] private AffinityChoiceButton[] choiceButtons;

        private GameObject Root => root != null ? root : gameObject;

        private void Awake() => Hide();

        public void Show(Sprite portrait, AffinityDialogueQuestion question, Action<AffinityDialogueChoice> onChosen)
        {
            Root.SetActive(true);

            if (portraitImage != null)
                portraitImage.sprite = portrait;

            if (questionText != null)
                questionText.text = question.questionText;

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (i < question.choices.Length)
                    choiceButtons[i].Bind(question.choices[i], onChosen);
                else
                    choiceButtons[i].Clear();
            }
        }

        public void Hide() => Root.SetActive(false);
    }
}
