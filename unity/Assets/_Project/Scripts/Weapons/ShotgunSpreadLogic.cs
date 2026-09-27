using UnityEngine;

namespace SushiSurvival.Weapons
{
    /// <summary>샷건 산탄이 시선 방향을 중심으로 부채꼴로 퍼지는 방향을 계산하는 순수 함수.</summary>
    public static class ShotgunSpreadLogic
    {
        /// <summary>
        /// index번째 산탄의 시선 대비 각도. 전체 퍼짐 각도를 균등하게 나눠 가운데가 0°가 되게 한다.
        /// 산탄이 1발이면 퍼짐 없이 정면으로 나간다.
        /// </summary>
        public static float OffsetDegrees(int index, int pelletCount, float spreadDegrees)
        {
            if (pelletCount <= 1) return 0f;

            float step = spreadDegrees / (pelletCount - 1);
            return -spreadDegrees * 0.5f + step * index;
        }

        public static Vector2 Rotate(Vector2 direction, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            return new Vector2(direction.x * cos - direction.y * sin, direction.x * sin + direction.y * cos);
        }
    }
}
