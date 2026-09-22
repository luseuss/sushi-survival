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