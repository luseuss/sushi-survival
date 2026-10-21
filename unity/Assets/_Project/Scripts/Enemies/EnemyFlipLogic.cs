using UnityEngine;

namespace SushiSurvival.Enemies
{
    /// <summary>몹이 가는 방향에 맞춰 그림을 좌우로 뒤집는 판정의 순수 계산부.</summary>
    public static class EnemyFlipLogic
    {
        /// <summary>
        /// 이동 방향의 가로 성분으로 그림을 뒤집을지 정한다. 그림이 오른쪽을 보고 그려져 있으면 왼쪽으로 갈 때 뒤집는다
        /// (artFacesRight = true). 가로 성분이 deadZone 안이면(거의 위아래로 움직일 때) 지금 상태를 유지해
        /// 방향이 왔다 갔다 하며 깜빡이는 걸 막는다.
        /// </summary>
        public static bool ShouldFlip(bool currentlyFlipped, float directionX, float deadZone, bool artFacesRight)
        {
            if (Mathf.Abs(directionX) < deadZone) return currentlyFlipped;

            bool movingLeft = directionX < 0f;
            return artFacesRight ? movingLeft : !movingLeft;
        }
    }
}
