using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SushiSurvival.UI
{
    public class IntroSceneController : MonoBehaviour
    {
        [SerializeField] private Button startButton;

        private void Awake()
        {
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);
        }

        private void OnDestroy()
        {
            if (startButton != null)
                startButton.onClick.RemoveListener(OnStartClicked);
        }

        private void OnStartClicked()
        {
            SceneManager.LoadScene("StoryScene");
        }
    }
}
