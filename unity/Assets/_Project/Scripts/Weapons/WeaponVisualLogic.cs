using UnityEngine;

namespace SushiSurvival.Weapons
{
    public enum WeaponOrientMode
    {
        /// <summary>계란(양산) — 좌우 반전만 한다.</summary>
        FlipOnly,
        /// <summary>간장새우(라이플) — 시선 방향으로 회전하며 캐릭터 주위를 공전한다.</summary>
        RotateToFacing
    }

    public static class WeaponVisualLogic
    {
        /// <summary>
        /// 캐릭터 기준 무기 위치. 왼쪽을 볼 때는 x만 뒤집는다(y는 그대로 —
        /// 뒤집으면 무기가 위아래로 튄다).
        /// </summary>
        public static Vector2 ComputeLocalOffset(Vector2 baseOffset, bool facingRight)
            => facingRight ? baseOffset : new Vector2(-baseOffset.x, baseOffset.y);

        /// <summary>시선 방향을 스프라이트 회전각(도)으로 바꾼다. 오른쪽이 0도.</summary>
        public static float ComputeRotationDegrees(Vector2 facing)
            => Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;

        /// <summary>시선 방향으로 distance만큼 떨어진 위치(캐릭터 주위 공전).</summary>
        public static Vector2 ComputeOrbitOffset(Vector2 facing, float distance)
            => facing.normalized * distance;

        /// <summary>
        /// 회전 후 스프라이트가 뒤집혀 보이는지 판정한다. 최종 회전각이
        /// 위쪽 반원(90°~270°)에 들어가면 그림이 거꾸로 서므로 세로로 뒤집어 준다.
        /// 스프라이트 기본 방향 오프셋이 더해진 "최종" 각도를 넣어야 한다.
        /// </summary>
        public static bool ShouldFlipVertically(float rotationDegrees)
            => Mathf.Cos(rotationDegrees * Mathf.Deg2Rad) < 0f;
    }
}
