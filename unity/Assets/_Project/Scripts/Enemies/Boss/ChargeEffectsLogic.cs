using UnityEngine;

namespace SushiSurvival.Enemies.Boss
{
    /// <summary>돌진 연출이 쓰는 순수 계산. 화면 반영과 시간 진행은 BossChargeEffects·BossController가 맡는다.</summary>
    public static class ChargeEffectsLogic
    {
        /// <summary>
        /// 돌진이 가는 최대 거리 = 이동속도 × 속도배율 × 돌진 시간. 예고선 길이가 이 값이다.
        /// 실제 이동은 플레이어에게 막힐 수 있어서 "최대 도달 거리"다.
        /// </summary>
        public static float ChargeDistance(float moveSpeed, float speedScale, float duration)
            => Mathf.Max(0f, moveSpeed) * Mathf.Max(0f, speedScale) * Mathf.Max(0f, duration);

        /// <summary>
        /// 예고가 시작된 뒤 돌진 방향을 확정하고 예고선을 고정하는 시점(초) = 예고 시간 − lockSeconds.
        /// lockSeconds가 0이면 예고 종료 순간(기존 동작)이고, 예고보다 길면 0(즉시 확정)이다.
        /// </summary>
        public static float LockDelay(float windup, float lockSeconds)
        {
            float clampedWindup = Mathf.Max(0f, windup);
            return Mathf.Clamp(clampedWindup - Mathf.Max(0f, lockSeconds), 0f, clampedWindup);
        }
    }
}
