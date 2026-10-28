using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 보스를 잡은 직후, 결과창보다 먼저 뜨는 게임 클리어 창. 나가기 버튼을 누르면 결과창(승리)으로 넘어간다.
    /// </summary>
    public class GameClearPanel : MonoBehaviour
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
                GameManager.Instance.FinishRun(RunOutcome.Victory);
        }

        private void OnDestroy()
        {
            if (exitButton != null)
                exitButton.onClick.RemoveListener(OnExitClicked);
        }
    }
}
