using UnityEngine;

namespace SushiSurvival.Enemies
{
    /// <summary>중형몹이 롤 몬스터를 던지는 패턴의 순수 계산부.</summary>
    public static class MidBossThrowLogic
    {
        /// <summary>플레이어가 던질 수 있는 거리 안에 있는지. 너무 붙어 있거나 너무 멀면 던지지 않는다.</summary>
        public static bool InThrowRange(float distance, float minRange, float maxRange)
            => distance >= minRange && distance <= maxRange;

        /// <summary>
        /// 포물선 비행의 높이. 진행률 0~1에서 0 → peakHeight → 0을 그린다.
        /// 땅 위 좌표는 직선으로 보내고 그림만 이만큼 띄워서 던져진 느낌을 낸다.
        /// </summary>
        public static float ArcHeight(float progress, float peakHeight)
        {
            float t = Mathf.Clamp01(progress);
            return 4f * peakHeight * t * (1f - t);
        }
    }
}
