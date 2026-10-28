using SushiSurvival.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SushiSurvival.UI
{
    public class IntroSceneController : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [Tooltip("타이틀 배경으로 반복 재생할 영상. 비워두면 기존 배경 그림만 보인다.")]
        [SerializeField] private VideoClip backgroundVideo;

        private void Awake()
        {
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);
        }

        private void Start()
        {
            // 타이틀 배경음악. 스토리·캐릭터 선택·전투가 같은 곡을 쓰면 끊기지 않고 이어진다.
            AudioManager.PlayBgm(BgmId.Title, 0.5f);

            if (backgroundVideo != null)
                PlayBackgroundVideo();
        }

        private void OnDestroy()
        {
            if (startButton != null)
                startButton.onClick.RemoveListener(OnStartClicked);
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

        private void OnStartClicked()
        {
            SceneManager.LoadScene("StoryScene");
        }
    }
}
