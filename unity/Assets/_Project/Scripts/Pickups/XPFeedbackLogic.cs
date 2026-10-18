using UnityEngine;

namespace SushiSurvival.Pickups
{
    /// <summary>
    /// 밥알(경험치 젬)을 먹을 때의 연출 값 계산 — 플레이어에게 빨려 들어가는 비행과 XP 게이지 반짝임.
    /// 시간과 거리만 받는 순수 함수라 적용은 XPGem, XpGaugeDisplay가 한다.
    /// </summary>
    public static class XPFeedbackLogic
    {
        /// <summary>게이지 반짝임 세기 — 경험치만 오를 때.</summary>
        public const float GainPulse = 0.6f;

        /// <summary>게이지 반짝임 세기 — 레벨이 오를 때.</summary>
        public const float LevelUpPulse = 1f;

        /// <summary>
        /// 빨려 들어가는 동안의 크기 배율. 시작 거리에서 1, 닿는 순간 minScale로 줄어든다.
        /// startDistance가 0 이하면 줄이지 않는다.
        /// </summary>
        public static float ShrinkScale(float distance, float startDistance, float minScale)
        {
            if (startDistance <= 0f) return 1f;

            float t = Mathf.Clamp01(distance / startDistance);
            return Mathf.Lerp(Mathf.Clamp01(minScale), 1f, t);
        }

        /// <summary>플레이어에게 충분히 가까워졌거나 너무 오래 날고 있으면 먹은 것으로 친다(경험치가 늦게 들어오는 일을 막는다).</summary>
        public static bool ShouldCollect(float distance, float flightSeconds, float collectDistance, float maxFlightSeconds)
            => distance <= collectDistance || flightSeconds >= maxFlightSeconds;

        /// <summary>
        /// 이번 프레임에 게이지를 얼마나 세게 반짝일지. 0이면 반짝이지 않는다.
        /// 레벨이 올랐으면 LevelUpPulse, 같은 레벨에서 진행률이 늘었으면 GainPulse.
        /// </summary>
        public static float GaugePulse(int previousLevel, float previousRatio, int level, float ratio)
        {
            if (level > previousLevel) return LevelUpPulse;
            if (ratio > previousRatio + 0.0001f) return GainPulse;
            return 0f;
        }

        /// <summary>반짝임이 duration초에 걸쳐 사라지도록 한 프레임 줄인다.</summary>
        public static float PulseDecay(float pulse, float deltaTime, float duration)
        {
            if (duration <= 0f) return 0f;
            return Mathf.Max(0f, pulse - deltaTime / duration);
        }
    }
}
