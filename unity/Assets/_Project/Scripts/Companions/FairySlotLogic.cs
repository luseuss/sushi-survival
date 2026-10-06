using UnityEngine;

namespace SushiSurvival.Companions
{
    public static class FairySlotLogic
    {
        private const float TopAngleDegrees = 90f;

        /// <summary>
        /// 요정은 플레이어 머리 위(90°)를 중심으로 arcSpacingDegrees 간격으로 부채꼴로 선다.
        /// bob은 마릿수와 무관하게 슬롯마다 위상을 달리해 둥둥 떠다니게 한다(y만).
        /// </summary>
        public static Vector2 SlotOffset(int index, int count, float radius, float arcSpacingDegrees,
                                         float time, float bobAmplitude, float bobSpeed)
        {
            if (count <= 0) return Vector2.zero;

            int clamped = Mathf.Clamp(index, 0, count - 1);
            float angle = (TopAngleDegrees + (clamped - (count - 1) * 0.5f) * -arcSpacingDegrees) * Mathf.Deg2Rad;

            float bob = Mathf.Sin(time * bobSpeed + clamped * 1.7f) * bobAmplitude;

            return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius + bob);
        }

        /// <summary>1 - e^(-k·dt) 보간. 프레임레이트가 달라도 같은 시간엔 같은 거리만큼 따라온다.</summary>
        public static Vector2 Follow(Vector2 current, Vector2 target, float sharpness, float deltaTime)
        {
            float t = 1f - Mathf.Exp(-Mathf.Max(0f, sharpness) * Mathf.Max(0f, deltaTime));
            return Vector2.Lerp(current, target, t);
        }
    }
}
