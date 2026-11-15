using SushiSurvival.Audio;
using SushiSurvival.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SushiSurvival.UI
{
    public class IntroSceneController : MonoBehaviour
    {
        [Tooltip("로고와 따로 있는 '게임 시작' 버튼.")]
        [SerializeField] private Button startButton;
        [Tooltip("타이틀 배경으로 반복 재생할 영상. 비워두면 기존 배경 그림만 보인다.")]
        [SerializeField] private VideoClip backgroundVideo;
        [Tooltip("'튜토리얼' 버튼의 글자와, 게임 시작 버튼보다 아래로 내릴 거리(화면 픽셀).")]
        [SerializeField] private string tutorialLabel = "튜토리얼";
        [SerializeField] private float tutorialButtonOffsetY = 80f;

        private Button _tutorialButton;
        private TutorialPrompt _prompt;

        private void Awake()
        {
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);
        }

        private void Start()
        {
            // 타이틀도 스토리 장면과 같은 곡을 쓴다. 같은 곡이면 장면이 바뀌어도 끊기지 않고 이어진다.
            AudioManager.PlayBgm(BgmId.Story, 0.5f);

            if (backgroundVideo != null)
                PlayBackgroundVideo();

            CreateTutorialButton();
        }

        private void OnDestroy()
        {
            if (startButton != null)
                startButton.onClick.RemoveListener(OnStartClicked);

            if (_tutorialButton != null)
                _tutorialButton.onClick.RemoveListener(OnTutorialClicked);
        }

        // '게임 시작' 버튼을 복제해 바로 아래에 '튜토리얼' 버튼을 만든다. 언제든 연습 플레이를 다시 볼 수 있다.
        private void CreateTutorialButton()
        {
            if (startButton == null) return;

            GameObject clone = Instantiate(startButton.gameObject, startButton.transform.parent);
            clone.name = "TutorialButton";

            var rect = (RectTransform)clone.transform;
            rect.anchoredPosition = ((RectTransform)startButton.transform).anchoredPosition + new Vector2(0f, -tutorialButtonOffsetY);

            Text label = clone.GetComponentInChildren<Text>(true);
            if (label != null) label.text = tutorialLabel;

            _tutorialButton = clone.GetComponent<Button>();
            _tutorialButton.interactable = true;
            _tutorialButton.onClick.AddListener(OnTutorialClicked);
        }

        // 캔버스의 첫 자식(배경 그림) 바로 위, Dim 아래에 끼워 넣어 어두운 딤 효과와 UI가 그대로 위에 얹힌다.
        private void PlayBackgroundVideo()
        {
            Canvas canvas = startButton != null ? startButton.GetComponentInParent<Canvas>() : FindObjectOfType<Canvas>();
            if (canvas == null) return;

            Transform canvasRoot = canvas.rootCanvas.transform;
            VideoSurface surface = VideoSurface.Create(canvasRoot, backgroundVideo, loop: true, "BackgroundVideo");
            surface.transform.SetSiblingIndex(Mathf.Min(1, canvasRoot.childCount - 1));
            surface.Play();
        }

        // 시작 버튼: 이 방문자가 연습 안내를 아직 안 봤으면 안내를 먼저 띄운다. 이미 봤으면 바로 스토리로 간다.
        private void OnStartClicked()
        {
            if (SceneTransition.IsBusy || _prompt != null) return;

            if (TutorialState.ShouldPrompt)
            {
                ShowTutorialPrompt();
                return;
            }

            GoToStory();
        }

        // 튜토리얼 버튼: 안내 없이 바로 연습 플레이로 간다.
        private void OnTutorialClicked()
        {
            if (SceneTransition.IsBusy || _prompt != null) return;

            StartTutorial();
        }

        private void ShowTutorialPrompt()
        {
            Canvas canvas = startButton.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                GoToStory();
                return;
            }

            Text anyText = startButton.GetComponentInChildren<Text>(true);
            Image buttonImage = startButton.GetComponent<Image>();
            _prompt = TutorialPrompt.Show(canvas.rootCanvas.transform, anyText != null ? anyText.font : null,
                                          buttonImage != null ? buttonImage.sprite : null, StartTutorial, SkipTutorial);
        }

        // 연습하기: GameScene이 연습 모드로 시작한다(끝나면 스토리로 넘어간다).
        private void StartTutorial()
        {
            LockButtons();
            TutorialState.RequestTutorial();
            SceneTransition.Go("GameScene");
        }

        // 건너뛰기: 이 방문자는 안내를 본 것으로 기록하고 스토리로 간다.
        private void SkipTutorial()
        {
            TutorialState.MarkSeen();
            GoToStory();
        }

        // 타이틀 화면이 커지며 어두워졌다가 스토리 장면이 밝아지며 나타난다(SceneTransition).
        private void GoToStory()
        {
            LockButtons();
            SceneTransition.Go("StoryScene");
        }

        // 연출 중 다시 눌러도 두 번 시작되지 않게 막는다.
        private void LockButtons()
        {
            if (startButton != null) startButton.interactable = false;
            if (_tutorialButton != null) _tutorialButton.interactable = false;
            if (_prompt != null) Destroy(_prompt.gameObject);
        }
    }
}
