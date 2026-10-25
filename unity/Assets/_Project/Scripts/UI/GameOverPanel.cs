using SushiSurvival.Audio;
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    public class GameOverPanel : MonoBehaviour
    {
        [SerializeField] private Button exitButton;

        private void Awake()
        {
            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);
        }

        // 게임오버 창이 뜨는 순간(어느 쪽에서 켜든) 패배 곡으로 바꾼다.
        private void OnEnable() => AudioManager.PlayBgm(BgmId.Defeat, 0.2f);

        public void Show()
        {
            gameObject.SetActive(true);
        }

        private void OnExitClicked()
        {
            gameObject.SetActive(false);

            if (GameManager.Instance != null)
                GameManager.Instance.FinishRun(RunOutcome.Defeat);
        }

        private void OnDestroy()
        {
            if (exitButton != null)
                exitButton.onClick.RemoveListener(OnExitClicked);
        }
    }
}