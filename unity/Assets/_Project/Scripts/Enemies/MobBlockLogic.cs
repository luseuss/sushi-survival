using UnityEngine;

namespace SushiSurvival.Enemies
{
    /// <summary>
    /// 몹끼리 겹치지 않게 하는 계산. 서로를 밀어내는 힘을 주는 대신(분리 힘), 이동하려는 몹이 이웃의 몸 안으로 들어가려
    /// 하면 그 경계를 따라 미끄러지게 막는다. 막는 쪽만 움직이고 이웃은 건드리지 않으므로 밀치는 느낌이 없다.
    /// 이미 겹친 채로 태어난 몹(소환·스폰)은 갑자기 튕겨 나가지 않고 한 프레임에 resolveStep만큼씩 천천히 벌어진다.
    /// </summary>
    public static class MobBlockLogic
    {
        private const float MinDelta = 0.0001f;

        /// <param name="current">지금 위치.</param>
        /// <param name="next">이동하려는 위치.</param>
        /// <param name="neighborCenter">이웃 몹의 중심.</param>
        /// <param name="minDistance">두 몹의 중심이 가져야 할 최소 거리(두 몸의 반폭 합).</param>
        /// <param name="resolveStep">이미 겹쳐 있을 때 한 번에 벌어질 수 있는 최대 거리.</param>
        public static Vector2 Resolve(Vector2 current, Vector2 next, Vector2 neighborCenter,
                                      float minDistance, float resolveStep)
        {
            if (minDistance <= 0f) return next;

            float currentDistance = Vector2.Distance(current, neighborCenter);

            // 이미 겹쳐 있으면 필요한 거리를 한 번에 채우지 않고 조금씩만 늘린다.
            float required = currentDistance >= minDistance
                ? minDistance
                : Mathf.Min(minDistance, currentDistance + Mathf.Max(0f, resolveStep));

            Vector2 delta = next - neighborCenter;
            float nextDistance = delta.magnitude;

            if (nextDistance >= required) return next;

            // 경계 위로 옮긴다. 방향이 정해지지 않는 자리(완전히 겹침)에서는 지금 위치 쪽을 쓴다.
            Vector2 direction;
            if (nextDistance > MinDelta)
                direction = delta / nextDistance;
            else if (currentDistance > MinDelta)
                direction = (current - neighborCenter) / currentDistance;
            else
                direction = Vector2.right;

            return neighborCenter + direction * required;
        }
    }
}
