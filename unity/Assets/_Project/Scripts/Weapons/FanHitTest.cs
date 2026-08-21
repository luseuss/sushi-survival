using UnityEngine;

namespace SushiSurvival.Weapons
{
    public static class FanHitTest
    {
        private const float MinDistance = 0.0001f;
        private const float MinFacingSqrMagnitude = 0.0001f;
        // Vector2.Angle은 float 정밀도 오차로 정확한 경계값(예: 60.0)이
        // 60.00001처럼 나올 수 있어 아주 작은 허용오차를 둔다.
        private const float AngleEpsilonDeg = 0.01f;

        public static bool IsInsideFan(Vector2 origin, Vector2 facing, float radius, float angleDeg, Vector2 targetPos)
        {
            Vector2 toTarget = targetPos - origin;
            float distance = toTarget.magnitude;

            if (distance > radius) return false;
            if (distance < MinDistance) return true;
            if (facing.sqrMagnitude < MinFacingSqrMagnitude) return false;

            float angleBetween = Vector2.Angle(facing.normalized, toTarget.normalized);
            return angleBetween <= angleDeg * 0.5f + AngleEpsilonDeg;
        }
    }
}
