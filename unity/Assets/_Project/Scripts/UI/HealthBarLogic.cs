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
    }
}
