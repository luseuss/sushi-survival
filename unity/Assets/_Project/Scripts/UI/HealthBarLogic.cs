using UnityEngine;

namespace SushiSurvival.UI
{
    public static class HealthBarLogic
    {
        public static float ComputeFillAmount(float current, float max)
        {
            if (max <= 0f) return 0f;

            return Mathf.Clamp01(current / max);
        }

        /// <summary>current를 target 쪽으로 maxDelta만큼만 옮긴다. 스냅 대신
        /// 부드럽게 줄어드는 체력바에 쓴다.</summary>
        public static float MoveTowardsFill(float current, float target, float maxDelta)
        {
            return Mathf.MoveTowards(Mathf.Clamp01(current), Mathf.Clamp01(target), Mathf.Max(0f, maxDelta));
        }
    }
}
