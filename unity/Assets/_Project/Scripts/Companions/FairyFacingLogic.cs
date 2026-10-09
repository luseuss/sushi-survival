using UnityEngine;

namespace SushiSurvival.Companions
{
    public static class FairyFacingLogic
    {
        /// <summary>
        /// 이동 방향에 맞춰 그림을 뒤집을지. 거의 안 움직이면(데드존 안) 이전 방향을 그대로 둬서
        /// 떨림으로 좌우가 깜빡이지 않게 한다.
        /// </summary>
        public static bool FlipX(float horizontalVelocity, bool currentlyFlipped, bool spriteFacesRight,
                                 float deadZone = 0.02f)
        {
            if (Mathf.Abs(horizontalVelocity) < deadZone) return currentlyFlipped;

            bool movingRight = horizontalVelocity > 0f;
            return movingRight != spriteFacesRight;
        }
    }
}
