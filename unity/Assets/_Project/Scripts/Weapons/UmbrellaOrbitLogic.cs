using UnityEngine;

namespace SushiSurvival.Weapons
{
    /// <summary>회전 우산이 균등한 각도로 캐릭터를 도는 좌표를 계산하는 순수 함수.</summary>
    public static class UmbrellaOrbitLogic
    {
        public static float AngleStepDegrees(int count) => count > 0 ? 360f / count : 0f;

        public static float AngleForIndex(float baseAngleDegrees, int index, int count)
            => baseAngleDegrees + AngleStepDegrees(count) * index;

        public static Vector2 PositionForAngle(float angleDegrees, float radius)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
        }
    }
}
