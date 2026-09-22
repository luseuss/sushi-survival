using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Data;
using SushiSurvival.UI;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 선택이 제한된 캐릭터(이나리)를 클릭했을 때의 안내 화면. 플레이어를 스폰하지도
    /// GameManager.CurrentState를 바꾸지도 않는다 — 캐릭터 선택 화면 안에서만 오간다.
    /// </summary>
    public class LockedCharacterController : MonoBehaviour
    {
        [SerializeField] private GameObject characterSelectPanel;
        [Tooltip("안내 문구를 보여주는 나레이션 패널(StoryScene에서 만든 것과 같은 컴포넌트).")]
        [SerializeField] private StoryPanel introPanel;
        [SerializeField] private Button backButton;
        [SerializeField] private Button continueButton;

        private CharacterData _current;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(HandleBackClicked);
            if (continueButton != null) continueButton.onClick.AddListener(HandleContinueClicked);

            if (introPanel != null) introPanel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (backButton != null) backButton.onClick.RemoveListener(HandleBackClicked);
            if (continueButton != null) continueButton.onClick.RemoveListener(HandleContinueClicked);
        }

        public void Show(CharacterData data)
        {
            _current = data;

            if (characterSelectPanel != null)
                characterSelectPanel.SetActive(false);

            ShowMessage();
        }

        private void ShowMessage()
        {
            if (introPanel == null || _current == null) return;

            introPanel.gameObject.SetActive(true);
            introPanel.ShowLine(new StoryLine
            {
                speakerName = _current.characterName,
                text = _current.lockedMessage,
            });
        }

        private void HandleBackClicked()
        {
            if (introPanel != null) introPanel.gameObject.SetActive(false);
            if (characterSelectPanel != null) characterSelectPanel.SetActive(true);
            _current = null;
        }

        // "계속 진행하기"는 실제로 진행되지 않는다 — 같은 안내를 다시 보여줄 뿐이다.
        private void HandleContinueClicked() => ShowMessage();
    }
}
