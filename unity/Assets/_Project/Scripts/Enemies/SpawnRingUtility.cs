using UnityEngine;

namespace SushiSurvival.Enemies
{
    public static class SpawnRingUtility
    {
        public static Vector2 GetPositionOnRing(Vector2 center, float radius, float angleRad)
        {
            var offset = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * radius;
            return center + offset;
        }
    }
}
