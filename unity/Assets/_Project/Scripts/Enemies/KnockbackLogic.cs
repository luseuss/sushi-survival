using UnityEngine;

namespace SushiSurvival.Enemies
{
    public static class KnockbackLogic
    {
        private const float MinDistanceSqr = 0.0001f;

        /// <summary>
        /// 공격자 반대 방향으로 밀어내는 속도. 저항이 1이면 밀리지 않는다.
        /// 공격자와 적이 정확히 겹치면 방향을 정할 수 없으므로 영벡터.
        /// </summary>
        public static Vector2 ComputeImpulse(Vector2 sourcePos, Vector2 targetPos, float force, float resistance)
        {
            Vector2 away = targetPos - sourcePos;
            if (away.sqrMagnitude < MinDistanceSqr) return Vector2.zero;

            float effective = force * (1f - Mathf.Clamp01(resistance));
            return away.normalized * effective;
        }

        /// <summary>속도를 일정 비율로 줄인다. 0 아래로 내려가 반대로 튀지 않는다.</summary>
        public static Vector2 Decay(Vector2 velocity, float decayPerSecond, float deltaTime)
        {
            float speed = velocity.magnitude;
            if (speed <= 0f) return Vector2.zero;

            float reduced = Mathf.Max(0f, speed - decayPerSecond * deltaTime);
            return reduced <= 0f ? Vector2.zero : velocity / speed * reduced;
        }
    }
}
