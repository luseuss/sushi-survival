using UnityEngine;

namespace SushiSurvival.Core
{
    public static class PauseLogic
    {
        /// <summary>
        /// 전투 중이고 timeScale이 정상(1)일 때만 일시정지할 수 있다. 팝업·대화(0)나
        /// 보스 연출·슬로모션(0.3)·히트스톱(0) 중에 멈췄다가 1로 되돌리면 그 연출이
        /// 깨지기 때문이다.
        /// </summary>
        public static bool CanPause(bool isPlaying, float timeScale)
            => isPlaying && Mathf.Approximately(timeScale, 1f);
    }
}
