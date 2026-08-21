using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 화면 상단에 남은 시간을 표시한다. 캐릭터 선택 중과 결과 화면에서는 숨긴다.
    /// </summary>
    public class RunTimerDisplay : MonoBehaviour
    {
        [SerializeField] private Text timerText;

        private void Update()
        {
            if (timerText == null) return;

            var manager = GameManager.Instance;
            if (manager == null) return;

            bool playing = manager.CurrentState == RunState.Playing;
            timerText.enabled = playing;

            if (!playing) return;

            // 보스 등장 전에는 남은 시간을 세고, 등장 후에는 생존 시간을 센다.
            // 그대로 두면 보스전 내내 0:00에 멈춰 있어 시계가 고장난 것처럼 보인다.
            timerText.text = manager.ElapsedTime < manager.BossSpawnTime
                ? RunClock.FormatRemaining(manager.ElapsedTime, manager.BossSpawnTime)
                : RunClock.FormatElapsed(manager.ElapsedTime);
        }
    }
}
