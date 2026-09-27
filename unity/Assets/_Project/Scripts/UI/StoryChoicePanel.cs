using System;
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Data;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 스토리 중간 질문의 선택지 버튼들. Legacy Button 최대 3개를 쓰고, 각 버튼의 자식 Text에 문구를 넣는다.
    /// 씬에서 켜져 있든 꺼져 있든 상관없다 — 첫 Show가 켜면서 버튼 리스너가 붙는다.
    /// </summary>
    public class StoryChoicePanel : MonoBehaviour
    {
        [Tooltip("패널 루트. 비워두면 이 오브젝트 자신을 켜고 끈다.")]
        [SerializeField] private GameObject root;
        [Tooltip("선택지 버튼 최대 3개. 각 버튼의 자식 Text에 문구가 들어간다.")]
        [SerializeField] private Button[] buttons;

        private Action<int> _onChosen;

        private GameObject Root => root != null ? root : gameObject;

        private void Awake()
        {
            if (buttons == null) return;

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;

                int index = i;
                buttons[i].onClick.AddListener(() => HandleClicked(index));
            }
        }

        public void Show(StoryChoice[] choices, Action<int> onChosen)
        {
            if (buttons == null || buttons.Length == 0)
            {
                Debug.LogError($"{name}: buttons가 비어 있어 선택지를 표시할 수 없습니다.");
                return;
            }

            _onChosen = onChosen;
            Root.SetActive(true);

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;

                bool used = choices != null && i < choices.Length && choices[i] != null;
                buttons[i].gameObject.SetActive(used);
                if (!used) continue;

                Text label = buttons[i].GetComponentInChildren<Text>();
                if (label != null) label.text = choices[i].choiceText;
            }
        }

        public void Hide()
        {
            _onChosen = null;
            Root.SetActive(false);
        }

        private void HandleClicked(int index)
        {
            Action<int> callback = _onChosen;
            if (callback == null) return;

            // 같은 클릭이 두 번 처리되지 않게 먼저 비운다.
            _onChosen = null;
            callback(index);
        }
    }
}
