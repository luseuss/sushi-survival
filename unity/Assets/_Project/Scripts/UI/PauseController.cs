using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 게임 중 ESC로 일시정지 패널을 토글한다. 패널과 별개의 항상 켜져 있는
    /// 오브젝트에 붙여야 한다 — 패널이 꺼져 있어도 Update가 돌아야 ESC를 받는다.
    /// </summary>
    public class PauseController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button resumeButton;

        private bool _paused;

        private void Awake()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(Resume);

            if (panel != null)
                panel.SetActive(false);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

            if (_paused)
                Resume();
            else
                TryPause();
        }

        private void OnDestroy()
        {
            if (resumeButton != null)
                resumeButton.onClick.RemoveListener(Resume);

            // 멈춘 채로 씬이 바뀌면 다음 씬이 timeScale 0으로 시작한다.
            if (_paused)
                Time.timeScale = 1f;
        }

        private void TryPause()
        {
            GameManager manager = GameManager.Instance;
            bool isPlaying = manager != null && manager.CurrentState == RunState.Playing;
            if (!PauseLogic.CanPause(isPlaying, Time.timeScale)) return;

            _paused = true;
            Time.timeScale = 0f;

            if (panel != null)
                panel.SetActive(true);
        }

        private void Resume()
        {
            if (!_paused) return;

            _paused = false;
            Time.timeScale = 1f;

            if (panel != null)
                panel.SetActive(false);
        }
    }
}
