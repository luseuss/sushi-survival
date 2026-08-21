using System.Collections.Generic;
using UnityEngine;

namespace SushiSurvival.Enemies
{
    public static class SeparationLogic
    {
        private const float MinDistanceSqr = 0.0001f;

        /// <summary>
        /// 주변 이웃에게서 멀어지는 방향을 돌려준다(정규화). 이웃이 없거나
        /// 힘이 상쇄되면 영벡터.
        /// </summary>
        public static Vector2 ComputeSeparation(Vector2 self, IReadOnlyList<Vector2> neighbors, float radius)
        {
            if (radius <= 0f || neighbors == null) return Vector2.zero;

            float radiusSqr = radius * radius;
            Vector2 push = Vector2.zero;

            foreach (Vector2 neighbor in neighbors)
            {
                Vector2 away = self - neighbor;
                float distSqr = away.sqrMagnitude;

                // 정확히 겹치면 방향을 정할 수 없다. 나누면 NaN이 되고
                // 한 번 NaN이 된 위치는 회복되지 않는다.
                if (distSqr < MinDistanceSqr) continue;
                if (distSqr > radiusSqr) continue;

                float dist = Mathf.Sqrt(distSqr);
                // 가까울수록 강하게 민다.
                push += (away / dist) * (1f - dist / radius);
            }

            return push.sqrMagnitude > MinDistanceSqr ? push.normalized : Vector2.zero;
        }
    }
}
