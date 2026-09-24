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

        /// <summary>
        /// 좌우만 구분하는 시선 방향. 계란 양산은 그림이 좌우로만 뒤집히므로, 판정도 눈에 보이는
        /// 방향과 같게 이 값을 써야 한다. 이동 방향 벡터를 그대로 쓰면 위로 달릴 때 양산은 오른쪽으로
        /// 휘둘리는데 판정은 위쪽으로 나가서, 보이는 스윙 앞의 적이 맞지 않는다.
        /// </summary>
        public static Vector2 HorizontalFacing(Vector2 facing)
            => IsFacingRight(facing) ? Vector2.right : Vector2.left;
    }
}
