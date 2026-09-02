using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
            // 씬이 정지된 상태로 로드되는 것을 방지하기 위해 반드시 timeScale을 복구한다[cite: 1]
            Time.timeScale = 1f;
            SceneManager.LoadScene("ResultScene");
        }

        private void OnDestroy()
        {
            if (exitButton != null)
                exitButton.onClick.RemoveListener(OnExitClicked);
        }
    }
}