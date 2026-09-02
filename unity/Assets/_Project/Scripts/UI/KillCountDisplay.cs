using UnityEngine;
using UnityEngine.UI;

namespace SushiSurvival.UI
{
    /// <summary>화면 상단 우측 처치 수. 캐릭터 선택·결과 화면에서는 숨긴다.</summary>
    public class KillCountDisplay : MonoBehaviour
    {
        [SerializeField] private Text countText;

        private void Update()
        {
            if (countText == null) return;

            var manager = Core.GameManager.Instance;
            if (manager == null) return;

            bool playing = manager.CurrentState == Core.RunState.Playing;
            countText.enabled = playing;
            if (!playing) return;

            countText.text = manager.KillCount.ToString();
        }
    }
}
