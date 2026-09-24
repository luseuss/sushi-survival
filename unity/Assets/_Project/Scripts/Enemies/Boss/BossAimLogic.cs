using UnityEngine;

namespace SushiSurvival.Enemies.Boss
{
    /// <summary>보스 조준·돌진 방향 계산.</summary>
    public static class BossAimLogic
    {
        /// <summary>
        /// 메테오가 떨어질 지점. 예고 시간 동안 플레이어가 이동할 거리를 lead(0~1) 비율만큼 앞서 조준한다.
        /// 0이면 발사 순간의 위치 그대로라 제자리에서만 맞고, 1이면 같은 방향으로 계속 달릴 때 정확히 맞는다.
        /// </summary>
        public static Vector2 PredictTarget(Vector2 position, Vector2 velocity, float warningTime, float lead)
        {
            float clampedLead = Mathf.Clamp01(lead);
            return position + velocity * (Mathf.Max(0f, warningTime) * clampedLead);
        }

        /// <summary>돌진 방향(단위 벡터). 둘이 겹쳐 방향이 없으면 fallback을 쓴다.</summary>
        public static Vector2 ChargeDirection(Vector2 from, Vector2 to, Vector2 fallback)
        {
            Vector2 delta = to - from;
            if (delta.sqrMagnitude < 0.0001f)
                return fallback.sqrMagnitude < 0.0001f ? Vector2.right : fallback.normalized;

            return delta.normalized;
        }
    }
}
