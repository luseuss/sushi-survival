using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 화면 상단에 남은 시간을 표시한다. 캐릭터 선택 중과 결과 화면에서는 숨긴다.
    /// 보스가 등장하면(ElapsedTime이 BossSpawnTime을 넘으면) 보스 체력바에게
    /// 자리를 내주고 아래로 밀려난다 — 새 이벤트 연결 없이 이미 읽고 있는
    /// 값으로 스스로 판단한다.
    /// </summary>
    public class RunTimerDisplay : MonoBehaviour
    {
        [SerializeField] private Text timerText;
        [SerializeField] private float normalY = 12f;
        [SerializeField] private float bossPhaseY = 76f;
        [SerializeField] private float moveDuration = 0.3f;

        private bool _bossPhaseActive;
        private Coroutine _moveRoutine;

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

            bool bossPhase = manager.ElapsedTime >= manager.BossSpawnTime;
            if (bossPhase != _bossPhaseActive)
            {
                _bossPhaseActive = bossPhase;
                if (_moveRoutine != null) StopCoroutine(_moveRoutine);
                _moveRoutine = StartCoroutine(MoveTo(bossPhase ? bossPhaseY : normalY));
            }
        }

        private IEnumerator MoveTo(float targetY)
        {
            RectTransform rect = timerText.rectTransform;
            Vector2 start = rect.anchoredPosition;
            var target = new Vector2(start.x, targetY);
            float elapsed = 0f;

            // 게임 진행 중의 연출이라 timeScale이 항상 1이다 — 레벨업 팝업
            // 스케일인과 달리 실시간(unscaled)을 쓸 필요가 없다.
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                rect.anchoredPosition = Vector2.Lerp(start, target, Mathf.Clamp01(elapsed / moveDuration));
                yield return null;
            }

            rect.anchoredPosition = target;
            _moveRoutine = null;
        }
    }
}
