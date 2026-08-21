using UnityEngine;

namespace SushiSurvival.Player
{
    /// <summary>
    /// "이동 중엔 이동 방향, 정지 시 마지막 이동 방향 유지" 로직의 순수 함수 버전.
    /// 계란·간장새우·이나리가 공통으로 사용한다.
    /// </summary>
    public static class FacingLogic
    {
        private const float MinInputSqrMagnitude = 0.0001f;

        /// <summary>
        /// 이 입력을 "이동 중"으로 볼지 판정한다. 방향 유지 판정과 이동
        /// 애니메이션 전환이 같은 기준을 쓰도록 여기 한 곳에만 임계값을 둔다.
        /// </summary>
        public static bool IsMoving(Vector2 moveInput)
            => moveInput.sqrMagnitude >= MinInputSqrMagnitude;

        public static Vector2 ComputeFacing(Vector2 currentFacing, Vector2 moveInput)
        {
            if (!IsMoving(moveInput))
                return currentFacing;

            return moveInput.normalized;
        }

        public static bool IsFacingRight(Vector2 facing) => facing.x >= 0f;
    }
}
