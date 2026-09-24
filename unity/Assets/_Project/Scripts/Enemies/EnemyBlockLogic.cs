using UnityEngine;

namespace SushiSurvival.Enemies
{
    /// <summary>
    /// 적이 플레이어 몸 안으로 파고들지 못하게 막는 계산. 플레이어와 적이 둘 다 Kinematic Rigidbody라
    /// 유니티 물리가 서로를 밀어내지 않고 그냥 겹쳐 통과한다. 그렇다고 Dynamic으로 바꾸면 적 수백 마리의
    /// 물리 부담과 서로 끼는 문제가 생겨서, 이동 결과를 원 두 개가 겹치지 않는 위치로 보정한다.
    /// </summary>
    public static class EnemyBlockLogic
    {
        private const float MinDelta = 0.0001f;

        /// <summary>
        /// next가 blocker 중심에서 minDistance보다 가까우면, 같은 방향으로 minDistance만큼 떨어진 경계 위로 옮긴다.
        /// 플레이어가 적 쪽으로 밀고 들어와도 다음 프레임에 적이 경계 밖으로 밀려나 "부딪히는" 느낌이 된다.
        /// </summary>
        /// <param name="fallbackDirection">두 중심이 거의 같은 자리일 때 밀어낼 방향(단위 벡터가 아니어도 된다).</param>
        public static Vector2 Resolve(Vector2 next, Vector2 blockerCenter, float minDistance, Vector2 fallbackDirection)
        {
            if (minDistance <= 0f) return next;

            Vector2 delta = next - blockerCenter;
            float distance = delta.magnitude;

            if (distance >= minDistance) return next;

            if (distance < MinDelta)
            {
                Vector2 direction = fallbackDirection.sqrMagnitude < MinDelta ? Vector2.right : fallbackDirection.normalized;
                return blockerCenter + direction * minDistance;
            }

            return blockerCenter + delta / distance * minDistance;
        }
    }
}
