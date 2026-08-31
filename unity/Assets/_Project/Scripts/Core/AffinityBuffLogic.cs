using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 호감도 대화 버프 = 증강의 누적 상한(maxCap) × 비율. 인게임 레벨업의
    /// 증강 누적(LevelSystem._accumulated)과는 완전히 별개로 친다 — 런
    /// 시작부터 작은 보너스를 받되, 이후 레벨업 선택지가 이 때문에 줄어들면
    /// 안 된다.
    /// </summary>
    public static class AffinityBuffLogic
    {
        public static float GetBuffAmount(float maxCap, float ratio)
            => Mathf.Max(0f, maxCap) * Mathf.Clamp01(ratio);
    }
}
