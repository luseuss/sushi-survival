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
        [Tooltip("대화창 안 작은 초상화 창. 비워두면 표시하지 않는다.")]
        [SerializeField] private Image miniPortraitImage;
        [SerializeField] private Text questionText;
        [Tooltip("선택지 버튼 최대 3개.")]
        [SerializeField] private AffinityChoiceButton[] choiceButtons;

        private GameObject Root => root != null ? root : gameObject;

        private void Awake() => Hide();

        /// <param name="portrait">대화창 안 미니 초상화(정사각).</param>
        /// <param name="standing">화면에 크게 서 있는 입상. null이면 portrait로 대신한다.</param>
        public void Show(Sprite portrait, Sprite standing, AffinityDialogueQuestion question,
                         Action<AffinityDialogueChoice> onChosen)
        {
            Root.SetActive(true);

            if (portraitImage != null)
                portraitImage.sprite = standing != null ? standing : portrait;

            if (miniPortraitImage != null)
                miniPortraitImage.sprite = portrait;

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
