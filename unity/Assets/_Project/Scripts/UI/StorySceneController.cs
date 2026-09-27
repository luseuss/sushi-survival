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
    ///
    /// 대화 중간에 질문(choicePoints)이 끼면 질문 대사 → 선택지 → 고른 성향 기록 → 반응 대사 한 줄을
    /// 거쳐 원래 대사로 돌아온다. choicePanel이 비어 있거나 질문이 없으면 기존 선형 재생과 같다.
    /// </summary>
    public class StorySceneController : MonoBehaviour
    {
        [SerializeField] private StoryDialogueData data;
        [SerializeField] private StoryPanel panel;
        [Tooltip("질문 선택지를 보여주는 패널. 비워두면 질문 없이 선형으로만 재생한다.")]
        [SerializeField] private StoryChoicePanel choicePanel;
        [SerializeField] private Button skipButton;
        [Tooltip("대화가 끝나거나 건너뛰면 이동할 씬. Build Settings 등록명과 정확히 같아야 한다.")]
        [SerializeField] private string nextSceneName = "GameScene";
        [Tooltip("배경 전환 페이드 시간(초, 실시간).")]
        [SerializeField] private float backgroundFadeSeconds = 0.35f;

        private bool[] _hasBackground;
        private bool[] _pointDone;
        private int _index;
        private bool _leaving;
        private bool _awaitingChoice;
        private bool _showingReply;
        private int _choiceFrame = -1;
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
            // 판마다 처음부터. "다시 하기"는 IntroScene → StoryScene을 거치므로 여기서 비우면 된다.
            PlayerTraitState.Reset();

            // 결과 화면 등에서 정지된 채 넘어오는 경우를 막는다.
            Time.timeScale = 1f;

            if (choicePanel != null)
                choicePanel.Hide();

            if (data == null || data.lines == null || data.lines.Length == 0 || panel == null)
            {
                Debug.LogWarning($"{name}: 대화 데이터나 패널이 비어 있어 스토리를 건너뜁니다.");
                Leave();
                return;
            }

            _hasBackground = new bool[data.lines.Length];
            for (int i = 0; i < data.lines.Length; i++)
                _hasBackground[i] = data.lines[i].background != null;

            _pointDone = new bool[data.choicePoints != null ? data.choicePoints.Length : 0];

            _index = 0;
            ShowCurrent(immediateBackground: true);
        }

        private void Update()
        {
            if (_leaving || _hasBackground == null) return;

            // 선택 중에는 클릭·Space·Enter로 넘어가지 않는다. 버튼으로만 진행한다.
            if (_awaitingChoice) return;

            // 선택 버튼을 누른 같은 프레임의 마우스 떼기가 "다음 줄" 입력으로도 세어지지 않게 한다.
            // EventSystem과 이 Update의 실행 순서가 어느 쪽이어도 안전하도록 프레임으로 막는다.
            if (_choiceFrame == Time.frameCount) return;

            if (!AdvancePressed()) return;

            // 아직 찍히는 중이면 다음 줄로 넘기지 않고 전체 문장부터 보여준다.
            if (panel.IsTyping)
            {
                panel.CompleteTyping();
                return;
            }

            // 반응 대사를 다 본 뒤엔 원래 대사 줄로 돌아온다.
            if (_showingReply)
            {
                _showingReply = false;
                ShowLineAt(_index, immediateBackground: false);
                return;
            }

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

        // 지금 줄 앞에 아직 안 한 질문이 있으면 그것부터, 없으면 대사 줄을 보여준다.
        private void ShowCurrent(bool immediateBackground)
        {
            int pointIndex = FindPendingPoint();
            if (pointIndex >= 0)
            {
                // 첫 화면이 질문이어도 깔려 있어야 할 배경은 바로 둔다.
                if (immediateBackground)
                    ApplyImmediateBackground();

                BeginChoice(pointIndex);
                return;
            }

            ShowLineAt(_index, immediateBackground);
        }

        private int FindPendingPoint()
        {
            if (choicePanel == null || data.choicePoints == null) return -1;

            int pointIndex = StoryChoiceLogic.FindPointIndex(data.choicePoints, _index);
            return pointIndex >= 0 && !_pointDone[pointIndex] ? pointIndex : -1;
        }

        private void BeginChoice(int pointIndex)
        {
            StoryChoicePoint point = data.choicePoints[pointIndex];
            _pointDone[pointIndex] = true;

            if (point.choices == null || point.choices.Length == 0)
            {
                ShowLineAt(_index, immediateBackground: false);
                return;
            }

            _awaitingChoice = true;

            if (point.prompt != null)
                panel.ShowLine(point.prompt);

            choicePanel.Show(point.choices, choiceIndex => OnChoiceMade(point, choiceIndex));
        }

        private void OnChoiceMade(StoryChoicePoint point, int choiceIndex)
        {
            StoryChoice choice = point.choices[choiceIndex];
            PlayerTraitState.Record(choice.trait);

            choicePanel.Hide();
            _awaitingChoice = false;
            _choiceFrame = Time.frameCount;

            bool hasReply = choice.reply != null && !string.IsNullOrEmpty(choice.reply.text);
            if (hasReply)
            {
                _showingReply = true;
                panel.ShowLine(choice.reply);
            }
            else
            {
                ShowLineAt(_index, immediateBackground: false);
            }
        }

        private void ShowLineAt(int index, bool immediateBackground)
        {
            StoryLine line = data.lines[index];
            panel.ShowLine(line);

            if (immediateBackground)
            {
                ApplyImmediateBackground();
            }
            else if (StoryDialogueLogic.IsBackgroundChange(_hasBackground, index))
            {
                panel.FadeToBackground(line.background, backgroundFadeSeconds);
            }
        }

        // 첫 화면은 페이드 없이, 지금 깔려 있어야 할 배경(없으면 null=단색)을 바로 둔다.
        private void ApplyImmediateBackground()
        {
            int backgroundLine = StoryDialogueLogic.ResolveBackgroundIndex(_hasBackground, _index);
            panel.SetBackground(backgroundLine >= 0 ? data.lines[backgroundLine].background : null);
        }

        // 누르는 순간(wasPressedThisFrame)이 아니라 떼는 순간을 본다. 마지막 줄에서
        // 넘기면 그 자리에서 다음 씬(주로 GameScene)이 로드되는데, 누르는 순간에
        // 반응하면 그 시점엔 버튼이 아직 물리적으로 눌린 상태라 새로 생긴
        // EventSystem이 그 위치의 UI(캐릭터 선택 버튼 등)에 유령 클릭을 일으킨다.
        private bool AdvancePressed()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasReleasedThisFrame && !IsPointerOnSkipButton(mouse))
                return true;

            Keyboard keyboard = Keyboard.current;
            return keyboard != null
                && (keyboard.spaceKey.wasReleasedThisFrame || keyboard.enterKey.wasReleasedThisFrame);
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
