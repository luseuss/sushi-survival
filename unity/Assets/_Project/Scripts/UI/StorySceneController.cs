using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 선형 대화 장면의 진입점. 클릭/Space/Enter로 한 줄씩 넘기고, 끝나거나 건너뛰면
    /// 다음 씬으로 이동한다. 데이터가 없으면 경고만 남기고 바로 넘어간다 — 대본이 아직
    /// 없어도 게임 흐름은 끊기면 안 된다(호감도 대화 컨트롤러와 같은 방침).
    /// </summary>
    public class StorySceneController : MonoBehaviour
    {
        [SerializeField] private StoryDialogueData data;
        [SerializeField] private StoryPanel panel;
        [SerializeField] private Button skipButton;
        [Tooltip("대화가 끝나거나 건너뛰면 이동할 씬. Build Settings 등록명과 정확히 같아야 한다.")]
        [SerializeField] private string nextSceneName = "GameScene";
        [Tooltip("배경 전환 페이드 시간(초, 실시간).")]
        [SerializeField] private float backgroundFadeSeconds = 0.35f;

        private bool[] _hasBackground;
        private int _index;
        private bool _leaving;
        private RectTransform _skipRect;

        private void Awake()
        {
            if (skipButton == null) return;

            skipButton.onClick.AddListener(Leave);
            _skipRect = skipButton.transform as RectTransform;
        }

        private void OnDestroy()
        {
            if (skipButton != null)
                skipButton.onClick.RemoveListener(Leave);
        }

        private void Start()
        {
            // 결과 화면 등에서 정지된 채 넘어오는 경우를 막는다.
            Time.timeScale = 1f;

            if (data == null || data.lines == null || data.lines.Length == 0 || panel == null)
            {
                Debug.LogWarning($"{name}: 대화 데이터나 패널이 비어 있어 스토리를 건너뜁니다.");
                Leave();
                return;
            }

            _hasBackground = new bool[data.lines.Length];
            for (int i = 0; i < data.lines.Length; i++)
                _hasBackground[i] = data.lines[i].background != null;

            _index = 0;
            ShowCurrent(immediateBackground: true);
        }

        private void Update()
        {
            if (_leaving || _hasBackground == null) return;

            if (AdvancePressed())
                Advance();
        }

        private void Advance()
        {
            _index = StoryDialogueLogic.NextIndex(_index, data.lines.Length);

            if (StoryDialogueLogic.IsFinished(_index, data.lines.Length))
            {
                Leave();
                return;
            }

            ShowCurrent(immediateBackground: false);
        }

        private void ShowCurrent(bool immediateBackground)
        {
            StoryLine line = data.lines[_index];
            panel.ShowLine(line);

            if (immediateBackground)
            {
                // 첫 화면은 페이드 없이, 지금 깔려 있어야 할 배경(없으면 null=단색)을 바로 둔다.
                int backgroundLine = StoryDialogueLogic.ResolveBackgroundIndex(_hasBackground, _index);
                panel.SetBackground(backgroundLine >= 0 ? data.lines[backgroundLine].background : null);
            }
            else if (StoryDialogueLogic.IsBackgroundChange(_hasBackground, _index))
            {
                panel.FadeToBackground(line.background, backgroundFadeSeconds);
            }
        }

        private bool AdvancePressed()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !IsPointerOnSkipButton(mouse))
                return true;

            Keyboard keyboard = Keyboard.current;
            return keyboard != null
                && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame);
        }

        // 건너뛰기 버튼을 누른 클릭이 "다음 줄" 입력으로도 세어지지 않게 한다.
        private bool IsPointerOnSkipButton(Mouse mouse)
            => _skipRect != null
               && RectTransformUtility.RectangleContainsScreenPoint(_skipRect, mouse.position.ReadValue());

        private void Leave()
        {
            if (_leaving) return;

            _leaving = true;
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
